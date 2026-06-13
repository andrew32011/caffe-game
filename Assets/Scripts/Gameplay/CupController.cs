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

    private readonly List<GameObject> _contents = new List<GameObject>();

    public enum Zone { Ingredients, Machine, Toppings, Counter }

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

    /// <summary>Очистка кружки и возврат на стол ингредиентов (пункт 3).</summary>
    public void ResetCup()
    {
        foreach (var c in _contents) if (c != null) Destroy(c);
        _contents.Clear();
        SnapTo(Zone.Ingredients);
    }
}
