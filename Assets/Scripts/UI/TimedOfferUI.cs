/// <summary>
/// Батч 13: таймовый оффер после рекламы (крючок конверсии, дефицит времени). Короткая
/// всплывашка внизу экрана со «Стартовым набором» и обратным отсчётом — показывается ОДИН
/// раз за сессию (после межстраничной рекламы, если реклама ещё не отключена). Строит свой
/// Canvas в КОДЕ (как AdRemovalPrompt). Покупка → GameManager.BuyGems(starter_pack).
///
/// ⚠️ Товар starter_pack завести в консоли Яндекс Игр (уже нужен Батчем 12).
/// Сцена: MainScene (рантайм). Зависимости: GameManager, Loc, TMPro, AudioController. SDK: YG2 Payments.
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TimedOfferUI : MonoBehaviour
{
    public static TimedOfferUI Instance { get; private set; }

    private static bool _shownThisSession; // не назойливо: один оффер за сессию

    [SerializeField] private float _seconds = 60f;

    private GameObject _panel;
    private TextMeshProUGUI _timerText;
    private TMP_FontAsset _font;
    private Coroutine _countdownCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static TimedOfferUI Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("TimedOffer");
            Instance = go.AddComponent<TimedOfferUI>();
        }
        return Instance;
    }

    /// <summary>Показать оффер один раз за сессию. Ничего не делает, если реклама уже отключена.</summary>
    public void ShowOffer()
    {
        if (_shownThisSession) return;
        if (YG.YG2.saves != null && YG.YG2.saves.adsDisabled) return; // стартовый набор бесполезен
        _shownThisSession = true;

        if (_panel == null) Build();
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        AudioController.Instance?.PlayBonus();

        if (_countdownCo != null) StopCoroutine(_countdownCo);
        _countdownCo = StartCoroutine(Countdown());
    }

    private void Hide()
    {
        if (_countdownCo != null) { StopCoroutine(_countdownCo); _countdownCo = null; }
        if (_panel != null) _panel.SetActive(false);
    }

    private IEnumerator Countdown()
    {
        float t = _seconds;
        int lastShown = -1;
        while (t > 0f)
        {
            // Перф: обновляем подпись (аллокация строки + rebuild меша TMP) только при
            // смене целой секунды, а не каждый кадр.
            int sec = Mathf.CeilToInt(t);
            if (sec != lastShown && _timerText != null)
            {
                lastShown = sec;
                _timerText.text = Loc.T($"Осталось {sec} с", $"{sec}s left");
            }
            t -= Time.unscaledDeltaTime;
            yield return null;
        }
        Hide();
    }

    private void OnBuy()
    {
        AudioController.Instance?.PlayClick();
        GameManager.Instance?.BuyGems(GameManager.StarterPackId);
        Hide();
    }

    private void Build()
    {
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("TimedOfferCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 305;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = new Vector2(0.26f, 0.03f);
        prt.anchorMax = new Vector2(0.74f, 0.20f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.12f, 0.10f, 0.18f, 0.97f);

        MakeText("Title", _panel.transform, new Vector2(0.03f, 0.62f), new Vector2(0.97f, 0.98f), 25,
            Loc.T("Только сейчас: Стартовый набор!", "Now only: Starter pack!"), new Color(1f, 0.92f, 0.7f), FontStyles.Bold);
        MakeText("Desc", _panel.transform, new Vector2(0.03f, 0.40f), new Vector2(0.66f, 0.62f), 18,
            Loc.T("100 кристаллов + 2000 монет + без рекламы", "100 gems + 2000 coins + no ads"), new Color(0.9f, 0.92f, 1f), FontStyles.Normal);
        _timerText = MakeText("Timer", _panel.transform, new Vector2(0.03f, 0.05f), new Vector2(0.40f, 0.38f), 20,
            "", new Color(1f, 0.7f, 0.5f), FontStyles.Bold);

        var buyLabel = MakeButton("BuyBtn", _panel.transform, new Vector2(0.44f, 0.10f), new Vector2(0.80f, 0.55f),
            new Color(0.22f, 0.48f, 0.28f, 1f), OnBuy);
        buyLabel.text = Loc.T("Забрать набор", "Grab it");

        var closeLabel = MakeButton("CloseBtn", _panel.transform, new Vector2(0.82f, 0.10f), new Vector2(0.97f, 0.55f),
            new Color(0.28f, 0.24f, 0.30f, 1f), Hide);
        closeLabel.text = Loc.T("Позже", "Later");

        _panel.SetActive(false);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size,
        string content, Color color, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = content; t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.color = color;
        t.fontStyle = style; t.raycastTarget = false; t.enableWordWrapping = true;
        t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = size;
        return t;
    }

    private TextMeshProUGUI MakeButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg, System.Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        return MakeText("Label", go.transform, Vector2.zero, Vector2.one, 22, "", Color.white, FontStyles.Normal);
    }
}
