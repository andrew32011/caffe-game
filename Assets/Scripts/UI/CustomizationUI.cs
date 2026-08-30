/// <summary>
/// Батч 13: экран оформления (кастомизация Батч 12-D). Выбор аватара (иконки Mini UI/Avatars)
/// и темы оформления (9-slice панели «Dark Theme RoundEdge»). Спрайты привязывает билдер
/// (они вне Resources), сетки выбора строятся в коде. Выбор персистится (avatarId/themeId) и
/// применяется: аватар → бейдж на HUD; тема → UiTheme (все панели через ThemedPanel).
/// Вход — тап по бейджу аватара на HUD (виден только после разблокировки на D4).
///
/// Сцена: MainScene (UI, билдер). Зависимости: GameManager, ProgressionManager, UiTheme, TMPro.
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CustomizationUI : MonoBehaviour
{
    public static CustomizationUI Instance { get; private set; }

    [Header("Панель")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _closeButton;

    [Header("Ассеты (привязывает билдер)")]
    [SerializeField] private Sprite[] _avatarSprites;
    [SerializeField] private Sprite[] _themeSprites; // 9-slice панели-варианты; [0] = дефолтная

    [Header("Применение")]
    [SerializeField] private Image _avatarBadge;         // бейдж аватара на HUD
    [SerializeField] private Button _avatarBadgeButton;  // тап по бейджу открывает экран

    private bool _built;
    private readonly List<Button> _avatarButtons = new List<Button>();
    private readonly List<Button> _themeButtons  = new List<Button>();
    private TMP_FontAsset _font;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_avatarBadgeButton != null) _avatarBadgeButton.onClick.AddListener(Open);

        ApplySaved();
        RefreshBadgeVisibility();
        if (_panel != null) _panel.SetActive(false);
    }

    /// <summary>Применяет сохранённые аватар/тему (на старте и при смене).</summary>
    public void ApplySaved()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (_avatarBadge != null && _avatarSprites != null && _avatarSprites.Length > 0)
            _avatarBadge.sprite = _avatarSprites[Mathf.Clamp(gm.AvatarId, 0, _avatarSprites.Length - 1)];

        if (_themeSprites != null && _themeSprites.Length > 0)
            UiTheme.SetPanelSprite(_themeSprites[Mathf.Clamp(gm.ThemeId, 0, _themeSprites.Length - 1)]);
    }

    /// <summary>Бейдж (и весь вход) виден только когда кастомизация разблокирована (D4).</summary>
    public void RefreshBadgeVisibility()
    {
        bool on = ProgressionManager.IsUnlocked(ProgressionManager.Feature.Customization);
        if (_avatarBadge != null) _avatarBadge.gameObject.SetActive(on);
    }

    public void Open()
    {
        if (_panel == null) return;
        BuildGrids();
        RefreshHighlights();
        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        AudioController.Instance?.PlayUiOpen();
    }

    public void Close()
    {
        AudioController.Instance?.PlayUiClose();
        if (_panel != null) _panel.SetActive(false);
    }

    // ─── Построение сеток выбора (в коде, из привязанных спрайтов) ──────────────
    private void BuildGrids()
    {
        if (_built || _panel == null) return;
        _built = true;

        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        MakeLabel("Оформление", "Customization", new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.99f), 32, FontStyles.Bold);
        MakeLabel("Аватар", "Avatar", new Vector2(0.05f, 0.82f), new Vector2(0.95f, 0.88f), 22, FontStyles.Normal);
        MakeLabel("Тема", "Theme", new Vector2(0.05f, 0.26f), new Vector2(0.95f, 0.32f), 22, FontStyles.Normal);

        // Аватары — сетка 4 колонки в области y 0.34–0.81.
        if (_avatarSprites != null)
        {
            int cols = 4;
            int n = _avatarSprites.Length;
            int rows = Mathf.Max(1, Mathf.CeilToInt(n / (float)cols));
            const float xL = 0.06f, xR = 0.94f, yB = 0.34f, yT = 0.81f;
            float cw = (xR - xL) / cols, ch = (yT - yB) / rows;
            float pad = 0.012f;
            for (int i = 0; i < n; i++)
            {
                int r = i / cols, c = i % cols;
                float x0 = xL + c * cw, y1 = yT - r * ch;
                int idx = i;
                var b = MakeSpriteButton($"Avatar{i}", _avatarSprites[i], true,
                    new Vector2(x0 + pad, y1 - ch + pad), new Vector2(x0 + cw - pad, y1 - pad),
                    () => SelectAvatar(idx));
                _avatarButtons.Add(b);
            }
        }

        // Темы — ряд свотчей в области y 0.15–0.25.
        if (_themeSprites != null)
        {
            int m = _themeSprites.Length;
            const float xL = 0.06f, xR = 0.94f, yB = 0.15f, yT = 0.25f;
            float cw = (xR - xL) / Mathf.Max(1, m);
            float pad = 0.008f;
            for (int i = 0; i < m; i++)
            {
                float x0 = xL + i * cw;
                int idx = i;
                var b = MakeSpriteButton($"Theme{i}", _themeSprites[i], false,
                    new Vector2(x0 + pad, yB), new Vector2(x0 + cw - pad, yT),
                    () => SelectTheme(idx));
                _themeButtons.Add(b);
            }
        }
    }

    private void SelectAvatar(int i)
    {
        AudioController.Instance?.PlayClick();
        GameManager.Instance?.SetAvatar(i);
        ApplySaved();
        RefreshHighlights();
    }

    private void SelectTheme(int i)
    {
        AudioController.Instance?.PlayClick();
        GameManager.Instance?.SetTheme(i);
        if (_themeSprites != null && i >= 0 && i < _themeSprites.Length)
            UiTheme.SetPanelSprite(_themeSprites[i]);
        RefreshHighlights();
    }

    private void RefreshHighlights()
    {
        var gm = GameManager.Instance;
        int a = gm != null ? gm.AvatarId : 0;
        int th = gm != null ? gm.ThemeId : 0;
        for (int i = 0; i < _avatarButtons.Count; i++)
            if (_avatarButtons[i] != null) _avatarButtons[i].transform.localScale = (i == a) ? Vector3.one * 1.14f : Vector3.one;
        for (int i = 0; i < _themeButtons.Count; i++)
            if (_themeButtons[i] != null) _themeButtons[i].transform.localScale = (i == th) ? Vector3.one * 1.14f : Vector3.one;
    }

    // ─── Хелперы ──────────────────────────────────────────────────────────────
    private Button MakeSpriteButton(string name, Sprite sprite, bool preserveAspect, Vector2 aMin, Vector2 aMax, UnityEngine.Events.UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(_panel.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = preserveAspect;
        if (sprite != null && !preserveAspect) img.type = Image.Type.Sliced; // темы-свотчи как панели
        go.GetComponent<Button>().onClick.AddListener(onClick);
        return go.GetComponent<Button>();
    }

    private void MakeLabel(string ru, string en, Vector2 aMin, Vector2 aMax, int size, FontStyles style)
    {
        var go = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(_panel.transform, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = Loc.T(ru, en); t.fontSize = size; t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white; t.fontStyle = style; t.raycastTarget = false;
        t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = size;
    }
}
