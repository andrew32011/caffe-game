/// <summary>
/// Полоска удовлетворённости гостя в мировом пространстве.
/// Прикрепляется как дочерний объект к гостю.
/// Сцена: MainScene (создаётся динамически)
/// Зависимости: Canvas (World Space), Image (Filled)
/// SDK: Нет
/// </summary>
using UnityEngine;
using UnityEngine.UI;

public class SatisfactionBar : MonoBehaviour
{
    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("UI элементы")]
    [SerializeField] private Image _fillImage;           // Image с type = Filled
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _iconImage;           // Смайл/иконка настроения (опционально)

    [Header("Цвета")]
    [SerializeField] private Gradient _fillGradient;     // От зелёного к красному

    [Header("Смайлы (опционально)")]
    [SerializeField] private Sprite _happyIcon;
    [SerializeField] private Sprite _neutralIcon;
    [SerializeField] private Sprite _sadIcon;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private float _currentValue = 1f; // 0..1
    private Camera _mainCamera;

    // ─── Жизненный цикл ──────────────────────────────────────────────────────

    private void Awake()
    {
        _mainCamera = Camera.main;

        // Если не задан градиент — создаём дефолтный (зелёный → жёлтый → красный)
        if (_fillGradient == null || _fillGradient.colorKeys.Length < 2)
        {
            _fillGradient = new Gradient();
            _fillGradient.SetKeys(
                new GradientColorKey[]
                {
                    new GradientColorKey(new Color(0.9f, 0.15f, 0.15f), 0f),   // Красный при 0%
                    new GradientColorKey(new Color(1f, 0.85f, 0.1f),   0.5f),  // Жёлтый при 50%
                    new GradientColorKey(new Color(0.2f, 0.85f, 0.2f), 1f)     // Зелёный при 100%
                },
                new GradientAlphaKey[]
                {
                    new GradientAlphaKey(1f, 0f),
                    new GradientAlphaKey(1f, 1f)
                }
            );
        }
    }

    private void LateUpdate()
    {
        // Всегда смотрим на камеру (billboard)
        if (_mainCamera != null)
        {
            transform.LookAt(
                transform.position + _mainCamera.transform.rotation * Vector3.forward,
                _mainCamera.transform.rotation * Vector3.up);
        }
    }

    // ─── Публичное API ───────────────────────────────────────────────────────

    /// <summary>Устанавливает значение полоски (0..1).</summary>
    public void SetValue(float value01)
    {
        _currentValue = Mathf.Clamp01(value01);

        if (_fillImage != null)
        {
            _fillImage.fillAmount = _currentValue;
            _fillImage.color      = _fillGradient.Evaluate(_currentValue);
        }

        UpdateIcon();
    }

    /// <summary>Текущее значение (0..1).</summary>
    public float Value => _currentValue;

    // ─── Вспомогательные ─────────────────────────────────────────────────────

    private void UpdateIcon()
    {
        if (_iconImage == null) return;

        if (_currentValue > 0.6f && _happyIcon  != null) _iconImage.sprite = _happyIcon;
        else if (_currentValue > 0.3f && _neutralIcon != null) _iconImage.sprite = _neutralIcon;
        else if (_sadIcon != null) _iconImage.sprite = _sadIcon;
    }

    // ─── Анимация появления ───────────────────────────────────────────────────

    private void OnEnable()
    {
        // Маленькая анимация появления
        transform.localScale = Vector3.zero;
        StartCoroutine(PopIn());
    }

    private System.Collections.IEnumerator PopIn()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 5f;
            transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
            yield return null;
        }
        transform.localScale = Vector3.one;
    }
}
