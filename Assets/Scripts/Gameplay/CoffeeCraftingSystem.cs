/// <summary>
/// Система приготовления кофе. Три зоны: Ингредиенты → Машина → Топпинги.
/// Камера двигается между зонами через существующую машину этапов Stages
/// (этапы 2/3/4 — зоны, этап 5 — возврат к стойке; cameraTarget задан в Stages).
/// Кружка отображается в UI снизу экрана.
/// Сцена: MainScene
/// Зависимости: Stages, CupUI
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CoffeeCraftingSystem : MonoBehaviour
{
    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Машина этапов (существующая, объект StagesScripts)")]
    [SerializeField] private Stages _stages;

    [Header("Зоны (индексы этапов Stages)")]
    [SerializeField] private int _ingredientsStageIndex = 2;
    [SerializeField] private int _machineStageIndex     = 3;
    [SerializeField] private int _toppingsStageIndex    = 4;
    [SerializeField] private int _counterStageIndex     = 5; // Возврат к стойке

    [Header("UI — Панель с зонами (кнопки навигации)")]
    [SerializeField] private GameObject _craftingPanel;
    [SerializeField] private Button     _btnIngredients;
    [SerializeField] private Button     _btnMachine;
    [SerializeField] private Button     _btnToppings;
    [SerializeField] private Button     _btnServe;         // Подать кофе

    [Header("UI — Зона ингредиентов")]
    [SerializeField] private GameObject _ingredientsPanel;
    [SerializeField] private Button[]   _ingredientButtons; // Кнопки типов напитков

    [Header("UI — Зона машины")]
    [SerializeField] private GameObject _machinePanel;
    [SerializeField] private Button     _btnSmall;
    [SerializeField] private Button     _btnMedium;
    [SerializeField] private Button     _btnLarge;
    [SerializeField] private Button     _btnSweetNone;
    [SerializeField] private Button     _btnSweetLow;
    [SerializeField] private Button     _btnSweetMed;
    [SerializeField] private Button     _btnSweetHigh;
    [SerializeField] private Button     _btnBrew;

    [Header("UI — Зона топпингов")]
    [SerializeField] private GameObject _toppingsPanel;
    [SerializeField] private Button[]   _toppingButtons;

    [Header("UI — Кружка (визуальное состояние)")]
    [SerializeField] private GameObject _cupUI;
    [SerializeField] private TextMeshProUGUI _cupStatusText;
    [SerializeField] private Image       _cupFillImage;

    [Header("UI — Заказ гостя (подсказка)")]
    [SerializeField] private TextMeshProUGUI _orderDisplayText;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private CoffeeOrder _targetOrder;   // Что заказал гость
    private CoffeeOrder _prepared;      // Что готовит игрок

    private bool _typeSelected    = false;
    private bool _volumeSelected  = false;
    private bool _sweetSelected   = false; // Может быть None — всё равно считается
    private bool _brewed          = false;
    private bool _toppingSelected = false; // Может быть None

    private bool _orderReady      = false;

    // ─── Публичные свойства ───────────────────────────────────────────────────

    public bool        IsOrderReady       => _orderReady;
    public CoffeeOrder GetPreparedOrder() => _prepared;

    // ─── Инициализация ───────────────────────────────────────────────────────

    private void Awake()
    {
        BindButtons();
    }

    private void BindButtons()
    {
        // Навигация между зонами
        _btnIngredients?.onClick.AddListener(() => GoToZone(_ingredientsStageIndex, _ingredientsPanel));
        _btnMachine    ?.onClick.AddListener(() => GoToZone(_machineStageIndex,     _machinePanel));
        _btnToppings   ?.onClick.AddListener(() => GoToZone(_toppingsStageIndex,    _toppingsPanel));
        _btnServe      ?.onClick.AddListener(OnServeClicked);

        // Ингредиенты (кнопки в порядке enum CoffeeType)
        if (_ingredientButtons != null)
        {
            CoffeeType[] types = (CoffeeType[])System.Enum.GetValues(typeof(CoffeeType));
            for (int i = 0; i < _ingredientButtons.Length && i < types.Length; i++)
            {
                CoffeeType t = types[i];
                _ingredientButtons[i].onClick.AddListener(() => SelectType(t));
            }
        }

        // Машина — объём
        _btnSmall ?.onClick.AddListener(() => SelectVolume(Volume.Small));
        _btnMedium?.onClick.AddListener(() => SelectVolume(Volume.Medium));
        _btnLarge ?.onClick.AddListener(() => SelectVolume(Volume.Large));

        // Машина — сладость
        _btnSweetNone?.onClick.AddListener(() => SelectSweetness(SweetnessLevel.None));
        _btnSweetLow ?.onClick.AddListener(() => SelectSweetness(SweetnessLevel.Low));
        _btnSweetMed ?.onClick.AddListener(() => SelectSweetness(SweetnessLevel.Medium));
        _btnSweetHigh?.onClick.AddListener(() => SelectSweetness(SweetnessLevel.High));

        // Заварить
        _btnBrew?.onClick.AddListener(OnBrewClicked);

        // Топпинги
        if (_toppingButtons != null)
        {
            Topping[] tops = (Topping[])System.Enum.GetValues(typeof(Topping));
            for (int i = 0; i < _toppingButtons.Length && i < tops.Length; i++)
            {
                Topping t = tops[i];
                _toppingButtons[i].onClick.AddListener(() => SelectTopping(t));
            }
        }
    }

    // ─── Публичное API ───────────────────────────────────────────────────────

    public void SetTargetOrder(CoffeeOrder order)
    {
        _targetOrder = order;
        if (_orderDisplayText != null)
            _orderDisplayText.text = Loc.T("Заказ: ", "Order: ") + order.GetDisplayName();
    }

    public void Show()
    {
        ResetCup();
        _craftingPanel?.SetActive(true);
        _cupUI        ?.SetActive(true);
        _orderReady = false;

        // Обновляем кнопку «Подать» — недоступна пока не готово
        if (_btnServe != null) _btnServe.interactable = false;

        // По умолчанию открываем зону ингредиентов
        GoToZone(_ingredientsStageIndex, _ingredientsPanel);
    }

    public void Hide()
    {
        _craftingPanel     ?.SetActive(false);
        _cupUI             ?.SetActive(false);
        _ingredientsPanel  ?.SetActive(false);
        _machinePanel      ?.SetActive(false);
        _toppingsPanel     ?.SetActive(false);
    }

    public void ResetCup()
    {
        _prepared       = new CoffeeOrder();
        _typeSelected   = false;
        _volumeSelected = false;
        _sweetSelected  = false;
        _brewed         = false;
        _toppingSelected= false;
        _orderReady     = false;

        UpdateCupUI();

        if (_btnServe != null) _btnServe.interactable = false;
    }

    // ─── Навигация зон ───────────────────────────────────────────────────────

    private void GoToZone(int stageIndex, GameObject panel)
    {
        HideAllZonePanels();
        panel?.SetActive(true);

        // Камеру двигает существующая машина этапов (cameraTarget в Stages)
        _stages?.JumpToStage(stageIndex);
    }

    private void HideAllZonePanels()
    {
        _ingredientsPanel?.SetActive(false);
        _machinePanel    ?.SetActive(false);
        _toppingsPanel   ?.SetActive(false);
    }

    // ─── Выбор ингредиентов ───────────────────────────────────────────────────

    private void SelectType(CoffeeType type)
    {
        _prepared.type  = type;
        _typeSelected   = true;
        UpdateCupUI();

        // Переходим к машине
        GoToZone(_machineStageIndex, _machinePanel);
    }

    private void SelectVolume(Volume vol)
    {
        _prepared.volume = vol;
        _volumeSelected  = true;
        UpdateCupUI();
        HighlightVolumeButton(vol);
    }

    private void SelectSweetness(SweetnessLevel sweet)
    {
        _prepared.sweet = sweet;
        _sweetSelected  = true;
        UpdateCupUI();
        HighlightSweetnessButton(sweet);
    }

    private void OnBrewClicked()
    {
        if (!_typeSelected || !_volumeSelected)
        {
            ShowHint(Loc.T("Сначала выбери тип напитка и объём!",
                           "Pick a drink type and size first!"));
            return;
        }

        _brewed = true;
        UpdateCupUI();

        // Анимация кофемашины
        StartCoroutine(BrewAnimation());
    }

    private IEnumerator BrewAnimation()
    {
        // Имитация приготовления
        if (_cupStatusText != null) _cupStatusText.text = Loc.T("Готовится...", "Brewing...");
        if (_cupFillImage  != null) _cupFillImage.color = new Color(0.6f, 0.4f, 0.1f);

        yield return new WaitForSeconds(1.5f);

        if (_cupStatusText != null)
            _cupStatusText.text = Loc.T("Готово! Добавь топпинг или подай.",
                                        "Done! Add a topping or serve.");

        // Переходим к топпингам
        GoToZone(_toppingsStageIndex, _toppingsPanel);
    }

    private void SelectTopping(Topping topping)
    {
        _prepared.topping = topping;
        _toppingSelected  = true;
        UpdateCupUI();

        // Возвращаемся к стойке
        GoToZone(_counterStageIndex, null);
        HideAllZonePanels();

        // Если заварено — готово к подаче
        if (_brewed)
        {
            _orderReady = true;
            if (_btnServe != null) _btnServe.interactable = true;
            if (_cupStatusText != null)
                _cupStatusText.text = Loc.T("Кофе готов! Нажми «Подать»",
                                            "Coffee is ready! Press \"Serve\"");
        }
    }

    private void OnServeClicked()
    {
        if (!_brewed)
        {
            ShowHint(Loc.T("Сначала заварить кофе в машине!",
                           "Brew the coffee in the machine first!"));
            return;
        }

        _orderReady = true;

        // Если топпинг не выбран явно — считается None
        if (!_toppingSelected)
            _prepared.topping = Topping.None;
        _toppingSelected = true;

        if (_btnServe != null) _btnServe.interactable = false;
    }

    // ─── UI кружки ────────────────────────────────────────────────────────────

    private void UpdateCupUI()
    {
        if (_cupStatusText == null) return;

        var lines = new System.Text.StringBuilder();
        lines.Append("☕ ");

        if (_typeSelected)   lines.Append(_prepared.GetTypeName());
        else                 lines.Append(Loc.T("[тип?]", "[type?]"));

        lines.Append(" • ");

        if (_volumeSelected) lines.Append(_prepared.GetVolumeName());
        else                 lines.Append(Loc.T("[объём?]", "[size?]"));

        if (_sweetSelected && _prepared.sweet != SweetnessLevel.None)
        {
            lines.Append(" • ");
            lines.Append(_prepared.GetSweetnessName());
        }

        if (_toppingSelected && _prepared.topping != Topping.None)
        {
            lines.Append(" + ");
            lines.Append(_prepared.GetToppingName());
        }

        _cupStatusText.text = lines.ToString();

        // Прогресс кружки (визуально)
        if (_cupFillImage != null)
        {
            float progress = 0;
            if (_typeSelected)   progress += 0.33f;
            if (_brewed)         progress += 0.33f;
            if (_toppingSelected)progress += 0.34f;
            _cupFillImage.fillAmount = progress;
        }
    }

    private void HighlightVolumeButton(Volume vol)
    {
        var selected = new ColorBlock();
        selected.normalColor      = new Color(0.3f, 0.8f, 0.3f);
        selected.highlightedColor = new Color(0.3f, 0.8f, 0.3f);
        selected.pressedColor     = new Color(0.2f, 0.6f, 0.2f);
        selected.selectedColor    = new Color(0.3f, 0.8f, 0.3f);
        selected.colorMultiplier  = 1f;
        selected.fadeDuration     = 0.1f;

        if (_btnSmall  != null) _btnSmall .colors = (vol == Volume.Small)  ? selected : ColorBlock.defaultColorBlock;
        if (_btnMedium != null) _btnMedium.colors = (vol == Volume.Medium) ? selected : ColorBlock.defaultColorBlock;
        if (_btnLarge  != null) _btnLarge .colors = (vol == Volume.Large)  ? selected : ColorBlock.defaultColorBlock;
    }

    private void HighlightSweetnessButton(SweetnessLevel sw)
    {
        var selected = new ColorBlock();
        selected.normalColor      = new Color(0.3f, 0.8f, 0.3f);
        selected.highlightedColor = new Color(0.3f, 0.8f, 0.3f);
        selected.pressedColor     = new Color(0.2f, 0.6f, 0.2f);
        selected.selectedColor    = new Color(0.3f, 0.8f, 0.3f);
        selected.colorMultiplier  = 1f;
        selected.fadeDuration     = 0.1f;

        if (_btnSweetNone != null) _btnSweetNone.colors = (sw == SweetnessLevel.None)   ? selected : ColorBlock.defaultColorBlock;
        if (_btnSweetLow  != null) _btnSweetLow .colors = (sw == SweetnessLevel.Low)    ? selected : ColorBlock.defaultColorBlock;
        if (_btnSweetMed  != null) _btnSweetMed .colors = (sw == SweetnessLevel.Medium) ? selected : ColorBlock.defaultColorBlock;
        if (_btnSweetHigh != null) _btnSweetHigh.colors = (sw == SweetnessLevel.High)   ? selected : ColorBlock.defaultColorBlock;
    }

    private void ShowHint(string message)
    {
        if (_cupStatusText != null)
        {
            _cupStatusText.text = message;
            StartCoroutine(ClearHintAfterDelay(message, 2f));
        }
    }

    private IEnumerator ClearHintAfterDelay(string original, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (_cupStatusText != null && _cupStatusText.text == original)
            UpdateCupUI();
    }
}
