/// <summary>
/// Батч 13 / переработано в Батч 14: магазин кристаллов — «Потратить» (перки, немедленно
/// полезно) + «Пополнить» (IAP). Строит свой Canvas в КОДЕ. Перки замыкают экономику: игрок
/// с первых дней хочет улучшать кофейню, а кристаллы дают это мгновенно, плюс «убрать рекламу».
/// Открывается тапом по строке «Кристаллы» в валютном HUD.
///
/// ⚠️ Товары gems_small/gems_medium/gems_large/starter_pack — в консоли Яндекс Игр.
/// Сцена: MainScene (рантайм). Зависимости: GameManager, RewardPopupUI, Loc, TMPro. SDK: YG2 Payments.
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

    // Перки — обновляем подписи/доступность при открытии.
    private Button _upgradeBtn, _adsBtn;
    private TextMeshProUGUI _upgradeLabel, _adsLabel;

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
        RefreshPerks();
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        AudioController.Instance?.PlayUiOpen();
    }

    public void Close()
    {
        AudioController.Instance?.PlayUiClose();
        if (_panel != null) _panel.SetActive(false);
    }

    // ─── Перки (сток кристаллов) ────────────────────────────────────────────────

    private void RefreshPerks()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        int nextUpg = gm.NextUpgradableType();
        bool canUpg  = nextUpg >= 0 && gm.Gems >= GameManager.GemUpgradeCost;
        if (_upgradeBtn != null) _upgradeBtn.interactable = canUpg;
        if (_upgradeLabel != null)
            _upgradeLabel.text = nextUpg < 0
                ? Loc.T("Кофейня улучшена полностью", "Café fully upgraded")
                : Loc.T($"Улучшить кофейню (−{GameManager.GemUpgradeCost})", $"Upgrade café (−{GameManager.GemUpgradeCost})");

        bool adsGone = YG.YG2.saves != null && YG.YG2.saves.adsDisabled;
        bool canAds  = !adsGone && gm.Gems >= GameManager.GemRemoveAdsCost;
        if (_adsBtn != null) _adsBtn.interactable = canAds;
        if (_adsLabel != null)
            _adsLabel.text = adsGone
                ? Loc.T("Реклама отключена", "Ads disabled")
                : Loc.T($"Убрать рекламу (−{GameManager.GemRemoveAdsCost})", $"Remove ads (−{GameManager.GemRemoveAdsCost})");
    }

    private void OnBuyUpgrade()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;
        int t = gm.NextUpgradableType();
        if (t >= 0 && gm.BuyUpgradeWithGems((UpgradeType)t))
        {
            AudioController.Instance?.PlayCoin();
            RewardPopupUI.Ensure().Show(Loc.T("Кофейня улучшена!", "Café upgraded!"),
                Loc.T("Улучшение применено сразу.", "Upgrade applied instantly."),
                new Color(0.42f, 0.85f, 0.45f), 2.6f);
        }
        else AudioController.Instance?.PlayWrongOrder();
        RefreshPerks();
    }

    private void OnRemoveAds()
    {
        var gm = GameManager.Instance;
        if (gm != null && gm.RemoveAdsWithGems())
        {
            AudioController.Instance?.PlayBonus();
            RewardPopupUI.Ensure().Show(Loc.T("Реклама отключена!", "Ads removed!"),
                Loc.T("Спасибо за поддержку.", "Thanks for the support."),
                new Color(0.35f, 0.7f, 0.95f), 2.6f);
        }
        else AudioController.Instance?.PlayWrongOrder();
        RefreshPerks();
    }

    // ─── Пополнение (IAP) ───────────────────────────────────────────────────────

    private void Buy(string productId)
    {
        AudioController.Instance?.PlayClick();
        GameManager.Instance?.BuyGems(productId);
        Close();
    }

    // ─── Построение ─────────────────────────────────────────────────────────────

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

        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        dim.transform.SetParent(canvasGo.transform, false);
        SetFull((RectTransform)dim.transform);
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        dim.GetComponent<Button>().onClick.AddListener(Close);

        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = new Vector2(0.27f, 0.12f);
        prt.anchorMax = new Vector2(0.73f, 0.88f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.05f, 0.05f, 0.12f, 0.98f); // тон как у панелей игры

        MakeText("Title", _panel.transform, new Vector2(0.05f, 0.91f), new Vector2(0.95f, 0.99f), 34,
            Loc.T("Кристаллы", "Gems"), new Color(0.6f, 0.85f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);

        // Секция «Потратить» (перки).
        MakeText("SpendHdr", _panel.transform, new Vector2(0.06f, 0.85f), new Vector2(0.94f, 0.90f), 20,
            Loc.T("Потратить", "Spend"), new Color(0.8f, 0.85f, 0.95f), FontStyles.Normal, TextAlignmentOptions.Left);
        _upgradeBtn = MakePerk(new Vector2(0.06f, 0.735f), new Vector2(0.94f, 0.835f),
            new Color(0.22f, 0.42f, 0.28f, 1f), OnBuyUpgrade, out _upgradeLabel);
        _adsBtn = MakePerk(new Vector2(0.06f, 0.625f), new Vector2(0.94f, 0.725f),
            new Color(0.22f, 0.38f, 0.5f, 1f), OnRemoveAds, out _adsLabel);

        // Секция «Пополнить» (IAP).
        MakeText("BuyHdr", _panel.transform, new Vector2(0.06f, 0.555f), new Vector2(0.94f, 0.605f), 20,
            Loc.T("Пополнить", "Get more"), new Color(0.8f, 0.85f, 0.95f), FontStyles.Normal, TextAlignmentOptions.Left);
        MakeOffer(new Vector2(0.06f, 0.44f), new Vector2(0.94f, 0.545f), new Color(0.22f, 0.40f, 0.55f, 1f),
            Loc.T("Стартовый набор", "Starter pack"),
            Loc.T("100 кристаллов + 2000 монет + без рекламы", "100 gems + 2000 coins + no ads"),
            GameManager.StarterPackId);
        MakeOffer(new Vector2(0.06f, 0.335f), new Vector2(0.94f, 0.43f), new Color(0.18f, 0.30f, 0.45f, 1f),
            Loc.T("Горсть кристаллов", "Handful of gems"), Loc.T("50 кристаллов", "50 gems"), GameManager.GemsSmallId);
        MakeOffer(new Vector2(0.06f, 0.23f), new Vector2(0.94f, 0.325f), new Color(0.18f, 0.30f, 0.45f, 1f),
            Loc.T("Мешочек кристаллов", "Sack of gems"), Loc.T("170 кристаллов", "170 gems"), GameManager.GemsMediumId);
        MakeOffer(new Vector2(0.06f, 0.125f), new Vector2(0.94f, 0.22f), new Color(0.18f, 0.30f, 0.45f, 1f),
            Loc.T("Сундук кристаллов", "Chest of gems"), Loc.T("500 кристаллов", "500 gems"), GameManager.GemsLargeId);

        var closeLabel = MakeButton("CloseBtn", _panel.transform, new Vector2(0.34f, 0.03f), new Vector2(0.66f, 0.11f),
            new Color(0.28f, 0.24f, 0.30f, 1f), Close);
        closeLabel.text = Loc.T("Закрыть", "Close");

        _panel.SetActive(false);
    }

    private Button MakePerk(Vector2 aMin, Vector2 aMax, Color bg, Action onClick, out TextMeshProUGUI label)
    {
        label = MakeButton("Perk", _panel.transform, aMin, aMax, bg, onClick);
        return label.transform.parent.GetComponent<Button>();
    }

    private void MakeOffer(Vector2 aMin, Vector2 aMax, Color bg, string title, string desc, string productId)
    {
        var go = new GameObject("Offer", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(_panel.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => Buy(productId));

        var t = MakeText("Title", go.transform, new Vector2(0.04f, 0.5f), new Vector2(0.96f, 0.98f), 24, title, Color.white, FontStyles.Bold, TextAlignmentOptions.Left);
        var d = MakeText("Desc", go.transform, new Vector2(0.04f, 0.04f), new Vector2(0.96f, 0.5f), 18, desc, new Color(0.85f, 0.92f, 1f), FontStyles.Normal, TextAlignmentOptions.Left);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size,
        string content, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = content; t.fontSize = size; t.alignment = align; t.color = color;
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
        return MakeText("Label", go.transform, Vector2.zero, Vector2.one, 22, "", Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
    }

    private static void SetFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
