/// <summary>
/// Батч 15 (Фаза C): ежедневное колесо наград (переменная награда — Coin Master). Раз в день
/// бесплатный «спин» даёт случайную награду (монеты/кристаллы); второй спин — за rewarded-рекламу.
/// Строит свой Canvas в КОДЕ. Дата спина в YG2.saves.wheelLastSpin.
/// Сцена: MainScene (рантайм). Зависимости: GameManager, UiEffects, Loc, TMPro. SDK: YG2 Rewarded (опц.).
/// </summary>
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class RewardWheelUI : MonoBehaviour
{
    public static RewardWheelUI Instance { get; private set; }

    private struct Prize { public string Ru, En; public int Coins, Gems; public Prize(string ru, string en, int c, int g){ Ru=ru;En=en;Coins=c;Gems=g; } }
    private static readonly Prize[] Prizes =
    {
        new Prize("+80 монет", "+80 coins", 80, 0),
        new Prize("+150 монет", "+150 coins", 150, 0),
        new Prize("+2 кристалла", "+2 gems", 0, 2),
        new Prize("+250 монет", "+250 coins", 250, 0),
        new Prize("+1 кристалл", "+1 gem", 0, 1),
        new Prize("+400 монет", "+400 coins", 400, 0),
    };

    private TMP_FontAsset _font;
    private GameObject _panel, _dim;
    private TextMeshProUGUI[] _rows = new TextMeshProUGUI[6];
    private Button _spinBtn, _adBtn; private TextMeshProUGUI _spinLbl, _adLbl, _status;
    private bool _spinning;

    private void Awake() { if (Instance != null && Instance != this) { Destroy(gameObject); return; } Instance = this; }

    public static RewardWheelUI Ensure()
    {
        if (Instance == null) Instance = new GameObject("RewardWheel").AddComponent<RewardWheelUI>();
        return Instance;
    }

    private static string Today => DateTime.Now.ToString("yyyyMMdd");
    private static bool FreeAvailable => YG2.isSDKEnabled && YG2.saves.wheelLastSpin != Today;

    public void Open()
    {
        if (_panel == null) Build();
        Refresh();
        _dim.SetActive(true); _dim.transform.SetAsLastSibling();
        _panel.SetActive(true); _panel.transform.SetAsLastSibling();
        AudioController.Instance?.PlayUiOpen();
    }

    public void Close()
    {
        AudioController.Instance?.PlayUiClose();
        if (_panel != null) _panel.SetActive(false);
        if (_dim != null) _dim.SetActive(false);
    }

    private void Refresh()
    {
        if (_spinBtn != null) _spinBtn.interactable = FreeAvailable && !_spinning;
        if (_spinLbl != null) _spinLbl.text = FreeAvailable ? Loc.T("Крутить (бесплатно)", "Spin (free)") : Loc.T("Сегодня уже крутили", "Spun today");
        bool ad = false;
#if RewardedAdv_yg
        ad = !FreeAvailable && !_spinning;
#endif
        if (_adBtn != null) _adBtn.gameObject.SetActive(ad);
        if (_adLbl != null) _adLbl.text = Loc.T("Ещё спин — реклама", "Extra spin — ad");
        if (_status != null) _status.text = "";
    }

    private void DoSpin(bool free)
    {
        if (_spinning) return;
        if (free) { if (!FreeAvailable) return; YG2.saves.wheelLastSpin = Today; GameManager.Instance?.SaveGame(); }
        StartCoroutine(SpinRoutine());
    }

    private IEnumerator SpinRoutine()
    {
        _spinning = true; Refresh();
        int final = UnityEngine.Random.Range(0, Prizes.Length);
        // «Прокрутка»: бегущая подсветка строк, замедляясь, останавливается на final.
        int steps = 18 + final;
        float delay = 0.04f;
        for (int s = 0; s <= steps; s++)
        {
            int hi = s % Prizes.Length;
            for (int i = 0; i < _rows.Length; i++)
                if (_rows[i] != null) _rows[i].color = (i == hi) ? new Color(1f, 0.9f, 0.4f) : Color.white;
            AudioController.Instance?.PlayClick();
            yield return new WaitForSecondsRealtime(delay);
            if (s > steps - 6) delay += 0.03f; // замедление в конце
        }
        var p = Prizes[final];
        if (p.Coins > 0) { GameManager.Instance?.AddCoins(p.Coins); UiEffects.Instance?.CoinBurst(p.Coins); }
        if (p.Gems  > 0) { GameManager.Instance?.AddGems(p.Gems); }
        AudioController.Instance?.PlayBonus();
        Analytics.Send("wheel_spin", "prize", (Loc.IsRu ? p.Ru : p.En));
        if (_status != null) _status.text = Loc.T("Выпало: ", "You got: ") + (Loc.IsRu ? p.Ru : p.En);
        _spinning = false;
        Refresh();
    }

#if RewardedAdv_yg
    private void OnEnable()  { YG2.onRewardAdv += OnReward; }
    private void OnDisable() { YG2.onRewardAdv -= OnReward; }
    private const string WheelAdId = "wheel_spin";
    private void OnReward(string id) { if (id == WheelAdId) DoSpin(free: false); }
#endif

    private void OnAdSpin()
    {
#if RewardedAdv_yg
        YG2.RewardedAdvShow(WheelAdId);
#endif
    }

    // ─── Построение ─────────────────────────────────────────────────────────────
    private void Build()
    {
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("WheelCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 322;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080); scaler.matchWidthOrHeight = 0.5f;

        _dim = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        _dim.transform.SetParent(canvasGo.transform, false);
        var drt = (RectTransform)_dim.transform; drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one; drt.offsetMin = drt.offsetMax = Vector2.zero;
        _dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        _dim.GetComponent<Button>().onClick.AddListener(Close);
        _dim.SetActive(false);

        _panel = new GameObject("WheelPanel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform; prt.anchorMin = new Vector2(0.3f, 0.16f); prt.anchorMax = new Vector2(0.7f, 0.84f); prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.06f, 0.06f, 0.13f, 0.98f);

        MakeText("Hdr", _panel.transform, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.99f), 28,
            Loc.T("Колесо удачи", "Wheel of luck"), new Color(1f, 0.9f, 0.5f), FontStyles.Bold, TextAlignmentOptions.Center);
        for (int i = 0; i < Prizes.Length; i++)
        {
            float top = 0.87f - i * 0.10f, bot = top - 0.085f;
            var row = new GameObject($"Prize{i}", typeof(RectTransform), typeof(Image));
            row.transform.SetParent(_panel.transform, false);
            var rrt = (RectTransform)row.transform; rrt.anchorMin = new Vector2(0.1f, bot); rrt.anchorMax = new Vector2(0.9f, top); rrt.offsetMin = rrt.offsetMax = Vector2.zero;
            row.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);
            _rows[i] = MakeText("L", row.transform, Vector2.zero, Vector2.one, 22, Loc.IsRu ? Prizes[i].Ru : Prizes[i].En, Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
        }
        _status = MakeText("Status", _panel.transform, new Vector2(0.05f, 0.19f), new Vector2(0.95f, 0.25f), 20, "", new Color(0.7f, 0.95f, 0.7f), FontStyles.Bold, TextAlignmentOptions.Center);
        _spinLbl = MakeButton("Spin", _panel.transform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.185f), new Color(0.22f, 0.45f, 0.28f, 1f), () => DoSpin(true));
        _spinBtn = _spinLbl.transform.parent.GetComponent<Button>();
        _adLbl = MakeButton("Ad", _panel.transform, new Vector2(0.1f, 0.02f), new Vector2(0.7f, 0.095f), new Color(0.22f, 0.38f, 0.5f, 1f), OnAdSpin);
        _adBtn = _adLbl.transform.parent.GetComponent<Button>();
        var close = MakeButton("Close", _panel.transform, new Vector2(0.72f, 0.02f), new Vector2(0.9f, 0.095f), new Color(0.28f, 0.24f, 0.30f, 1f), Close);
        close.text = Loc.T("Закрыть", "Close");

        _panel.SetActive(false);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size, string content, Color color, FontStyles style, TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.text = content; t.fontSize = size; t.alignment = align; t.color = color; t.fontStyle = style;
        t.raycastTarget = false; t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = size;
        return t;
    }

    private TextMeshProUGUI MakeButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform; rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());
        return MakeText("Label", go.transform, Vector2.zero, Vector2.one, 20, "", Color.white, FontStyles.Normal, TextAlignmentOptions.Center);
    }
}
