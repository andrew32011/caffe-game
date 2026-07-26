/// <summary>
/// Ненавязчивое напоминание «Убрать рекламу» (пункт: органично вплести покупку).
/// Вместо постоянной кнопки-«бельма» в углу показывает короткую всплывашку ПОСЛЕ рекламы:
/// «Отключить рекламу и поддержать автора?» + кнопка покупки. Авто-скрывается через ~7с,
/// прячется навсегда после покупки/если уже куплено. Подписи берутся из общей таблицы
/// UiTranslations (все поддерживаемые языки), как у других элементов UI.
///
/// Товар «no_ads» должен быть заведён в консоли Яндекс Игр.
/// Сцена: MainScene (singleton). Зависимости: YG2 (Payments), UiTranslations/Loc, TMPro.
/// SDK: YG2 (Payments)
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class AdRemovalPrompt : MonoBehaviour
{
    public static AdRemovalPrompt Instance { get; private set; }

    // Id некупляемого товара «отключить рекламу» в консоли Яндекс Игр.
    public const string ProductId = "no_ads";

    [SerializeField] private TMP_FontAsset _font;

    private GameObject _panel;
    private Coroutine _hideCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void OnEnable()  { YG2.onPurchaseSuccess += OnPurchase; }
    private void OnDisable() { YG2.onPurchaseSuccess -= OnPurchase; }

    // Локализация статической подписи по общей таблице (все языки), с падением на Loc.
    private static string Tr(string ru) =>
        UiTranslations.Has(ru) ? UiTranslations.Get(ru, Loc.Lang) : ru;

    /// <summary>Показать напоминание (вызывается после показа всплывающей рекламы).
    /// Если реклама уже отключена — ничего не делаем.</summary>
    public void ShowAfterAd()
    {
        if (YG2.saves != null && YG2.saves.adsDisabled) return;
        if (_panel == null) Build();
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        if (_hideCo != null) StopCoroutine(_hideCo);
        _hideCo = StartCoroutine(AutoHide(7f));
    }

    private void Hide()
    {
        if (_hideCo != null) { StopCoroutine(_hideCo); _hideCo = null; }
        if (_panel != null) _panel.SetActive(false);
    }

    private IEnumerator AutoHide(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        if (_panel != null) _panel.SetActive(false);
    }

    // ─── Построение всплывашки (нижний центр, компактно) ─────────────────────────

    private void Build()
    {
        var canvasGo = new GameObject("AdRemovalCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 300;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = new Vector2(0.28f, 0.03f);
        prt.anchorMax = new Vector2(0.72f, 0.15f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        var pimg = _panel.GetComponent<Image>();
        pimg.color = new Color(0.10f, 0.09f, 0.16f, 0.95f);

        // Текст-напоминание (верхняя часть панели).
        var txtGo = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(_panel.transform, false);
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = new Vector2(0.03f, 0.5f);
        trt.anchorMax = new Vector2(0.97f, 0.98f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var t = txtGo.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = Tr("Отключить рекламу и поддержать автора?");
        t.fontSize = 26;
        t.alignment = TextAlignmentOptions.Center;
        t.color = new Color(1f, 0.95f, 0.8f);
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        t.enableAutoSizing = true;
        t.fontSizeMin = 14; t.fontSizeMax = 28;

        // Кнопка покупки (слева внизу).
        var buyBtn = MakeButton("BuyBtn", _panel.transform,
            new Vector2(0.06f, 0.08f), new Vector2(0.64f, 0.46f),
            new Color(0.20f, 0.45f, 0.25f, 1f), Tr("Убрать рекламу"));
        buyBtn.onClick.AddListener(OnBuyClick);

        // Кнопка «Закрыть» (справа внизу).
        var closeBtn = MakeButton("CloseBtn", _panel.transform,
            new Vector2(0.68f, 0.08f), new Vector2(0.94f, 0.46f),
            new Color(0.28f, 0.24f, 0.30f, 1f), Tr("Закрыть"));
        closeBtn.onClick.AddListener(Hide);

        _panel.SetActive(false);
    }

    private Button MakeButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;

        var lg = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        lg.transform.SetParent(go.transform, false);
        var lrt = (RectTransform)lg.transform;
        lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
        lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        var lt = lg.GetComponent<TextMeshProUGUI>();
        if (_font != null) lt.font = _font;
        lt.text = label;
        lt.fontSize = 24;
        lt.alignment = TextAlignmentOptions.Center;
        lt.color = Color.white;
        lt.raycastTarget = false;
        lt.enableAutoSizing = true;
        lt.fontSizeMin = 12; lt.fontSizeMax = 26;

        return go.GetComponent<Button>();
    }

    private void OnBuyClick()
    {
        AudioController.Instance?.PlayClick();
#if Payments_yg
        YG2.BuyPayments(ProductId);
#else
        Debug.LogWarning("AdRemovalPrompt: модуль YG2 Payments не установлен (define Payments_yg).");
#endif
    }

    private void OnPurchase(string id)
    {
        if (id != ProductId) return;
        if (YG2.saves != null)
        {
            YG2.saves.adsDisabled = true;
            GameManager.Instance?.SaveGame();
        }
        AudioController.Instance?.PlayBonus();
        Hide();
    }
}
