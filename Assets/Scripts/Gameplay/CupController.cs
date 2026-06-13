/// <summary>
/// Кружка игрока. Прикреплена к камере (дочерний объект) и ездит с игроком
/// между локациями. Выбранные предметы «влетают» в кружку. После выдачи
/// клиенту содержимое сбрасывается (кружка появляется пустой у первого стола).
/// Сцена: MainScene (создаётся под Main Camera)
/// Зависимости: IngredientItem
/// SDK: Нет
/// </summary>
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CupController : MonoBehaviour
{
    [Header("Куда складываются предметы (точка внутри кружки)")]
    [SerializeField] private Transform _contentAnchor;
    [SerializeField] private float _flyDuration = 0.45f;
    [SerializeField] private float _contentScale = 0.4f;

    private readonly List<GameObject> _contents = new List<GameObject>();

    /// <summary>Клон выбранного предмета «влетает» в кружку.</summary>
    public void AddItemVisual(IngredientItem source)
    {
        if (source == null) return;

        var clone = Instantiate(source.gameObject);
        Strip(clone);
        StartCoroutine(FlyIn(clone, source.transform.position));
    }

    private void Strip(GameObject go)
    {
        foreach (var c in go.GetComponentsInChildren<Collider>())      Destroy(c);
        foreach (var s in go.GetComponentsInChildren<IngredientItem>()) Destroy(s);
    }

    private IEnumerator FlyIn(GameObject clone, Vector3 from)
    {
        Transform target = _contentAnchor != null ? _contentAnchor : transform;
        Vector3 startScale = clone.transform.localScale;
        Vector3 endScale   = startScale * _contentScale;

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, _flyDuration);
            clone.transform.position   = Vector3.Lerp(from, target.position, t);
            clone.transform.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        clone.transform.SetParent(target, true);
        _contents.Add(clone);
    }

    /// <summary>Очищает кружку (после выдачи / при сбросе заказа).</summary>
    public void ResetCup()
    {
        foreach (var c in _contents)
            if (c != null) Destroy(c);
        _contents.Clear();
    }
}
