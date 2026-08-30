/// <summary>
/// Батч 12: универсальный праздничный поп-ап награды/разблокировки. Строит свой Canvas в КОДЕ
/// (как EngagementPrompt/AdRemovalPrompt) — сборка сцены билдером не нужна. Используется:
///   • разблокировка новой механики (ProgressionManager) — «НОВОЕ ОТКРЫТО»;
///   • выпадение лута/открытие сундука (LootSystem).
/// Иконки пока не грузим в рантайме (спрайты Mini UI вне Resources) — используем цвет+текст;
/// иконки можно добавить позже привязкой в билдере. Текст — через Loc (готовые строки).
/// Сцена: MainScene (создаётся в рантайме). Зависимости: TMPro, AudioController. SDK: нет.
/// </summary>
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RewardPopupUI : MonoBehaviour
{
    public static RewardPopupUI Instance { get; private set; }
    public bool IsShowing { get; private set; }

    private GameObject _panel;
    private Image _accentBar;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _bodyText;
    private TextMeshProUGUI _btnLabel;
    private TMP_FontAsset _font;
    private Action _onClose;
    private Coroutine _autoHideCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static RewardPopupUI Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("RewardPopup");
            Instance = go.AddComponent<RewardPopupUI>();
        }
        return Instance;
    }

    /// <summary>Показывает праздничный поп-ап. accent — цвет полосы-акцента (валюта/тип).
    /// Не блокирует поток — авто-скрытие через autoHide секунд.</summary>
    public void Show(string title, string body, Color accent, float autoHide = 3.5f, Action onClose = null)
    {
        if (_panel == null) Build();

        _onClose = onClose;
        if (_titleText != null) _titleText.text = title;
        if (_bodyText  != null) _bodyText.text  = body;
        if (_accentBar != null) _accentBar.color = accent;

        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        IsShowing = true;
        AudioController.Instance?.PlayStar();

        // Небольшой «поп» масштабом для сочности.
        StartCoroutine(PopScale());

        if (_autoHideCo != null) StopCoroutine(_autoHideCo);
        _autoHideCo = StartCoroutine(AutoHide(Mathf.Max(1.5f, autoHide)));
    }

    private IEnumerator PopScale()
    {
        var tr = _panel.transform;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 6f;
            float s = Mathf.SmoothStep(0.8f, 1f, Mathf.Clamp01(t));
            tr.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        tr.localScale = Vector3.one;
    }

    private void OnClose()
    {
        AudioController.Instance?.PlayClick();
        var cb = _onClose;
        Hide();
        cb?.Invoke();
    }

    private void Hide()
    {
        if (_autoHideCo != null) { StopCoroutine(_autoHideCo); _autoHideCo = null; }
        _onClose = null;
        if (_panel != null) _panel.SetActive(false);
        IsShowing = false;
    }

    private IEnumerator AutoHide(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        var cb = _onClose;
        Hide();
        cb?.Invoke();
    }

    private void Build()
    {
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("RewardCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 330;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = new Vector2(0.32f, 0.60f);
        prt.anchorMax = new Vector2(0.68f, 0.82f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.11f, 0.10f, 0.17f, 0.98f);

        // Полоса-акцент сверху (цвет = тип награды/валюты).
        var bar = new GameObject("Accent", typeof(RectTransform), typeof(Image));
        bar.transform.SetParent(_panel.transform, false);
        var brt = (RectTransform)bar.transform;
        brt.anchorMin = new Vector2(0f, 0.86f); brt.anchorMax = new Vector2(1f, 1f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        _accentBar = bar.GetComponent<Image>();
        _accentBar.color = new Color(0.95f, 0.8f, 0.3f);

        _titleText = MakeText("Title", _panel.transform, new Vector2(0.06f, 0.5f), new Vector2(0.94f, 0.84f), 34);
        _titleText.color = new Color(1f, 0.96f, 0.85f);
        _titleText.fontStyle = FontStyles.Bold;

        _bodyText = MakeText("Body", _panel.transform, new Vector2(0.06f, 0.24f), new Vector2(0.94f, 0.5f), 26);

        _btnLabel = MakeButton("OkBtn", _panel.transform, new Vector2(0.34f, 0.05f), new Vector2(0.66f, 0.22f),
            new Color(0.22f, 0.48f, 0.28f, 1f), OnClose);
        _btnLabel.text = Loc.T("Забрать", "Claim");

        _panel.SetActive(false);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.color = Color.white;
        t.raycastTarget = false; t.enableWordWrapping = true;
        t.enableAutoSizing = true; t.fontSizeMin = 12; t.fontSizeMax = size;
        return t;
    }

    private TextMeshProUGUI MakeButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        var label = MakeText("Label", go.transform, Vector2.zero, Vector2.one, 24);
        return label;
    }
}
