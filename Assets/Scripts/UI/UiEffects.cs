/// <summary>
/// Казуальные 2D UI-эффекты достижений (пункт 5): фонтан монет с «+N», всплывающий
/// текст, баннер конца дня. Синглтон — зовётся откуда угодно: UiEffects.Instance?.X().
/// Сцена: MainScene (UI на Canvas)
/// Зависимости: UnityEngine.UI, TMPro
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UiEffects : MonoBehaviour
{
    public static UiEffects Instance { get; private set; }

    [Header("Корень для спавна эффектов (Canvas)")]
    [SerializeField] private RectTransform _root;
    [Header("Спрайт монеты")]
    [SerializeField] private Sprite _coinSprite;
    [Header("Спрайты празднования (Батч 1)")]
    [SerializeField] private Sprite _starSprite;
    [SerializeField] private Sprite _heartSprite;
    [Header("Шрифт UI")]
    [SerializeField] private TMP_FontAsset _font;

    private void Awake() { Instance = this; }

    private RectTransform Root => _root != null ? _root : (RectTransform)transform;

    // ─── Фонтан монет + «+N» (успешный кофе, деньги) ─────────────────────────

    public void CoinBurst(int amount)
    {
        StartCoroutine(CoinBurstRoutine(amount));
        FloatingText("+" + amount, new Color(1f, 0.85f, 0.25f));
    }

    private IEnumerator CoinBurstRoutine(int amount)
    {
        if (_coinSprite == null) yield break;
        int count = Mathf.Clamp(amount / 10 + 3, 3, 12);
        // Пункт 4: монеты летят в верхнем-левом углу (к кассе), а НЕ в центре-низу —
        // чтобы не перекрывать диалог (низ) и заказ/шкалу (центр-верх).
        Vector2 origin = new Vector2(-760f, 300f);

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("CoinFx", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(Root, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(48, 48);
            rt.anchoredPosition = origin;
            var img = go.GetComponent<Image>();
            img.sprite = _coinSprite; img.preserveAspect = true; img.raycastTarget = false;
            // Небольшой разлёт вверх, не уезжая в центр экрана.
            Vector2 vel = new Vector2(Random.Range(-120f, 160f), Random.Range(300f, 520f));
            StartCoroutine(FlyCoin(rt, go.GetComponent<CanvasGroup>(), vel));
            yield return new WaitForSeconds(0.04f);
        }
    }

    private IEnumerator FlyCoin(RectTransform rt, CanvasGroup cg, Vector2 vel)
    {
        float t = 0f, life = 1.1f;
        while (t < life)
        {
            t += Time.deltaTime;
            vel.y -= 900f * Time.deltaTime; // гравитация
            rt.anchoredPosition += vel * Time.deltaTime;
            rt.Rotate(0f, 0f, 360f * Time.deltaTime);
            if (cg != null) cg.alpha = 1f - (t / life);
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    // ─── Всплывающий текст ────────────────────────────────────────────────────

    public void FloatingText(string text, Color color)
    {
        var go = new GameObject("FloatTextFx", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        go.transform.SetParent(Root, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(400, 90);
        // Пункт 4: «+N» всплывает в верхнем-левом углу у кассы, не на тексте/диалоге.
        rt.anchoredPosition = new Vector2(-700f, 300f);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = text; tmp.fontSize = 56; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color; tmp.fontStyle = FontStyles.Bold; tmp.raycastTarget = false;
        StartCoroutine(FloatRoutine(rt, go.GetComponent<CanvasGroup>()));
    }

    private IEnumerator FloatRoutine(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, life = 1.3f;
        Vector2 start = rt.anchoredPosition;
        while (t < life)
        {
            t += Time.deltaTime;
            float k = t / life;
            rt.anchoredPosition = start + Vector2.up * (120f * k);
            rt.localScale = Vector3.one * Mathf.Lerp(0.6f, 1.1f, Mathf.Min(1f, k * 3f));
            if (cg != null) cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    // ─── Празднование подачи: звёзды + сердечки + надпись (Батч 1) ─────────────

    public void Celebrate(int stars)
    {
        stars = Mathf.Clamp(stars, 1, 3);
        StartCoroutine(CelebrateRoutine(stars));
        string label = stars == 3 ? Loc.T("Идеально!", "Perfect!")
            : stars == 2 ? Loc.T("Отлично!", "Great!")
            : Loc.T("Готово!", "Done!");
        Color col = stars == 3 ? new Color(1f, 0.85f, 0.25f)
            : stars == 2 ? new Color(0.6f, 1f, 0.6f) : Color.white;
        FloatingText(label, col);
    }

    private IEnumerator CelebrateRoutine(int stars)
    {
        // Ряд звёзд по центру-верху, с поп-анимацией и звоном.
        float spacing = 130f;
        float startX = -spacing * (stars - 1) / 2f;
        for (int i = 0; i < stars; i++)
        {
            if (_starSprite != null)
            {
                var go = new GameObject("StarFx", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                go.transform.SetParent(Root, false);
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(110, 110);
                rt.anchoredPosition = new Vector2(startX + i * spacing, 150f);
                var img = go.GetComponent<Image>();
                img.sprite = _starSprite; img.preserveAspect = true; img.raycastTarget = false;
                StartCoroutine(PopStar(rt, go.GetComponent<CanvasGroup>()));
            }
            AudioController.Instance?.PlayStar();
            yield return new WaitForSeconds(0.18f);
        }

        // 3 звезды — дополнительный фонтан сердечек.
        if (stars >= 3 && _heartSprite != null)
        {
            for (int i = 0; i < 6; i++)
            {
                var go = new GameObject("HeartFx", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
                go.transform.SetParent(Root, false);
                var rt = (RectTransform)go.transform;
                rt.sizeDelta = new Vector2(60, 60);
                rt.anchoredPosition = new Vector2(Random.Range(-120f, 120f), 60f);
                var img = go.GetComponent<Image>();
                img.sprite = _heartSprite; img.preserveAspect = true; img.raycastTarget = false;
                StartCoroutine(FloatHeart(rt, go.GetComponent<CanvasGroup>()));
                yield return new WaitForSeconds(0.05f);
            }
        }
    }

    private IEnumerator PopStar(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 5f; rt.localScale = Vector3.one * Mathf.SmoothStep(0.1f, 1.15f, Mathf.Min(1f, t)); yield return null; }
        yield return new WaitForSeconds(0.9f);
        t = 0f;
        while (t < 1f) { t += Time.deltaTime * 2.5f; if (cg != null) cg.alpha = 1f - t; rt.localScale = Vector3.one * (1.15f - 0.3f * t); yield return null; }
        Destroy(rt.gameObject);
    }

    private IEnumerator FloatHeart(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f, life = 1.2f;
        Vector2 start = rt.anchoredPosition;
        float drift = Random.Range(-40f, 40f);
        while (t < life)
        {
            t += Time.deltaTime;
            float k = t / life;
            rt.anchoredPosition = start + new Vector2(drift * k, 220f * k);
            if (cg != null) cg.alpha = 1f - k;
            yield return null;
        }
        Destroy(rt.gameObject);
    }

    // ─── Баннер конца дня ─────────────────────────────────────────────────────

    public void DayEndBanner(string text)
    {
        var go = new GameObject("DayBannerFx", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        go.transform.SetParent(Root, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(900, 160);
        // Пункт 4: баннер дня — выше центра, чтобы не лёг поверх панели итогов дня.
        rt.anchoredPosition = new Vector2(0f, 360f);
        var tmp = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) tmp.font = _font;
        tmp.text = text; tmp.fontSize = 80; tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.95f, 0.7f); tmp.fontStyle = FontStyles.Bold; tmp.raycastTarget = false;
        StartCoroutine(BannerRoutine(rt, go.GetComponent<CanvasGroup>()));
    }

    private IEnumerator BannerRoutine(RectTransform rt, CanvasGroup cg)
    {
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 3f; rt.localScale = Vector3.one * Mathf.SmoothStep(0.2f, 1f, t); yield return null; }
        yield return new WaitForSeconds(1.2f);
        t = 0f;
        while (t < 1f) { t += Time.deltaTime * 2f; if (cg != null) cg.alpha = 1f - t; yield return null; }
        Destroy(rt.gameObject);
    }

    // ─── Индикатор комбо (Батч 3) ─────────────────────────────────────────────
    // Постоянная надпись «Комбо ×N!» у верха экрана: растёт с серией, прячется при сбросе.

    private TextMeshProUGUI _comboLabel;
    private Coroutine _comboCo;

    public void ShowCombo(int count)
    {
        if (count < 2)
        {
            if (_comboLabel != null) _comboLabel.gameObject.SetActive(false);
            return;
        }

        if (_comboLabel == null)
        {
            var go = new GameObject("ComboLabelFx", typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(Root, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(600, 100);
            rt.anchoredPosition = new Vector2(0f, 250f);
            _comboLabel = go.GetComponent<TextMeshProUGUI>();
            if (_font != null) _comboLabel.font = _font;
            _comboLabel.fontSize = 60;
            _comboLabel.alignment = TextAlignmentOptions.Center;
            _comboLabel.fontStyle = FontStyles.Bold;
            _comboLabel.raycastTarget = false;
        }

        _comboLabel.gameObject.SetActive(true);
        _comboLabel.text = Loc.T($"Комбо ×{count}!", $"Combo ×{count}!");
        _comboLabel.color = new Color(1f, 0.7f, 0.2f);
        if (_comboCo != null) StopCoroutine(_comboCo);
        _comboCo = StartCoroutine(ComboPop(_comboLabel.rectTransform));
    }

    private IEnumerator ComboPop(RectTransform rt)
    {
        float t = 0f;
        while (t < 1f) { t += Time.deltaTime * 6f; rt.localScale = Vector3.one * Mathf.SmoothStep(0.6f, 1.2f, Mathf.Min(1f, t)); yield return null; }
        t = 0f;
        while (t < 1f) { t += Time.deltaTime * 4f; rt.localScale = Vector3.one * (1.2f - 0.2f * t); yield return null; }
        rt.localScale = Vector3.one;

        // Держим надпись пару секунд, затем плавно гасим и прячем — чтобы «Комбо xN»
        // не висело постоянно после получения комбо. Таймер перезапускается на каждом
        // новом комбо (StopCoroutine/StartCoroutine в ShowCombo), при сбросе серии
        // ShowCombo(count<2) прячет надпись сразу.
        yield return new WaitForSeconds(2f);
        if (_comboLabel != null)
        {
            Color c0 = _comboLabel.color;
            float f = 0f;
            while (f < 1f)
            {
                f += Time.unscaledDeltaTime * 2f;
                _comboLabel.color = new Color(c0.r, c0.g, c0.b, Mathf.Lerp(1f, 0f, f));
                yield return null;
            }
            _comboLabel.gameObject.SetActive(false);
            _comboLabel.color = new Color(c0.r, c0.g, c0.b, 1f); // вернуть непрозрачность на будущее
        }
    }
}
