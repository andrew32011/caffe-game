/// <summary>
/// Кликабельный 3D-предмет на столе (ингредиент / объём / сладость / заварка / топпинг).
/// Игрок выбирает напиток не кнопкой, а кликом по предмету на столе.
/// Требует Collider. Клик ловится через OnMouseDown (нужна Camera.main).
/// Сцена: MainScene
/// Зависимости: CoffeeCraftingSystem, GameInput, GameEnums
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IngredientItem : MonoBehaviour
{
    public enum ItemKind { Drink, Volume, Sweetness, Brew, Topping }

    [Header("Что это за предмет")]
    public ItemKind kind = ItemKind.Drink;
    public CoffeeType     drinkType;
    public Volume         volume;
    public SweetnessLevel sweetness;
    public Topping        topping;

    [Header("Подсветка / пульсация")]
    public float pulseAmplitude = 0.12f;
    public float pulseSpeed     = 4f;

    private Vector3   _baseScale;
    private bool      _pulsing;
    private Renderer[] _renderers;
    private MaterialPropertyBlock _mpb;

    private static readonly Color PulseColor  = new Color(0.95f, 0.85f, 0.2f);
    private static readonly Color SelectColor = new Color(0.3f, 0.9f, 0.4f);

    private void Awake()
    {
        _baseScale  = transform.localScale;
        _renderers  = GetComponentsInChildren<Renderer>();
        _mpb        = new MaterialPropertyBlock();
    }

    private void OnEnable()  => CoffeeCraftingSystem.Register(this);
    private void OnDisable() => CoffeeCraftingSystem.Unregister(this);

    private void OnMouseDown()
    {
        if (GameInput.Locked) return;
        CoffeeCraftingSystem.Instance?.OnItemClicked(this);
    }

    private void Update()
    {
        if (_pulsing)
        {
            float p = 1f + pulseAmplitude * Mathf.Sin(Time.time * pulseSpeed);
            transform.localScale = _baseScale * p;
        }
    }

    /// <summary>Пульсация + свечение (подсказка/туториал — «нажми меня»).</summary>
    public void SetPulsing(bool on)
    {
        _pulsing = on;
        if (!on) transform.localScale = _baseScale;
        SetEmission(on ? PulseColor : Color.black);
    }

    /// <summary>Короткая вспышка при выборе.</summary>
    public void FlashSelected()
    {
        StopAllCoroutines();
        StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        SetEmission(SelectColor);
        yield return new WaitForSeconds(0.25f);
        if (!_pulsing) SetEmission(Color.black);
    }

    private void SetEmission(Color c)
    {
        if (_renderers == null) return;
        foreach (var r in _renderers)
        {
            if (r == null) continue;
            r.GetPropertyBlock(_mpb);
            _mpb.SetColor("_EmissionColor", c);
            r.SetPropertyBlock(_mpb);
        }
    }

    /// <summary>Совпадает ли предмет с требованием заказа на шаге expected.</summary>
    public bool Matches(CoffeeOrder target, ItemKind expected)
    {
        if (target == null || kind != expected) return false;
        switch (kind)
        {
            case ItemKind.Drink:     return drinkType == target.type;
            case ItemKind.Volume:    return volume    == target.volume;
            case ItemKind.Sweetness: return sweetness == target.sweet;
            case ItemKind.Topping:   return topping   == target.topping;
            case ItemKind.Brew:      return true;
        }
        return false;
    }
}
