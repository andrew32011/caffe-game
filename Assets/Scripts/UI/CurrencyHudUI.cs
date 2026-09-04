/// <summary>
/// Батч 12-B / упрощено в Батч 14: компактный HUD премиум-валюты — КРИСТАЛЛЫ (монеты
/// показывает CoinsUI, отдельная строка). Строит свой Canvas в КОДЕ. Появляется по
/// разблокировке кристаллов (ProgressionManager, D3). Строка кликабельна — открывает
/// магазин кристаллов/перков (GemShopUI). Слева-сверху под кассой, чтобы не пересекать
/// правый док (журнал/подсказка).
/// Сцена: MainScene (рантайм). Зависимости: GameManager, ProgressionManager, GemShopUI, TMPro.
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyHudUI : MonoBehaviour
{
    public static CurrencyHudUI Instance { get; private set; }

    // Иконка кристаллов (спрайт Mini UI). Задаёт билдер; если пусто — цветная «пилюля».
    [SerializeField] private Sprite _gemIcon;

    private TMP_FontAsset _font;
    private GameObject _gemRow;
    private TextMeshProUGUI _gemText;
    private bool _built;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureBuilt();
    }

    private void EnsureBuilt()
    {
        if (_built) return;
        _built = true;
        Build();
    }

    public static CurrencyHudUI Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("CurrencyHud");
            Instance = go.AddComponent<CurrencyHudUI>();
        }
        Instance.EnsureBuilt();
        return Instance;
    }

    /// <summary>Обновляет число кристаллов и видимость строки (по разблокировке).</summary>
    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool gemsOn = ProgressionManager.IsUnlocked(ProgressionManager.Feature.Gems);
        if (_gemRow  != null) _gemRow.SetActive(gemsOn);
        if (_gemText != null) _gemText.text = gm.Gems.ToString();
    }

    private void Build()
    {
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("CurrencyCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 210; // над обычным HUD, под попапами
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Слева-сверху, ПОД строкой кассы (CoinsText 0.9–0.96) — не пересекает правый док.
        _gemRow = new GameObject("GemRow", typeof(RectTransform), typeof(Image), typeof(Button));
        _gemRow.transform.SetParent(canvasGo.transform, false);
        var rt = (RectTransform)_gemRow.transform;
        rt.anchorMin = new Vector2(0.02f, 0.835f);
        rt.anchorMax = new Vector2(0.20f, 0.895f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        _gemRow.GetComponent<Image>().color = new Color(0.20f, 0.35f, 0.55f, 0.92f);
        _gemRow.GetComponent<Button>().onClick.AddListener(() => GemShopUI.Ensure().Open());

        if (_gemIcon != null)
        {
            var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            ic.transform.SetParent(_gemRow.transform, false);
            var irt = (RectTransform)ic.transform;
            irt.anchorMin = new Vector2(0.04f, 0.12f); irt.anchorMax = new Vector2(0.30f, 0.88f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            var iimg = ic.GetComponent<Image>();
            iimg.sprite = _gemIcon; iimg.preserveAspect = true; iimg.raycastTarget = false;
        }

        _gemText = MakeText("Amount", _gemRow.transform, new Vector2(0.34f, 0f), new Vector2(0.96f, 1f), 26, TextAlignmentOptions.Left);
        _gemText.text = "0";
        _gemText.fontStyle = FontStyles.Bold;

        Refresh();
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.fontSize = size; t.alignment = align; t.color = Color.white; t.raycastTarget = false;
        t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = size;
        return t;
    }
}
