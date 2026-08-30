/// <summary>
/// Батч 13: магазин кристаллов (замыкает монетизацию Батч 12-B). Строит свой Canvas в КОДЕ
/// (как RewardPopupUI/EngagementPrompt) — сборка сцены билдером не нужна. Четыре товара
/// зовут GameManager.BuyGems(id) → YG2 Payments; начисление и «спасибо» обрабатывает
/// GameManager.OnGemPurchase. Открывается тапом по строке «Кристаллы» в валютном HUD
/// и кнопкой «Магазин кристаллов» в настройках (обе показываются лишь после разблокировки).
///
/// ⚠️ Товары gems_small/gems_medium/gems_large/starter_pack завести в консоли Яндекс Игр.
/// Сцена: MainScene (рантайм). Зависимости: GameManager, Loc, TMPro, AudioController. SDK: YG2 Payments.
/// </summary>
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GemShopUI : MonoBehaviour
{
    public static GemShopUI Instance { get; private set; }

    private GameObject _panel;
    private TMP_FontAsset _font;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public static GemShopUI Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("GemShop");
            Instance = go.AddComponent<GemShopUI>();
        }
        return Instance;
    }

    public void Open()
    {
        if (_panel == null) Build();
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        AudioController.Instance?.PlayUiOpen();
    }

    public void Close()
    {
        AudioController.Instance?.PlayUiClose();
        if (_panel != null) _panel.SetActive(false);
    }

    private void Buy(string productId)
    {
        AudioController.Instance?.PlayClick();
        GameManager.Instance?.BuyGems(productId);
        Close(); // окно покупки Яндекса открывается поверх; результат придёт в OnGemPurchase
    }

    private void Build()
    {
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("GemShopCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Затемняющая подложка (клик мимо — закрыть).
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dim.transform.SetParent(canvasGo.transform, false);
        SetFull((RectTransform)dim.transform);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        dim.GetComponent<Button>().onClick.AddListener(Close);

        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = new Vector2(0.28f, 0.16f);
        prt.anchorMax = new Vector2(0.72f, 0.86f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.10f, 0.10f, 0.17f, 0.98f);

        MakeText("Title", _panel.transform, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.98f), 34,
            Loc.T("Магазин кристаллов", "Gem shop"), new Color(0.6f, 0.85f, 1f), FontStyles.Bold);

        // Стартовый набор — выделенный оффер сверху (лучшая ценность + без рекламы).
        MakeOffer(new Vector2(0.06f, 0.70f), new Vector2(0.94f, 0.85f), new Color(0.22f, 0.40f, 0.55f, 1f),
            Loc.T("Стартовый набор", "Starter pack"),
            Loc.T("100 кристаллов + 2000 монет + без рекламы", "100 gems + 2000 coins + no ads"),
            GameManager.StarterPackId);

        MakeOffer(new Vector2(0.06f, 0.545f), new Vector2(0.94f, 0.685f), new Color(0.18f, 0.30f, 0.45f, 1f),
            Loc.T("Горсть кристаллов", "Handful of gems"), Loc.T("50 кристаллов", "50 gems"), GameManager.GemsSmallId);

        MakeOffer(new Vector2(0.06f, 0.39f), new Vector2(0.94f, 0.53f), new Color(0.18f, 0.30f, 0.45f, 1f),
            Loc.T("Мешочек кристаллов", "Sack of gems"), Loc.T("170 кристаллов", "170 gems"), GameManager.GemsMediumId);

        MakeOffer(new Vector2(0.06f, 0.235f), new Vector2(0.94f, 0.375f), new Color(0.18f, 0.30f, 0.45f, 1f),
            Loc.T("Сундук кристаллов", "Chest of gems"), Loc.T("500 кристаллов", "500 gems"), GameManager.GemsLargeId);

        var closeLabel = MakeButton("CloseBtn", _panel.transform, new Vector2(0.33f, 0.05f), new Vector2(0.67f, 0.17f),
            new Color(0.28f, 0.24f, 0.30f, 1f), Close);
        closeLabel.text = Loc.T("Закрыть", "Close");

        _panel.SetActive(false);
    }

    private void MakeOffer(Vector2 aMin, Vector2 aMax, Color bg, string title, string desc, string productId)
    {
        var go = new GameObject("Offer", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(_panel.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => Buy(productId));

        var t = MakeText("Title", go.transform, new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.98f), 26, title, Color.white, FontStyles.Bold);
        t.alignment = TextAlignmentOptions.Left;
        var d = MakeText("Desc", go.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.5f), 20, desc, new Color(0.85f, 0.92f, 1f), FontStyles.Normal);
        d.alignment = TextAlignmentOptions.Left;
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
        t.enableAutoSizing = true; t.fontSizeMin = 11; t.fontSizeMax = size;
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
        return MakeText("Label", go.transform, Vector2.zero, Vector2.one, 24, "", Color.white, FontStyles.Normal);
    }

    private static void SetFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
