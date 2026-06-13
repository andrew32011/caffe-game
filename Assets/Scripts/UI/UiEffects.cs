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
        Vector2 origin = new Vector2(0f, -60f); // центр-низ

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("CoinFx", typeof(RectTransform), typeof(Image), typeof(CanvasGroup));
            go.transform.SetParent(Root, false);
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(48, 48);
            rt.anchoredPosition = origin;
            var img = go.GetComponent<Image>();
            img.sprite = _coinSprite; img.preserveAspect = true; img.raycastTarget = false;
            Vector2 vel = new Vector2(Random.Range(-220f, 220f), Random.Range(350f, 620f));
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
        rt.anchoredPosition = new Vector2(0f, 40f);
        var tmp = go.GetComponent<TextMeshProUGUI>();
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

    // ─── Баннер конца дня ─────────────────────────────────────────────────────

    public void DayEndBanner(string text)
    {
        var go = new GameObject("DayBannerFx", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(CanvasGroup));
        go.transform.SetParent(Root, false);
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(900, 160);
        rt.anchoredPosition = Vector2.zero;
        var tmp = go.GetComponent<TextMeshProUGUI>();
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
}
