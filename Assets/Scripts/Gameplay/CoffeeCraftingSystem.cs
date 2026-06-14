/// <summary>
/// Оркестратор приготовления. Флоу (пункт 5):
///   зона 1 — клик по сосуду (Ingridients1): сначала показываем выбор и кнопку
///            «Подтвердить»; после подтверждения «выливаем» в кружку и едем к машине;
///   машина — минигейм 2 вертикальных шкал (температура, объём);
///   топпинги — клик по предметам ShelfItems → копия падает в кружку;
///   кнопка «Подать» → возвращаемся за стойку, заказ готов.
/// Полоса удовлетворённости — UI сверху, обновляется после каждого шага, с
/// комментарием («Супер!»/«Не совсем то»/«Не то»). Себестоимость ингредиента и
/// топпингов списывается живьём (пункт 5). Клиент стартует с 50%.
/// Сцена: MainScene
/// Зависимости: Stages, CupController, MachineMinigame, IngredientItem, GameManager, GameInput
/// SDK: Нет
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoffeeCraftingSystem : MonoBehaviour
{
    public static CoffeeCraftingSystem Instance { get; private set; }

    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Машина этапов (камера)")]
    [SerializeField] private Stages _stages;
    [SerializeField] private int _ingredientsStageIndex = 2;
    [SerializeField] private int _machineStageIndex     = 3;
    [SerializeField] private int _toppingsStageIndex    = 4;
    [SerializeField] private int _counterStageIndex     = 5;

    [Header("Кружка и минигейм")]
    [SerializeField] private CupController  _cup;
    [SerializeField] private MachineMinigame _machine;

    [Header("UI — заказ / выбор / подтверждение")]
    [SerializeField] private TextMeshProUGUI _orderDisplayText;
    [SerializeField] private TextMeshProUGUI _selectedText;   // «Выбрано: …»
    [SerializeField] private Button          _confirmButton;  // «Подтвердить» (пункт 2)
    [SerializeField] private Button          _serveButton;    // «Подать» (пункт 10)
    [SerializeField] private TextMeshProUGUI _achievementText;// «В точку!» (минигейм)

    [Header("UI — удовлетворённость (сверху, пункт 4)")]
    [SerializeField] private Image           _satisfactionFill; // Image: Filled, Horizontal
    [SerializeField] private TextMeshProUGUI _commentText;      // «Супер!/Не то»

    [Header("Экономика (пункт 5)")]
    [SerializeField] private int _ingredientCost = 8;
    [SerializeField] private int _toppingCost    = 3;
    [Tooltip("Панель «нет денег» с кнопкой рекламы за монеты (включается при нехватке).")]
    [SerializeField] private GameObject _noMoneyPanel;

    [Header("Главный герой (деактивируется на время готовки, пункт 8)")]
    [SerializeField] private GameObject _heroObject;

    [Header("Подсказка за рекламу (доступна после 2 провалов подряд, пункт 4.1)")]
    [SerializeField] private Button _adHintButton;

    [Header("Допуск совпадения ползунков (0..1)")]
    [SerializeField] private float _tolerance = 0.15f;

    // ─── Реестр предметов ─────────────────────────────────────────────────────

    private static readonly List<IngredientItem> _items = new List<IngredientItem>();
    public static void Register(IngredientItem i)   { if (!_items.Contains(i)) _items.Add(i); }
    public static void Unregister(IngredientItem i) { _items.Remove(i); }

    // ─── Состояние ───────────────────────────────────────────────────────────

    private enum Stage { Idle, Ingredients, Machine, Toppings, Done }
    private Stage _stage = Stage.Idle;

    private CoffeeOrder _target;

    private int   _chosenIngredient = -1;
    private float _chosenTemp, _chosenVolume;
    private readonly List<Topping> _chosenToppings = new List<Topping>();

    private bool _ingredientDecided, _machineDecided, _toppingDecided;
    private IngredientItem _pending;     // выбранный, но не подтверждённый сосуд
    private int  _currentDrinkCost;      // себестоимость текущего напитка

    private bool _orderReady;
    public bool IsOrderReady       => _orderReady;
    public int  ChosenToppingCount => _chosenToppings.Count;
    public int  CurrentDrinkCost   => _currentDrinkCost;

    // ─── Жизненный цикл ──────────────────────────────────────────────────────

    private bool _adHintAllowed;

    private void Awake()
    {
        Instance = this;
        if (_serveButton   != null) _serveButton.onClick.AddListener(OnServeClicked);
        if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmIngredient);
        if (_adHintButton  != null) _adHintButton.onClick.AddListener(OnAdHintClicked);
    }

    /// <summary>Разрешена ли реклама-подсказка для текущего клиента (ставит DayController).</summary>
    public void SetAdHintAllowed(bool allowed) => _adHintAllowed = allowed;

    private void OnAdHintClicked()
    {
#if RewardedAdv_yg
        YG.YG2.RewardedAdvShow("hint");
        // награду ловим в HintManager/здесь? упрощённо — открываем сразу после показа
#endif
        RevealPreciseHint();
        if (_adHintButton != null) _adHintButton.gameObject.SetActive(false);
    }

    // ─── Публичное API (DayController) ───────────────────────────────────────

    public void SetTargetOrder(CoffeeOrder order)
    {
        _target = order;
        _hintRevealed = false;
        // Пункт 4.1/4.2: по умолчанию только расплывчатый намёк, не точный заказ
        if (_orderDisplayText != null)
            _orderDisplayText.text = order != null ? VagueHint() : "";
    }

    private bool _hintRevealed;

    /// <summary>Открыть точную подсказку (после просмотра рекламы при 2 провалах подряд, пункт 4.1).</summary>
    public void RevealPreciseHint()
    {
        _hintRevealed = true;
        if (_orderDisplayText != null && _target != null)
            _orderDisplayText.text = DescribeTarget();
    }

    // Расплывчатый намёк клиента — игрок додумывает сам (пункт 4.2)
    private string VagueHint()
    {
        string temp = TempTarget() < 0.35f ? Loc.T("прохладного", "something cool")
            : TempTarget() < 0.7f ? Loc.T("тёплого", "something warm")
            : Loc.T("обжигающе горячего", "something piping hot");
        string vol = _target.volume == Volume.Small ? Loc.T("совсем чуть-чуть", "just a little")
            : _target.volume == Volume.Large ? Loc.T("да побольше", "and plenty of it")
            : Loc.T("в самый раз", "just right");
        // Пункт 7: намёк про основу теперь привязан к реальному напитку (а не к
        // абстрактному «необычное/привычное»), чтобы был понятен в связке с ингредиентами.
        string baseHint = BaseHintWord();
        string top = _target.topping != Topping.None
            ? Loc.T(", и чтоб с изюминкой", ", with a little something extra")
            : "";
        return Loc.T("«Хочется ", "\"I'd like ") + baseHint + ", " + temp + ", " + vol + top + "…»";
    }

    // Намёк по основе напитка — по категории типа кофе (пункт 7).
    private string BaseHintWord()
    {
        switch (_target.type)
        {
            case CoffeeType.Espresso:
            case CoffeeType.Americano:
            case CoffeeType.BlackCoffee:
            case CoffeeType.TruthBrew:
                return Loc.T("крепкого кофе", "a strong coffee");
            case CoffeeType.Cappuccino:
            case CoffeeType.Latte:
            case CoffeeType.Mocha:
            case CoffeeType.HotChocolate:
                return Loc.T("чего-то мягкого, молочного", "something soft and milky");
            case CoffeeType.HerbalTea:
            case CoffeeType.GreenTea:
                return Loc.T("чего-то чайного, травяного", "something tea-like and herbal");
            case CoffeeType.Water:
                return Loc.T("просто воды", "just some water");
            default:
                return Loc.T("чего-нибудь на твой вкус", "something to your taste");
        }
    }

    // Точное описание желания (только после рекламной подсказки, пункт 4.1)
    private string DescribeTarget()
    {
        string baseName = IngredientName(TargetIngredientIndex());
        string temp = TempWord(TempTarget());
        string vol  = VolWord(_target.volume);
        string top  = _target.topping != Topping.None ? " + " + ToppingName(_target.topping) : "";
        return Loc.T("Точно хочет: ", "Exactly wants: ") + baseName + " · " + temp + " · " + vol + top;
    }

    private string IngredientName(int index)
    {
        var it = FindIngredient(index);
        if (it != null && !string.IsNullOrEmpty(it.displayName)) return it.displayName;
        return Loc.T("основа ", "base ") + (index + 1);
    }

    private string ToppingName(Topping t)
    {
        var it = FindTopping(t);
        if (it != null && !string.IsNullOrEmpty(it.displayName)) return it.displayName;
        return t.ToString();
    }

    private string TempWord(float v) =>
        v < 0.35f ? Loc.T("холодный", "cold")
        : v < 0.7f ? Loc.T("тёплый", "warm")
        : Loc.T("горячий", "hot");

    private string VolWord(Volume v) =>
        v == Volume.Small  ? Loc.T("маленький", "small")
        : v == Volume.Large ? Loc.T("большой", "large")
        : Loc.T("средний", "medium");

    public void Show()
    {
        ResetState();
        _cup?.ResetCup();              // пункт 1: чистим содержимое кружки под нового клиента
        _stage = Stage.Ingredients;
        GameInput.Locked = false;      // пункт 5: разблокируем клики для нового клиента
        // Пункт 2: ГГ НЕ прячем сразу — он исчезнет только после того, как камера
        // уедет от стойки к ингредиентам (см. HideHeroWhenCameraLeaves ниже).
        if (_orderDisplayText != null) _orderDisplayText.gameObject.SetActive(true);
        HideButton(_serveButton);
        HideButton(_confirmButton);
        if (_selectedText != null) _selectedText.gameObject.SetActive(false);
        if (_noMoneyPanel != null) _noMoneyPanel.SetActive(false);
        if (_adHintButton != null) _adHintButton.gameObject.SetActive(_adHintAllowed); // пункт 4.1
        _machine?.HidePanel();
        UpdateSatisfactionUI();        // полоса в нейтраль (50%)
        _stages?.JumpToStage(_ingredientsStageIndex);
        StartCoroutine(HideHeroWhenCameraLeaves());
    }

    /// <summary>Показать/скрыть главного героя (активен только на диалоге и во сне, пункт 2).</summary>
    public void SetHeroVisible(bool on)
    {
        if (_heroObject != null) _heroObject.SetActive(on);
    }

    // Пункт 2: прячем ГГ только после того, как камера доехала до зоны готовки
    // (ушла от стойки), чтобы он не исчезал прямо в кадре.
    private IEnumerator HideHeroWhenCameraLeaves()
    {
        if (_stages != null)
        {
            yield return null;                       // даём кадр на старт перехода
            while (_stages.IsTransitioning) yield return null;
        }
        SetHeroVisible(false);
    }

    public void Hide()
    {
        if (_orderDisplayText != null) _orderDisplayText.gameObject.SetActive(false);
        HideButton(_serveButton);
        HideButton(_confirmButton);
        _machine?.HidePanel();
    }

    public void ResetCup()
    {
        ResetState();
        _cup?.ResetCup();
    }

    /// <summary>Пункт 3: кружка плавно переходит из руки ГГ в руку гостя и исчезает.
    /// Вызывается на этапе подачи/реакции (где иначе «ничего не происходит»).</summary>
    public IEnumerator HandCupToCustomer(Transform customer)
    {
        if (_cup != null) yield return StartCoroutine(_cup.HandToCustomer(customer));
    }

    private void ResetState()
    {
        _chosenIngredient = -1;
        _chosenTemp = _chosenVolume = 0f;
        _chosenToppings.Clear();
        _ingredientDecided = _machineDecided = _toppingDecided = false;
        _pending = null;
        _currentDrinkCost = 0;
        _orderReady = false;
    }

    /// <summary>Итоговая удовлетворённость 0..1 (старт 50%, ±за каждый параметр).</summary>
    public float EvaluateSatisfaction()
    {
        _toppingDecided = true; // на выдаче топпинг считается решённым (даже если не клали)
        return Satisfaction();
    }

    // ─── Клик по предмету ─────────────────────────────────────────────────────

    public void OnItemClicked(IngredientItem item)
    {
        if (GameInput.Locked || item == null) return;

        if (_stage == Stage.Ingredients && item.kind == IngredientItem.ItemKind.Ingredient)
        {
            // Пункт 2: сначала выбираем (подсветка + «Выбрано» + подтвердить)
            if (_pending != null) _pending.SetPulsing(false);
            _pending = item;
            item.SetPulsing(true);
            if (_selectedText != null)
            {
                _selectedText.gameObject.SetActive(true);
                string nm = !string.IsNullOrEmpty(item.displayName)
                    ? item.displayName
                    : Loc.T("основа ", "base ") + (item.ingredientIndex + 1);
                _selectedText.text = Loc.T("Выбрано: ", "Selected: ") + nm;
            }
            ShowButton(_confirmButton);
        }
        else if (_stage == Stage.Toppings && item.kind == IngredientItem.ItemKind.Topping)
        {
            if (!_chosenToppings.Contains(item.topping))
            {
                _chosenToppings.Add(item.topping);
                Spend(_toppingCost);                 // пункт 5: списываем за топпинг
            }
            item.FlashSelected();
            _cup?.DropTopping(item);
            StepFeedback(_target != null && item.topping == _target.topping, partial: true);
            ShowButton(_serveButton);
        }
    }

    // Подтверждение выбранного ингредиента (кнопка)
    private void OnConfirmIngredient()
    {
        if (_stage != Stage.Ingredients || _pending == null) return;

        // Пункт 5: не хватает денег на основу — показываем экран рекламы за монеты
        if (GameManager.Instance != null && GameManager.Instance.TotalCoins < _ingredientCost)
        {
            if (_noMoneyPanel != null) _noMoneyPanel.SetActive(true);
            return;
        }

        var vessel = _pending;
        vessel.SetPulsing(false);
        _chosenIngredient = vessel.ingredientIndex;
        _ingredientDecided = true;
        Spend(_ingredientCost);                       // пункт 5: списываем за основу

        if (_selectedText != null) _selectedText.gameObject.SetActive(false);
        HideButton(_confirmButton);

        StepFeedback(_chosenIngredient == TargetIngredientIndex(), partial: true);
        StartCoroutine(IngredientThenMachine(vessel));
        _pending = null;
    }

    private IEnumerator IngredientThenMachine(IngredientItem vessel)
    {
        GameInput.Locked = true;
        if (_cup != null) yield return StartCoroutine(_cup.PourIngredient(vessel));

        _stage = Stage.Machine;
        _stages?.JumpToStage(_machineStageIndex);
        if (_cup != null) yield return StartCoroutine(_cup.MoveTo(CupController.Zone.Machine));

        if (_machine != null)
        {
            GameInput.Locked = false; // клики нужны для фиксации бегунка
            _machine.BeginGame(OnMachineDone);
        }
        else
        {
            OnMachineDone(TempTarget(), VolumeTarget());
        }
    }

    private void OnMachineDone(float temp, float volume)
    {
        _chosenTemp   = temp;
        _chosenVolume = volume;
        _machineDecided = true;
        _machine?.HidePanel();

        bool tempOk = Mathf.Abs(temp   - TempTarget())   <= _tolerance;
        bool volOk  = Mathf.Abs(volume - VolumeTarget()) <= _tolerance;
        if (tempOk && volOk) ShowAchievement(Loc.T("В точку!", "Spot on!"));

        // Пункт 5: удовлетворённость зависит ОТ КАЖДОЙ фичи отдельно — и температуры,
        // и объёма. Шкала уже учитывает обе (Satisfaction()), а комментарий честно
        // показывает, что именно угадано.
        string msg;
        if (tempOk && volOk)
            msg = Loc.T("Температура и объём — идеально!", "Temperature and volume — perfect!");
        else if (tempOk)
            msg = Loc.T("Температура та, а объём мимо.", "Temperature is right, but the volume is off.");
        else if (volOk)
            msg = Loc.T("Объём верный, но температура не та.", "Volume is right, but the temperature is off.");
        else
            msg = Loc.T("И температура, и объём не угаданы.", "Both temperature and volume are off.");

        StepFeedback(tempOk && volOk, partial: true, message: msg);
        StartCoroutine(ToToppings());
    }

    private IEnumerator ToToppings()
    {
        GameInput.Locked = true;
        _stage = Stage.Toppings;
        _stages?.JumpToStage(_toppingsStageIndex);
        if (_cup != null) yield return StartCoroutine(_cup.MoveTo(CupController.Zone.Toppings));

        GameInput.Locked = false;
        ShowButton(_serveButton); // топпинги по желанию — можно сразу подать
    }

    private void OnServeClicked()
    {
        if (_stage != Stage.Toppings) return;
        StartCoroutine(ServeRoutine());
    }

    private IEnumerator ServeRoutine()
    {
        GameInput.Locked = true;
        HideButton(_serveButton);
        _stage = Stage.Done;
        _stages?.JumpToStage(_counterStageIndex);
        if (_cup != null) yield return StartCoroutine(_cup.MoveTo(CupController.Zone.Counter));
        SetHeroVisible(true);   // пункт 8: ГГ снова виден за стойкой
        _orderReady = true;
    }

    // ─── Удовлетворённость и комментарии ─────────────────────────────────────

    private float Satisfaction()
    {
        int matched = 0, mismatched = 0;

        if (_ingredientDecided) { if (_chosenIngredient == TargetIngredientIndex()) matched++; else mismatched++; }
        if (_machineDecided)
        {
            if (Mathf.Abs(_chosenTemp   - TempTarget())   <= _tolerance) matched++; else mismatched++;
            if (Mathf.Abs(_chosenVolume - VolumeTarget()) <= _tolerance) matched++; else mismatched++;
        }
        if (_toppingDecided) { if (ToppingMatches()) matched++; else mismatched++; }

        return Mathf.Clamp01(0.5f + 0.5f * (matched - mismatched) / 4f);
    }

    private Coroutine _commentCo;

    // partial=true — обновляем полосу по ходу готовки.
    // message != null — показываем свой текст (например, отдельно по температуре/объёму).
    private void StepFeedback(bool good, bool partial, string message = null)
    {
        UpdateSatisfactionUI();
        if (_commentText == null) return;
        _commentText.text = message ?? (good
            ? Loc.T("Супер!", "Great!")
            : Loc.T("Не совсем то…", "Not quite…"));
        if (_commentCo != null) StopCoroutine(_commentCo);
        _commentCo = StartCoroutine(CommentRoutine());
    }

    private IEnumerator CommentRoutine()
    {
        _commentText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.4f);
        if (_commentText != null) _commentText.gameObject.SetActive(false);
    }

    private void UpdateSatisfactionUI()
    {
        if (_satisfactionFill != null)
            _satisfactionFill.fillAmount = Satisfaction();
    }

    // ─── Экономика ────────────────────────────────────────────────────────────

    private void Spend(int cost)
    {
        _currentDrinkCost += cost;
        GameManager.Instance?.AddCoins(-cost); // живое списание из денег кофейни
    }

    // ─── Подсказка для туториала ──────────────────────────────────────────────

    public IngredientItem GetNextHintItem()
    {
        if (_target == null) return null;
        if (_stage == Stage.Ingredients && !_ingredientDecided)
            return FindIngredient(TargetIngredientIndex());
        if (_stage == Stage.Toppings && _target.topping != Topping.None && !_chosenToppings.Contains(_target.topping))
            return FindTopping(_target.topping);
        return null;
    }

    private IngredientItem FindIngredient(int index)
    {
        foreach (var it in _items)
            if (it != null && it.kind == IngredientItem.ItemKind.Ingredient && it.ingredientIndex == index)
                return it;
        return null;
    }

    private IngredientItem FindTopping(Topping t)
    {
        foreach (var it in _items)
            if (it != null && it.kind == IngredientItem.ItemKind.Topping && it.topping == t)
                return it;
        return null;
    }

    // ─── Цели из заказа ───────────────────────────────────────────────────────

    private int IngredientCount()
    {
        int n = 0;
        foreach (var it in _items)
            if (it != null && it.kind == IngredientItem.ItemKind.Ingredient) n++;
        return Mathf.Max(1, n);
    }

    private int TargetIngredientIndex() => (int)_target.type % IngredientCount();

    private float TempTarget()
    {
        switch (_target.sweet)
        {
            case SweetnessLevel.None:   return 0.2f;
            case SweetnessLevel.Low:    return 0.45f;
            case SweetnessLevel.Medium: return 0.65f;
            default:                    return 0.9f;
        }
    }

    private float VolumeTarget()
    {
        switch (_target.volume)
        {
            case Volume.Small:  return 0.25f;
            case Volume.Medium: return 0.55f;
            default:            return 0.85f;
        }
    }

    private bool ToppingMatches()
    {
        if (_target.topping == Topping.None) return _chosenToppings.Count == 0;
        return _chosenToppings.Contains(_target.topping);
    }

    // ─── Утилиты UI ────────────────────────────────────────────────────────────

    private void ShowButton(Button b) { if (b != null) b.gameObject.SetActive(true); }
    private void HideButton(Button b) { if (b != null) b.gameObject.SetActive(false); }

    private Coroutine _achievementCo;

    private void ShowAchievement(string text)
    {
        if (_achievementText == null) return;
        if (_achievementCo != null) StopCoroutine(_achievementCo);
        _achievementText.text = text;
        _achievementCo = StartCoroutine(AchievementRoutine());
    }

    private IEnumerator AchievementRoutine()
    {
        _achievementText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.6f);
        _achievementText.gameObject.SetActive(false);
    }
}
