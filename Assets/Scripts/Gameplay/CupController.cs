/// <summary>
/// Кружка (вешается на объект PlayerCup). Стоит на своём месте и НЕ привязана к
/// камере. По ходу готовки переезжает между якорями зон:
///   ингредиенты → машина → топпинги → стойка (выдача).
/// Ингредиент «выливается» в кружку (сосуд наклоняется к ней и возвращается),
/// топпинг падает в кружку уменьшенной копией и остаётся.
/// Сцена: MainScene
/// Зависимости: IngredientItem
/// SDK: Нет
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CupController : MonoBehaviour
{
    [Header("Якоря зон (куда переезжает кружка)")]
    [SerializeField] private Transform _ingredientsAnchor;
    [SerializeField] private Transform _machineAnchor;
    [SerializeField] private Transform _toppingsAnchor;
    [SerializeField] private Transform _counterAnchor;

    [Header("Точка внутри кружки для топпингов")]
    [SerializeField] private Transform _contentAnchor;

    [Header("Скорости")]
    [SerializeField] private float _moveSpeed   = 4f;
    [SerializeField] private float _pourDuration = 0.6f;
    [SerializeField] private float _toppingScale = 0.35f;

    [Header("Передача кружки клиенту (пункт 3)")]
    [Tooltip("Высота, на которой кружка «зависает» в руке гостя перед исчезновением.")]
    [SerializeField] private float _handoffHeight = 1.0f;
    [SerializeField] private float _handoffDuration = 0.9f;

    private readonly List<GameObject> _contents = new List<GameObject>();

    private Vector3 _baseScale = Vector3.one;
    private bool    _baseScaleCaptured;

    public enum Zone { Ingredients, Machine, Toppings, Counter }

    private void Awake()
    {
        _baseScale = transform.localScale;
        _baseScaleCaptured = true;
    }

    private Transform AnchorFor(Zone z)
    {
        switch (z)
        {
            case Zone.Ingredients: return _ingredientsAnchor;
            case Zone.Machine:     return _machineAnchor;
            case Zone.Toppings:    return _toppingsAnchor;
            case Zone.Counter:     return _counterAnchor;
        }
        return null;
    }

    /// <summary>Мгновенно ставит кружку на якорь зоны.</summary>
    public void SnapTo(Zone zone)
    {
        var a = AnchorFor(zone);
        if (a != null) { transform.position = a.position; transform.rotation = a.rotation; }
    }

    /// <summary>Плавно перевозит кружку на якорь зоны. Awaitable.</summary>
    public IEnumerator MoveTo(Zone zone)
    {
        var a = AnchorFor(zone);
        if (a == null) yield break;

        while (Vector3.Distance(transform.position, a.position) > 0.02f)
        {
            transform.position = Vector3.MoveTowards(transform.position, a.position, _moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, a.rotation, 180f * Time.deltaTime);
            yield return null;
        }
        transform.position = a.position;
        transform.rotation = a.rotation;
    }

    /// <summary>Сосуд наклоняется к кружке (наливание) и возвращается на место.</summary>
    public IEnumerator PourIngredient(IngredientItem vessel)
    {
        if (vessel == null) yield break;

        Transform v = vessel.transform;
        Vector3 startPos = v.position;
        Quaternion startRot = v.rotation;

        // Подносим к кружке и наклоняем
        Vector3 nearCup = transform.position + Vector3.up * 0.25f + (startPos - transform.position).normalized * 0.25f;
        Quaternion tilt = startRot * Quaternion.Euler(0f, 0f, 110f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, _pourDuration);
            v.position = Vector3.Lerp(startPos, nearCup, t);
            v.rotation = Quaternion.Slerp(startRot, tilt, t);
            yield return null;
        }
        yield return new WaitForSeconds(0.2f);

        // Возвращаем на место
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, _pourDuration);
            v.position = Vector3.Lerp(nearCup, startPos, t);
            v.rotation = Quaternion.Slerp(tilt, startRot, t);
            yield return null;
        }
        v.position = startPos;
        v.rotation = startRot;
    }

    /// <summary>Уменьшенная копия топпинга падает в кружку и остаётся.</summary>
    public void DropTopping(IngredientItem topping)
    {
        if (topping == null) return;

        var clone = Instantiate(topping.gameObject);
        foreach (var c in clone.GetComponentsInChildren<Collider>())       Destroy(c);
        foreach (var s in clone.GetComponentsInChildren<IngredientItem>()) Destroy(s);

        Transform target = _contentAnchor != null ? _contentAnchor : transform;
        StartCoroutine(DropRoutine(clone, topping.transform.position, target));
    }

    private IEnumerator DropRoutine(GameObject clone, Vector3 from, Transform target)
    {
        Vector3 startScale = clone.transform.localScale;
        Vector3 endScale   = startScale * _toppingScale;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 0.4f;
            clone.transform.position   = Vector3.Lerp(from, target.position, t);
            clone.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }
        clone.transform.SetParent(target, true);
        _contents.Add(clone);
    }

    /// <summary>
    /// Пункт 3: кружка мягко переходит от руки ГГ (текущая позиция у стойки) к руке
    /// гостя и исчезает там. Содержимое уезжает вместе с ней. Awaitable.
    /// Новая кружка появится для следующего гостя в ResetCup().
    /// </summary>
    public IEnumerator HandToCustomer(Transform customer)
    {
        if (customer == null) { SetVisible(false); yield break; }

        Vector3 startPos  = transform.position;
        // Точка «в руке гостя»: чуть выше его основания и слегка в сторону стойки.
        Vector3 handPos   = customer.position + Vector3.up * _handoffHeight
                            + (startPos - customer.position).normalized * 0.15f;
        Vector3 startScale = transform.localScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, _handoffDuration);
            float e = Mathf.SmoothStep(0f, 1f, t);
            transform.position   = Vector3.Lerp(startPos, handPos, e);
            // К концу пути кружка чуть уменьшается и тает.
            transform.localScale = Vector3.Lerp(startScale, startScale * 0.6f, e);
            yield return null;
        }

        // Исчезает в руке гостя.
        SetVisible(false);
        ClearContents();
    }

    /// <summary>Показать/скрыть визуал кружки (все рендеры в иерархии).</summary>
    public void SetVisible(bool on)
    {
        foreach (var r in GetComponentsInChildren<Renderer>(true))
            r.enabled = on;
    }

    private void ClearContents()
    {
        foreach (var c in _contents) if (c != null) Destroy(c);
        _contents.Clear();
    }

    /// <summary>Очистка кружки и возврат на стол ингредиентов (пункт 3).
    /// Здесь же «создаётся» новая кружка для следующего гостя: визуал снова виден,
    /// масштаб восстановлен.</summary>
    public void ResetCup()
    {
        ClearContents();
        if (_baseScaleCaptured) transform.localScale = _baseScale;
        SetVisible(true);
        SnapTo(Zone.Ingredients);
    }
}
