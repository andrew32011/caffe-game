/// <summary>
/// Оркестратор приготовления. Флоу (пункт 5):
///   зона 1 — клик по сосуду (Ingridients1) → «выливается» в кружку → едем к машине;
///   машина — минигейм 2 заполнений (температура, объём), показываем «Отлично!»;
///   топпинги — клик по предметам ShelfItems → копия падает в кружку;
///   кнопка «Подать» → возвращаемся за стойку, заказ готов.
/// Желание клиента берём из CoffeeOrder (StoryDatabase): type→основа, volume→объём,
/// sweet→температура, topping→топпинг. Удовлетворённость = доля совпавших параметров.
/// Сцена: MainScene
/// Зависимости: Stages, CupController, MachineMinigame, IngredientItem, GameInput
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

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI _orderDisplayText;
    [SerializeField] private TextMeshProUGUI _achievementText; // «Отлично!» вверху экрана
    [SerializeField] private Button          _serveButton;     // «Подать» (пункт 10)

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

    private bool _orderReady;
    public bool IsOrderReady => _orderReady;
    public int  ChosenToppingCount => _chosenToppings.Count;

    // ─── Жизненный цикл ──────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        if (_serveButton != null) _serveButton.onClick.AddListener(OnServeClicked);
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
        _stage = Stage.Ingredients;
        if (_orderDisplayText != null) _orderDisplayText.gameObject.SetActive(true);
        if (_serveButton != null) _serveButton.gameObject.SetActive(false);
        _machine?.HidePanel();
        _cup?.SnapTo(CupController.Zone.Ingredients);
        _stages?.JumpToStage(_ingredientsStageIndex);
    }

    public void Hide()
    {
        if (_orderDisplayText != null) _orderDisplayText.gameObject.SetActive(false);
        if (_serveButton != null) _serveButton.gameObject.SetActive(false);
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
        _orderReady = false;
    }

    /// <summary>Доля совпавших параметров (0..1): основа, температура, объём, топпинг.</summary>
    public float EvaluateSatisfaction()
    {
        if (_target == null) return 0f;
        int total = 4, matched = 0;

        if (_chosenIngredient == TargetIngredientIndex()) matched++;
        if (Mathf.Abs(_chosenTemp   - TempTarget())   <= _tolerance) matched++;
        if (Mathf.Abs(_chosenVolume - VolumeTarget()) <= _tolerance) matched++;
        if (ToppingMatches()) matched++;

        return (float)matched / total;
    }

    // ─── Клик по предмету ─────────────────────────────────────────────────────

    public void OnItemClicked(IngredientItem item)
    {
        if (GameInput.Locked || item == null) return;

        if (_stage == Stage.Ingredients && item.kind == IngredientItem.ItemKind.Ingredient)
        {
            _chosenIngredient = item.ingredientIndex;
            item.FlashSelected();
            StartCoroutine(IngredientThenMachine(item));
        }
        else if (_stage == Stage.Toppings && item.kind == IngredientItem.ItemKind.Topping)
        {
            if (!_chosenToppings.Contains(item.topping))
                _chosenToppings.Add(item.topping);
            item.FlashSelected();
            _cup?.DropTopping(item);
            if (_serveButton != null) _serveButton.gameObject.SetActive(true);
        }
    }

    private IEnumerator IngredientThenMachine(IngredientItem vessel)
    {
        GameInput.Locked = true; // во время налива и переезда клики выключены
        if (_cup != null) yield return StartCoroutine(_cup.PourIngredient(vessel));

        _stage = Stage.Machine;
        _stages?.JumpToStage(_machineStageIndex);
        if (_cup != null) yield return StartCoroutine(_cup.MoveTo(CupController.Zone.Machine));

        // Минигейм машины: бегунок температуры, затем объёма
        if (_machine != null)
        {
            GameInput.Locked = false; // клики нужны для фиксации бегунка
            _machine.BeginGame(OnMachineDone);
        }
        else
        {
            OnMachineDone(TempTarget(), VolumeTarget()); // без минигейма — авто-совпадение
        }
    }

    private void OnMachineDone(float temp, float volume)
    {
        _chosenTemp   = temp;
        _chosenVolume = volume;
        _machine?.HidePanel();

        bool tempOk = Mathf.Abs(temp - TempTarget()) <= _tolerance;
        bool volOk  = Mathf.Abs(volume - VolumeTarget()) <= _tolerance;
        if (tempOk && volOk) ShowAchievement(Loc.T("В точку!", "Spot on!"));
        else if (tempOk || volOk) ShowAchievement(Loc.T("Неплохо", "Not bad"));

        StartCoroutine(ToToppings());
    }

    private IEnumerator ToToppings()
    {
        GameInput.Locked = true;
        _stage = Stage.Toppings;
        _stages?.JumpToStage(_toppingsStageIndex);
        if (_cup != null) yield return StartCoroutine(_cup.MoveTo(CupController.Zone.Toppings));

        GameInput.Locked = false;
        // Кнопку «Подать» можно нажать сразу (топпинги по желанию)
        if (_serveButton != null) _serveButton.gameObject.SetActive(true);
    }

    private void OnServeClicked()
    {
        if (_stage != Stage.Toppings) return;
        StartCoroutine(ServeRoutine());
    }

    private IEnumerator ServeRoutine()
    {
        GameInput.Locked = true;
        if (_serveButton != null) _serveButton.gameObject.SetActive(false);
        _stage = Stage.Done;
        _stages?.JumpToStage(_counterStageIndex);
        if (_cup != null) yield return StartCoroutine(_cup.MoveTo(CupController.Zone.Counter));
        _orderReady = true;
    }

    // ─── Подсказка для туториала: следующий нужный предмет ────────────────────

    public IngredientItem GetNextHintItem()
    {
        if (_target == null) return null;
        if (_stage == Stage.Ingredients)
            return FindIngredient(TargetIngredientIndex());
        if (_stage == Stage.Toppings && _target.topping != Topping.None)
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

    // ─── Ачивка ────────────────────────────────────────────────────────────────

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
