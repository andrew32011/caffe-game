/// <summary>
/// Кликабельный 3D-предмет на столе. Два вида:
///   Ingredient — сосуд из Ingridients1 (основа напитка), при клике «выливается» в кружку;
///   Topping    — предмет из ShelfItems, при клике уменьшенная копия падает в кружку.
/// Требует Collider. Клик ловится через OnMouseDown (нужна Camera.main).
/// Объём и температуру задаёт не предмет, а минигейм машины (MachineMinigame).
/// Сцена: MainScene
/// Зависимости: CoffeeCraftingSystem, GameInput, GameEnums
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class IngredientItem : MonoBehaviour
{
    public enum ItemKind { Ingredient, Topping }

    [Header("Что это за предмет")]
    public ItemKind kind = ItemKind.Ingredient;
    [Tooltip("Индекс основы (0..N-1, как порядок детей Ingridients1).")]
    public int ingredientIndex;
    [Tooltip("Какой топпинг даёт этот предмет (для kind = Topping).")]
    public Topping topping;

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

    public void SetPulsing(bool on)
    {
        _pulsing = on;
        if (!on) transform.localScale = _baseScale;
        SetEmission(on ? PulseColor : Color.black);
    }

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
}
