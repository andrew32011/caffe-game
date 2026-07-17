/// <summary>
/// Лёгкая «сочность» кнопки: дыхание масштаба, качание, блеск (яркость) и подскок при клике.
/// Вешается билдером на важные кнопки. Всё на unscaled-времени (работает и на паузе меню).
/// Сцена: UI. Зависимости: UnityEngine.UI, EventSystems. SDK: нет.
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class ButtonJuice : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private bool  _idlePulse  = true;   // дыхание масштаба
    [SerializeField] private float _pulseAmount = 0.04f;
    [SerializeField] private float _pulseSpeed  = 2.5f;
    [SerializeField] private bool  _wobble      = false; // лёгкое качание (±град)
    [SerializeField] private float _wobbleDeg   = 1.8f;
    [SerializeField] private bool  _shine       = false; // периодический блеск (яркость)
    [SerializeField] private bool  _pressPunch  = true;  // подскок при клике

    private RectTransform _rt;
    private Vector3 _baseScale;
    private Graphic _graphic;
    private Color   _baseColor;
    private float   _punch;
    private float   _phase;

    private void Awake()
    {
        _rt = (RectTransform)transform;
        _baseScale = _rt.localScale;
        _graphic = GetComponent<Graphic>();
        if (_graphic != null) _baseColor = _graphic.color;
        _phase = Random.value * 6.283f; // рассинхронизируем разные кнопки
    }

    private void OnDisable()
    {
        if (_rt != null) { _rt.localScale = _baseScale; _rt.localRotation = Quaternion.identity; }
        if (_graphic != null) _graphic.color = _baseColor;
    }

    private void Update()
    {
        float t = Time.unscaledTime * _pulseSpeed + _phase;

        float s = 1f;
        if (_idlePulse) s += Mathf.Sin(t) * _pulseAmount;
        if (_punch > 0.001f) { s += _punch; _punch = Mathf.Lerp(_punch, 0f, Time.unscaledDeltaTime * 8f); }
        _rt.localScale = _baseScale * s;

        if (_wobble)
            _rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 0.8f) * _wobbleDeg);

        if (_shine && _graphic != null)
        {
            float b = 1f + Mathf.Max(0f, Mathf.Sin(t * 0.7f)) * 0.25f; // мягкая вспышка яркости
            _graphic.color = new Color(_baseColor.r * b, _baseColor.g * b, _baseColor.b * b, _baseColor.a);
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (_pressPunch) _punch = 0.12f;
    }

    /// <summary>Настройка из билдера.</summary>
    public void Configure(bool pulse, bool shine, bool wobble)
    {
        _idlePulse = pulse; _shine = shine; _wobble = wobble;
    }
}
