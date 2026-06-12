/// <summary>
/// Визуальные эффекты: дрожь камеры, потеря зрения, красная пульсация,
/// вспышки темноты, восстановление яркости.
/// Используется для вставных сцен между днями.
/// Сцена: MainScene
/// Зависимости: Post-Processing Profile (опционально), Camera
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class VisualEffectsController : MonoBehaviour
{
    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Камера")]
    [SerializeField] private Transform _cameraTransform;

    [Header("UI оверлеи")]
    [SerializeField] private Image _blackOverlay;    // Чёрный оверлей (fade)
    [SerializeField] private Image _redOverlay;      // Красный оверлей
    [SerializeField] private Image _whiteOverlay;    // Белый оверлей (яркость)
    [SerializeField] private Image _coinEffectPrefab;// Монета (опционально)
    [SerializeField] private RectTransform _coinParent; // Canvas для монет

    [Header("Настройки тряски")]
    [SerializeField] private float _defaultShakeMagnitude = 0.15f;
    [SerializeField] private float _defaultShakeDuration  = 0.4f;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private Vector3 _camOriginalPosition;
    private bool    _isShaking = false;

    // ─── Инициализация ───────────────────────────────────────────────────────

    private void Awake()
    {
        if (_cameraTransform == null && Camera.main != null)
            _cameraTransform = Camera.main.transform;

        if (_cameraTransform != null)
            _camOriginalPosition = _cameraTransform.localPosition;

        SetOverlayAlpha(_blackOverlay, 0f);
        SetOverlayAlpha(_redOverlay,   0f);
        SetOverlayAlpha(_whiteOverlay, 0f);
    }

    // ─── Публичное API ───────────────────────────────────────────────────────

    /// <summary>Дрожь камеры.</summary>
    public void ShakeCamera(float duration = -1f, float magnitude = -1f)
    {
        if (duration  < 0) duration  = _defaultShakeDuration;
        if (magnitude < 0) magnitude = _defaultShakeMagnitude;

        if (!_isShaking)
            StartCoroutine(ShakeRoutine(duration, magnitude));
    }

    /// <summary>Эффект монет при оплате.</summary>
    public void PlayCoinEffect(int totalCoins)
    {
        if (_coinEffectPrefab == null || _coinParent == null) return;
        StartCoroutine(CoinPopRoutine(totalCoins));
    }

    /// <summary>Запускает вставную сцену с нужным эффектом и текстом.</summary>
    public IEnumerator PlayVignette(
        VignetteEffectType effectType,
        string text,
        DialogueDisplayer dialogue)
    {
        // Затемнение входа
        yield return StartCoroutine(FadeOverlay(_blackOverlay, 0f, 0.8f, 0.5f));
        yield return new WaitForSeconds(0.3f);

        // Основной эффект
        switch (effectType)
        {
            case VignetteEffectType.CameraShake:
                yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.8f, 0f, 0.4f));
                ShakeCamera(1.2f, 0.25f);
                yield return new WaitForSeconds(1.2f);
                break;

            case VignetteEffectType.VisionLoss:
                yield return StartCoroutine(VisionLossEffect());
                break;

            case VignetteEffectType.RedPulse:
                yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.8f, 0f, 0.4f));
                yield return StartCoroutine(RedPulseEffect(3));
                break;

            case VignetteEffectType.DarknessFlash:
                yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.8f, 0f, 0.2f));
                yield return StartCoroutine(DarknessFlashEffect(4));
                ShakeCamera(2f, 0.3f);
                break;

            case VignetteEffectType.BrightRestore:
                yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.8f, 0f, 0.8f));
                yield return StartCoroutine(FadeOverlay(_whiteOverlay, 0f, 0.5f, 0.5f));
                yield return new WaitForSeconds(0.5f);
                yield return StartCoroutine(FadeOverlay(_whiteOverlay, 0.5f, 0f, 1.2f));
                break;

            default:
                yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.8f, 0f, 0.4f));
                break;
        }

        // Показываем текст вставной сцены
        if (!string.IsNullOrEmpty(text) && dialogue != null)
        {
            var lines = new System.Collections.Generic.List<DialogueLine>
            {
                new DialogueLine { speakerName = "...", text = text, triggerSpeech = false }
            };
            yield return StartCoroutine(dialogue.PlayDialogueLines(lines));
        }

        // Затемнение выхода
        yield return StartCoroutine(FadeOverlay(_blackOverlay, 0f, 0.8f, 0.4f));
        yield return new WaitForSeconds(0.3f);
        yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.8f, 0f, 0.5f));
    }

    // ─── Эффекты ─────────────────────────────────────────────────────────────

    private IEnumerator ShakeRoutine(float duration, float magnitude)
    {
        _isShaking = true;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float remaining = 1f - (elapsed / duration);

            float x = Random.Range(-1f, 1f) * magnitude * remaining;
            float y = Random.Range(-1f, 1f) * magnitude * remaining;

            if (_cameraTransform != null)
                _cameraTransform.localPosition = _camOriginalPosition + new Vector3(x, y, 0f);

            yield return null;
        }

        if (_cameraTransform != null)
            _cameraTransform.localPosition = _camOriginalPosition;

        _isShaking = false;
    }

    private IEnumerator VisionLossEffect()
    {
        // Постепенная потеря зрения
        yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.8f, 0f, 0.2f));

        for (int i = 0; i < 3; i++)
        {
            yield return StartCoroutine(FadeOverlay(_blackOverlay, 0f, 0.7f, 0.3f));
            yield return new WaitForSeconds(0.1f);
            yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.7f, 0f, 0.4f));
            yield return new WaitForSeconds(0.2f);
        }

        yield return StartCoroutine(FadeOverlay(_blackOverlay, 0f, 0.95f, 0.8f));
        yield return new WaitForSeconds(1f);
        yield return StartCoroutine(FadeOverlay(_blackOverlay, 0.95f, 0f, 0.6f));
    }

    private IEnumerator RedPulseEffect(int pulses)
    {
        for (int i = 0; i < pulses; i++)
        {
            yield return StartCoroutine(FadeOverlay(_redOverlay, 0f, 0.45f, 0.25f));
            yield return StartCoroutine(FadeOverlay(_redOverlay, 0.45f, 0f, 0.35f));
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator DarknessFlashEffect(int flashes)
    {
        for (int i = 0; i < flashes; i++)
        {
            yield return StartCoroutine(FadeOverlay(_blackOverlay, 0f, 1f, 0.1f));
            ShakeCamera(0.15f, 0.2f);
            yield return StartCoroutine(FadeOverlay(_blackOverlay, 1f, 0f, 0.2f));
            yield return new WaitForSeconds(0.15f + Random.Range(0f, 0.3f));
        }
    }

    // ─── Утилиты ─────────────────────────────────────────────────────────────

    private IEnumerator FadeOverlay(Image overlay, float from, float to, float duration)
    {
        if (overlay == null) yield break;

        SetOverlayAlpha(overlay, from);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetOverlayAlpha(overlay, Mathf.Lerp(from, to, t));
            yield return null;
        }

        SetOverlayAlpha(overlay, to);
    }

    private void SetOverlayAlpha(Image overlay, float alpha)
    {
        if (overlay == null) return;
        Color c = overlay.color;
        c.a           = alpha;
        overlay.color = c;
        overlay.gameObject.SetActive(alpha > 0.01f);
    }

    private IEnumerator CoinPopRoutine(int totalCoins)
    {
        // Создаём несколько всплывающих монет
        for (int i = 0; i < Mathf.Min(5, totalCoins / 5 + 1); i++)
        {
            if (_coinEffectPrefab != null && _coinParent != null)
            {
                Image coin = Instantiate(_coinEffectPrefab, _coinParent);
                coin.rectTransform.anchoredPosition = new Vector2(
                    Random.Range(-80f, 80f),
                    Random.Range(-40f, 40f));
                StartCoroutine(AnimateCoin(coin.rectTransform));
            }
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator AnimateCoin(RectTransform coin)
    {
        if (coin == null) yield break;

        float elapsed = 0f;
        float duration = 0.8f;
        Vector2 startPos = coin.anchoredPosition;
        Vector2 endPos   = startPos + Vector2.up * 100f;
        CanvasGroup cg   = coin.GetComponent<CanvasGroup>();

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            coin.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            if (cg != null) cg.alpha = 1f - t;
            yield return null;
        }

        Destroy(coin.gameObject);
    }
}
