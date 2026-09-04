/// <summary>
/// Батч 15: UI доски задач дня. Кнопка «Задачи» в левой колонке (под виджетом цели) с
/// бейджем доступных наград; панель с 3 задачами (цель+прогресс+Забрать) и бонусом «все три».
/// Строит свой Canvas в КОДЕ. Награды — монеты сразу по клику (кормят обустройство).
/// Сцена: MainScene (рантайм). Зависимости: DailyTaskBoard, GameManager, UiEffects, Loc, TMPro.
/// </summary>
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DailyTaskBoardUI : MonoBehaviour
{
    public static DailyTaskBoardUI Instance { get; private set; }

    private TMP_FontAsset _font;
    private bool _built;

    private GameObject _btn, _badge, _panel, _dim;
    private TextMeshProUGUI _badgeText;
    private readonly TextMeshProUGUI[] _rowGoal = new TextMeshProUGUI[3];
    private readonly TextMeshProUGUI[] _rowProg = new TextMeshProUGUI[3];
    private readonly Button[] _rowClaim = new Button[3];
    private readonly TextMeshProUGUI[] _rowClaimLbl = new TextMeshProUGUI[3];
    private Button _bonusBtn; private TextMeshProUGUI _bonusLbl;
    private int _shownClaimable = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        EnsureBuilt();
    }

    private void EnsureBuilt() { if (_built) return; _built = true; Build(); }

    public static DailyTaskBoardUI Ensure()
    {
        if (Instance == null) Instance = new GameObject("DailyTaskBoard").AddComponent<DailyTaskBoardUI>();
        Instance.EnsureBuilt();
        return Instance;
    }

    private void Update()
    {
        if (_badge == null) return;
        int c = DailyTaskBoard.Claimable();
        if (c == _shownClaimable) return;
        _shownClaimable = c;
        _badge.SetActive(c > 0);
        if (_badgeText != null) _badgeText.text = c.ToString();
    }

    public void Open()
    {
        if (_panel == null) return;
        RefreshPanel();
        if (_dim != null) { _dim.SetActive(true); _dim.transform.SetAsLastSibling(); }
        _panel.SetActive(true); _panel.transform.SetAsLastSibling();
        AudioController.Instance?.PlayUiOpen();
    }

    public void Close()
    {
        AudioController.Instance?.PlayUiClose();
        if (_panel != null) _panel.SetActive(false);
        if (_dim != null) _dim.SetActive(false);
    }

    private void RefreshPanel()
    {
        var tasks = DailyTaskBoard.Tasks;
        for (int i = 0; i < 3; i++)
        {
            bool has = i < tasks.Count;
            if (_rowGoal[i] != null) _rowGoal[i].text = has ? tasks[i].GoalText() : "";
            if (_rowProg[i] != null) _rowProg[i].text = has ? tasks[i].ProgressText() : "";
            bool canClaim = has && tasks[i].Complete && !DailyTaskBoard.IsClaimed(i);
            if (_rowClaim[i] != null) _rowClaim[i].interactable = canClaim;
            if (_rowClaimLbl[i] != null)
                _rowClaimLbl[i].text = !has ? "" :
                    DailyTaskBoard.IsClaimed(i) ? Loc.T("Забрано", "Claimed")
                    : tasks[i].Complete ? Loc.T($"Забрать +{tasks[i].Reward}", $"Claim +{tasks[i].Reward}")
                    : Loc.T("В процессе", "In progress");
        }
        bool bonusReady = DailyTaskBoard.AllClaimed && !DailyTaskBoard.BonusClaimed;
        if (_bonusBtn != null) _bonusBtn.interactable = bonusReady;
        if (_bonusLbl != null)
            _bonusLbl.text = DailyTaskBoard.BonusClaimed ? Loc.T("Бонус забран", "Bonus claimed")
                : Loc.T($"Бонус за все три: +{DailyTaskBoard.BonusReward} и +{DailyTaskBoard.BonusGems} крист.",
                        $"All three bonus: +{DailyTaskBoard.BonusReward} and +{DailyTaskBoard.BonusGems} gems");
    }

    private void OnClaim(int i)
    {
        int r = DailyTaskBoard.Claim(i);
        if (r > 0) { AudioController.Instance?.PlayCoin(); UiEffects.Instance?.CoinBurst(r); }
        else AudioController.Instance?.PlayWrongOrder();
        _shownClaimable = -1;
        RefreshPanel();
    }

    private void OnBonus()
    {
        int r = DailyTaskBoard.ClaimBonus();
        if (r > 0) { AudioController.Instance?.PlayBonus(); UiEffects.Instance?.CoinBurst(r); }
        else AudioController.Instance?.PlayWrongOrder();
        _shownClaimable = -1;
        RefreshPanel();
    }

    // ─── Построение ─────────────────────────────────────────────────────────────
    private void Build()
    {
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("TasksCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 216;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;

        // Кнопка «Задачи» в левой колонке под виджетом цели (0.745..0.815) → 0.665..0.735.
        _btn = new GameObject("TasksBtn", typeof(RectTransform), typeof(Image), typeof(Button));
        _btn.transform.SetParent(canvasGo.transform, false);
        var brt = (RectTransform)_btn.transform;
        brt.anchorMin = new Vector2(0.02f, 0.665f); brt.anchorMax = new Vector2(0.20f, 0.735f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        _btn.GetComponent<Image>().color = new Color(0.10f, 0.09f, 0.16f, 0.9f);
        _btn.GetComponent<Button>().onClick.AddListener(Open);
        MakeText("Cap", _btn.transform, new Vector2(0.06f, 0f), new Vector2(0.96f, 1f), 20,
            Loc.T("Задачи дня", "Daily tasks"), new Color(0.9f, 0.92f, 1f), FontStyles.Normal, TextAlignmentOptions.Left);

        _badge = new GameObject("Badge", typeof(RectTransform), typeof(Image));
        _badge.transform.SetParent(_btn.transform, false);
        var badgeRt = (RectTransform)_badge.transform;
        badgeRt.anchorMin = badgeRt.anchorMax = new Vector2(1f, 1f);
        badgeRt.sizeDelta = new Vector2(38, 38); badgeRt.anchoredPosition = new Vector2(2f, 2f);
        _badge.GetComponent<Image>().color = new Color(0.9f, 0.2f, 0.2f, 1f);
        _badge.GetComponent<Image>().raycastTarget = false;
        _badgeText = MakeText("N", _badge.transform, Vector2.zero, Vector2.one, 22, "", Color.white, FontStyles.Bold, TextAlignmentOptions.Center);
        _badge.SetActive(false);

        // Панель
        _dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        _dim.transform.SetParent(canvasGo.transform, false);
        var drt = (RectTransform)_dim.transform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;
        _dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        _dim.GetComponent<Button>().onClick.AddListener(Close);
        _dim.SetActive(false);

        _panel = new GameObject("TasksPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform; prt.anchorMin = new Vector2(0.26f, 0.2f); prt.anchorMax = new Vector2(0.74f, 0.82f); prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.13f, 0.98f);

        MakeText("Hdr", _panel.transform, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.99f), 28,
            Loc.T("Задачи дня", "Daily tasks"), new Color(0.9f, 0.92f, 1f), FontStyles.Bold, TextAlignmentOptions.Center);

        for (int i = 0; i < 3; i++)
        {
            float top = 0.86f - i * 0.19f, bot = top - 0.16f;
            var row = new GameObject($"Row{i}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_panel.transform, false);
            var rrt = (RectTransform)row.transform; rrt.anchorMin = new Vector2(0.05f, bot); rrt.anchorMax = new Vector2(0.95f, top); rrt.offsetMin = rrt.offsetMax = Vector2.zero;
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.05f);
            _rowGoal[i] = MakeText("Goal", row.transform, new Vector2(0.03f, 0.45f), new Vector2(0.62f, 0.97f), 20, "", Color.white, FontStyles.Normal, TextAlignmentOptions.Left);
            _rowProg[i] = MakeText("Prog", row.transform, new Vector2(0.03f, 0.05f), new Vector2(0.62f, 0.45f), 18, "", new Color(0.8f, 0.85f, 0.95f), FontStyles.Normal, TextAlignmentOptions.Left);
            int idx = i;
            _rowClaimLbl[i] = MakeButton($"Claim{i}", row.transform, new Vector2(0.64f, 0.15f), new Vector2(0.97f, 0.85f), new Color(0.22f, 0.45f, 0.28f, 1f), () => OnClaim(idx));
            _rowClaim[i] = _rowClaimLbl[i].transform.parent.GetComponent<Button>();
        }

        _bonusLbl = MakeButton("Bonus", _panel.transform, new Vector2(0.06f, 0.11f), new Vector2(0.94f, 0.24f), new Color(0.22f, 0.38f, 0.5f, 1f), OnBonus);
        _bonusBtn = _bonusLbl.transform.parent.GetComponent<Button>();

        var closeLbl = MakeButton("Close", _panel.transform, new Vector2(0.36f, 0.02f), new Vector2(0.64f, 0.10f), new Color(0.28f, 0.24f, 0.30f, 1f), Close);
        closeLbl.text = Loc.T("Закрыть", "Close");

        _panel.SetActive(false);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size,
        string content, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = content; t.fontSize = size; t.alignment = align; t.color = color; t.fontStyle = style;
        t.raycastTarget = false; t.enableWordWrapping = true; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = size;
        return t;
    }

    private TextMeshProUGUI MakeButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        return MakeText("Label", go.transform, Vector2.zero, Vector2.one, 18, "", Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
    }
}
