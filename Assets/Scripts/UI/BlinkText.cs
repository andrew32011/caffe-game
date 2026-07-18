/// <summary>
/// Мигание графики (пульс альфы) — для подсказки «нажмите для продолжения».
/// Сцена: UI. Зависимости: UnityEngine.UI. SDK: нет.
/// </summary>
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Graphic))]
public class BlinkText : MonoBehaviour
{
    [SerializeField] private float _speed = 3f;
    [SerializeField] private float _min = 0.25f;

    private Graphic _g;

    private void Awake() => _g = GetComponent<Graphic>();

    private void OnEnable()
    {
        if (_g == null) _g = GetComponent<Graphic>();
    }

    private void Update()
    {
        if (_g == null) return;
        float a = Mathf.Lerp(_min, 1f, (Mathf.Sin(Time.unscaledTime * _speed) + 1f) * 0.5f);
        var c = _g.color; c.a = a; _g.color = c;
    }
}
