/// <summary>
/// Батч 12-B: компактный HUD новых валют — кристаллы, жетоны, ключи. Строит свой Canvas в КОДЕ
/// (сборка сцены билдером не нужна). Монеты НЕ дублирует (их показывает существующий CoinsUI).
/// Строки появляются по мере разблокировки (ProgressionManager): кристаллы — D3, жетоны/ключи — D2.
/// Обновляется по требованию (GameManager.*.Refresh) после изменения валют.
/// Иконки — цветные «пилюли» + число (спрайты Mini UI можно привязать позже билдером).
/// Сцена: MainScene (создаётся в рантайме). Зависимости: GameManager, ProgressionManager, TMPro.
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CurrencyHudUI : MonoBehaviour
{
    public static CurrencyHudUI Instance { get; private set; }

    // Батч 13: иконки валют (спрайты Mini UI). Задаёт билдер; если пусто — цветные «пилюли».
    [SerializeField] private Sprite _gemIcon;
    [SerializeField] private Sprite _tokenIcon;
    [SerializeField] private Sprite _keyIcon;

    private TMP_FontAsset _font;
    private GameObject _gemRow, _tokenRow, _keyRow;
    private TextMeshProUGUI _gemText, _tokenText, _keyText;
    private bool _built;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureBuilt(); // самособирается и когда объект создан билдером (иконки уже привязаны)
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
            Instance = go.AddComponent<CurrencyHudUI>(); // Awake построит
        }
        Instance.EnsureBuilt();
        return Instance;
    }

    /// <summary>Обновляет числа и видимость строк по текущим значениям и разблокировкам.</summary>
    public void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        bool gemsOn  = ProgressionManager.IsUnlocked(ProgressionManager.Feature.Gems);
        bool lootOn  = ProgressionManager.IsUnlocked(ProgressionManager.Feature.LootChests);

        if (_gemRow   != null) _gemRow.SetActive(gemsOn);
        if (_tokenRow != null) _tokenRow.SetActive(lootOn);
        if (_keyRow   != null) _keyRow.SetActive(lootOn);

        if (_gemText   != null) _gemText.text   = gm.Gems.ToString();
        if (_tokenText != null) _tokenText.text = gm.Tokens.ToString();
        if (_keyText   != null) _keyText.text   = gm.Keys.ToString();
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

        // Вертикальная колонка справа сверху (под возможной кнопкой настроек).
        var col = new GameObject("Column", typeof(RectTransform), typeof(VerticalLayoutGroup));
        col.transform.SetParent(canvasGo.transform, false);
        var crt = (RectTransform)col.transform;
        crt.anchorMin = new Vector2(0.82f, 0.74f);
        crt.anchorMax = new Vector2(0.995f, 0.90f);
        crt.offsetMin = crt.offsetMax = Vector2.zero;
        var vlg = col.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 6f; vlg.childControlHeight = true; vlg.childControlWidth = true;
        vlg.childForceExpandHeight = true; vlg.childForceExpandWidth = true;

        // Строка «Кристаллы» — кликабельна: открывает магазин кристаллов (Батч 13).
        _gemRow   = MakeRow(col.transform, new Color(0.20f, 0.35f, 0.55f, 0.92f), Loc.T("Кристаллы", "Gems"),  _gemIcon,   out _gemText, () => GemShopUI.Ensure().Open());
        _tokenRow = MakeRow(col.transform, new Color(0.45f, 0.35f, 0.15f, 0.92f), Loc.T("Жетоны", "Tokens"),   _tokenIcon, out _tokenText, null);
        _keyRow   = MakeRow(col.transform, new Color(0.40f, 0.28f, 0.12f, 0.92f), Loc.T("Ключи", "Keys"),      _keyIcon,   out _keyText, null);

        Refresh();
    }

    private GameObject MakeRow(Transform parent, Color bg, string caption, Sprite icon, out TextMeshProUGUI amount, System.Action onClick)
    {
        var row = new GameObject("Row", typeof(RectTransform), typeof(Image));
        row.transform.SetParent(parent, false);
        row.GetComponent<Image>().color = bg;

        // Иконка слева (если задана билдером), иначе подпись занимает её место.
        bool hasIcon = icon != null;
        if (hasIcon)
        {
            var ic = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            ic.transform.SetParent(row.transform, false);
            var irt = (RectTransform)ic.transform;
            irt.anchorMin = new Vector2(0.03f, 0.12f); irt.anchorMax = new Vector2(0.24f, 0.88f);
            irt.offsetMin = irt.offsetMax = Vector2.zero;
            var iimg = ic.GetComponent<Image>();
            iimg.sprite = icon; iimg.preserveAspect = true; iimg.raycastTarget = false;
        }

        float capLeft = hasIcon ? 0.27f : 0.06f;
        var cap = MakeText("Caption", row.transform, new Vector2(capLeft, 0f), new Vector2(0.66f, 1f), 20, TextAlignmentOptions.Left);
        cap.text = caption;

        amount = MakeText("Amount", row.transform, new Vector2(0.66f, 0f), new Vector2(0.96f, 1f), 24, TextAlignmentOptions.Right);
        amount.text = "0";
        amount.fontStyle = FontStyles.Bold;

        if (onClick != null)
        {
            var btn = row.AddComponent<Button>();
            btn.onClick.AddListener(() => onClick());
        }
        return row;
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
