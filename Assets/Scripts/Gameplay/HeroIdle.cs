/// <summary>
/// Лёгкая процедурная idle-анимация главного героя: едва заметное «дыхание»
/// (покачивание вверх-вниз) и небольшой перенос веса (наклон). Не требует
/// скелетных клипов — работает на любой модели (у Low Poly Female их нет).
/// Герой — дочерний объект Main Camera; работаем в локальных координатах,
/// поэтому он остаётся в кадре при движении камеры.
/// Сцена: MainScene
/// Зависимости: Нет
/// SDK: Нет
/// </summary>
using UnityEngine;

public class HeroIdle : MonoBehaviour
{
    [Header("Дыхание (вертикальное покачивание)")]
    [SerializeField] private float _bobAmplitude = 0.025f;
    [SerializeField] private float _bobSpeed     = 1.4f;

    [Header("Перенос веса (наклон)")]
    [SerializeField] private float _swayAngle = 1.5f;
    [SerializeField] private float _swaySpeed = 0.7f;

    private Vector3    _basePos;
    private Quaternion _baseRot;
    private float      _seed;

    private void Awake()
    {
        _basePos = transform.localPosition;
        _baseRot = transform.localRotation;
        _seed    = Random.value * 10f;
    }

    private void OnDisable()
    {
        // Возвращаем исходную позу, чтобы не накапливалось смещение
        transform.localPosition = _basePos;
        transform.localRotation = _baseRot;
    }

    private void Update()
    {
        float t = Time.time;
        float bob  = Mathf.Sin(t * _bobSpeed  + _seed) * _bobAmplitude;
        float sway = Mathf.Sin(t * _swaySpeed + _seed) * _swayAngle;

        transform.localPosition = _basePos + new Vector3(0f, bob, 0f);
        transform.localRotation = _baseRot * Quaternion.Euler(0f, 0f, sway);
    }
}
