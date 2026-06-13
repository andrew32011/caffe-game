/// <summary>
/// Система приготовления кофе на основе кликов по 3D-предметам на столах
/// (а не по кнопкам UI). Игрок выбирает предмет → он подсвечивается и летит
/// в кружку игрока (CupController), камера переезжает на следующую зону через Stages.
///
/// Порядок зон (этапы Stages): 2 ингредиенты → 3 машина (объём/сладость/заварка)
/// → 4 топпинги → 5 стойка (готово к подаче).
///
/// Сцена: MainScene
/// Зависимости: Stages, CupController, IngredientItem (саморегистрация), GameInput
/// SDK: Нет
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CoffeeCraftingSystem : MonoBehaviour
{
    public static CoffeeCraftingSystem Instance { get; private set; }

    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Машина этапов (существующая, объект StagesScripts)")]
    [SerializeField] private Stages _stages;

    [Header("Индексы этапов Stages (зоны)")]
    [SerializeField] private int _ingredientsStageIndex = 2;
    [SerializeField] private int _machineStageIndex     = 3;
    [SerializeField] private int _toppingsStageIndex    = 4;
    [SerializeField] private int _counterStageIndex     = 5;

    [Header("Кружка игрока")]
    [SerializeField] private CupController _cup;

    [Header("UI — заказ гостя (подсказка, опционально)")]
    [SerializeField] private TextMeshProUGUI _orderDisplayText;

    // ─── Реестр предметов (саморегистрация IngredientItem) ────────────────────

    private static readonly List<IngredientItem> _items = new List<IngredientItem>();
    public static void Register(IngredientItem i)   { if (!_items.Contains(i)) _items.Add(i); }
    public static void Unregister(IngredientItem i) { _items.Remove(i); }

    // ─── Состояние ───────────────────────────────────────────────────────────

    private CoffeeOrder _targetOrder;
    private CoffeeOrder _prepared;

    private bool _typeSelected, _volumeSelected, _sweetSelected, _brewed, _toppingSelected, _orderReady;

    public bool        IsOrderReady       => _orderReady;
    public CoffeeOrder GetPreparedOrder() => _prepared;
    public CoffeeOrder TargetOrder        => _targetOrder;

    // ─── Жизненный цикл ──────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
        ResetCup();
    }

    // ─── Публичное API (вызывает DayController) ──────────────────────────────

    public void SetTargetOrder(CoffeeOrder order)
    {
        _targetOrder = order;
        if (_orderDisplayText != null)
            _orderDisplayText.text = Loc.T("Заказ: ", "Order: ") + order.GetDisplayName();
    }

    /// <summary>Начинает приготовление: чистая кружка, камера к ингредиентам.</summary>
    public void Show()
    {
        ResetCup();
        _orderReady = false;
        if (_orderDisplayText != null) _orderDisplayText.gameObject.SetActive(true);
        _stages?.JumpToStage(_ingredientsStageIndex);
    }

    /// <summary>Скрывает подсказку заказа. Кружка остаётся у игрока.</summary>
    public void Hide()
    {
        if (_orderDisplayText != null) _orderDisplayText.gameObject.SetActive(false);
    }

    public void ResetCup()
    {
        _prepared        = new CoffeeOrder();
        _typeSelected    = false;
        _volumeSelected  = false;
        _sweetSelected   = false;
        _brewed          = false;
        _toppingSelected = false;
        _orderReady      = false;
        _cup?.ResetCup();
    }

    // ─── Клик по предмету ─────────────────────────────────────────────────────

    public void OnItemClicked(IngredientItem item)
    {
        if (GameInput.Locked || item == null) return;

        switch (item.kind)
        {
            case IngredientItem.ItemKind.Drink:
                _prepared.type = item.drinkType;
                _typeSelected  = true;
                _cup?.AddItemVisual(item);
                item.FlashSelected();
                _stages?.JumpToStage(_machineStageIndex);
                break;

            case IngredientItem.ItemKind.Volume:
                _prepared.volume = item.volume;
                _volumeSelected  = true;
                item.FlashSelected();
                break;

            case IngredientItem.ItemKind.Sweetness:
                _prepared.sweet = item.sweetness;
                _sweetSelected  = true;
                item.FlashSelected();
                break;

            case IngredientItem.ItemKind.Brew:
                if (_typeSelected && _volumeSelected)
                {
                    _brewed = true;
                    item.FlashSelected();
                    _stages?.JumpToStage(_toppingsStageIndex);
                }
                break;

            case IngredientItem.ItemKind.Topping:
                _prepared.topping = item.topping;
                _toppingSelected  = true;
                _cup?.AddItemVisual(item);
                item.FlashSelected();
                _stages?.JumpToStage(_counterStageIndex);
                if (_brewed) _orderReady = true;
                break;
        }
    }

    // ─── Подсказка: какой предмет нажать следующим ───────────────────────────
    //  Используется туториалом (пульсация нужного предмета) и HintManager-ом.

    public IngredientItem GetNextHintItem()
    {
        if (_targetOrder == null) return null;

        if (!_typeSelected)
            return Find(IngredientItem.ItemKind.Drink);
        if (!_volumeSelected)
            return Find(IngredientItem.ItemKind.Volume);
        if (_targetOrder.sweet != SweetnessLevel.None && !_sweetSelected)
            return Find(IngredientItem.ItemKind.Sweetness);
        if (!_brewed)
            return FindAny(IngredientItem.ItemKind.Brew);
        if (!_toppingSelected)
            return Find(IngredientItem.ItemKind.Topping);

        return null;
    }

    private IngredientItem Find(IngredientItem.ItemKind kind)
    {
        foreach (var it in _items)
            if (it != null && it.Matches(_targetOrder, kind)) return it;
        return null;
    }

    private IngredientItem FindAny(IngredientItem.ItemKind kind)
    {
        foreach (var it in _items)
            if (it != null && it.kind == kind) return it;
        return null;
    }
}
