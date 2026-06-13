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

    private void Awake()
    {
        Instance = this;
        if (_serveButton   != null) _serveButton.onClick.AddListener(OnServeClicked);
        if (_confirmButton != null) _confirmButton.onClick.AddListener(OnConfirmIngredient);
    }

    // ─── Публичное API (DayController) ───────────────────────────────────────

    public void SetTargetOrder(CoffeeOrder order)
    {
        _target = order;
        if (_orderDisplayText != null)
            _orderDisplayText.text = Loc.T("Заказ: ", "Order: ") + (order != null ? order.GetDisplayName() : "");
    }

    public void Show()
    {
        ResetState();
        _cup?.ResetCup();              // пункт 1: чистим содержимое кружки под нового клиента
        _stage = Stage.Ingredients;
        if (_orderDisplayText != null) _orderDisplayText.gameObject.SetActive(true);
        HideButton(_serveButton);
        HideButton(_confirmButton);
        if (_selectedText != null) _selectedText.gameObject.SetActive(false);
        _machine?.HidePanel();
        UpdateSatisfactionUI();        // полоса в нейтраль (50%)
        _stages?.JumpToStage(_ingredientsStageIndex);
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
                _selectedText.text = Loc.T("Выбрано: основа ", "Selected: base ") + (item.ingredientIndex + 1);
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

        StepFeedback(tempOk && volOk, partial: true);
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

    // partial=true — обновляем полосу по ходу готовки
    private void StepFeedback(bool good, bool partial)
    {
        UpdateSatisfactionUI();
        if (_commentText == null) return;
        _commentText.text = good
            ? Loc.T("Супер!", "Great!")
            : Loc.T("Не совсем то…", "Not quite…");
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
