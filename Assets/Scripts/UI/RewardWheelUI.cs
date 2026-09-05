/// <summary>
/// Батч 16: ежедневное колесо удачи — ТЕПЕРЬ РЕАЛЬНО КРУТИТСЯ. Использует спрайт колеса на 6
/// секторов (UiSkin.wheelSprite = Assets/5052447.png): вращаем Image вокруг Z с замедлением
/// (ease-out) и останавливаем так, чтобы выбранный сектор оказался под указателем сверху.
/// Раз в день бесплатный спин (YG2.saves.wheelLastSpin); второй — за rewarded-рекламу.
/// Панель/кнопки — в стиле Mini UI (UiKit). Сцена: MainScene (рантайм).
/// Зависимости: GameManager, UiEffects, UiKit/UiSkin, Loc, TMPro. SDK: YG2 Rewarded (опц.).
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
    // Порядок = сектора колеса по часовой стрелке от верха (сектор 0 сверху).
    private static readonly Prize[] Prizes =
    {
        new Prize("+120 монет", "+120 coins", 120, 0),
        new Prize("+1 кристалл", "+1 gem", 0, 1),
        new Prize("+250 монет", "+250 coins", 250, 0),
        new Prize("+3 кристалла", "+3 gems", 0, 3),
        new Prize("+400 монет", "+400 coins", 400, 0),
        new Prize("+700 монет", "+700 coins", 700, 0),
    };
    private const int Sectors = 6;
    private const float SectorDeg = 360f / Sectors;

    // Если сектор 0 на спрайте не строго сверху — подстрой этот угол в инспекторе.
    [SerializeField] private float _pointerOffsetDeg = 0f;

    private GameObject _panel, _dim;
    private RectTransform _wheel;
    private Button _spinBtn, _adBtn;
    private TextMeshProUGUI _spinLbl, _adLbl, _status;
    private bool _spinning;

    private void Awake() { if (Instance != null && Instance != this) { Destroy(this); return; } Instance = this; }

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
        if (_status != null) _status.text = "";

        int final = UnityEngine.Random.Range(0, Sectors);
        // Целевой угол: несколько полных оборотов + приведение сектора final под указатель.
        float jitter = UnityEngine.Random.Range(-SectorDeg * 0.35f, SectorDeg * 0.35f);
        float startZ = _wheel != null ? _wheel.localEulerAngles.z : 0f;
        // Нормируем старт в [0,360) и добавляем 4–6 оборотов.
        startZ = Mathf.Repeat(startZ, 360f);
        float turns = UnityEngine.Random.Range(4, 6) * 360f;
        float targetZ = turns + final * SectorDeg + _pointerOffsetDeg + jitter;

        float dur = 3.2f;
        float t = 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / dur);
            // ease-out (быстро → медленно)
            float e = 1f - Mathf.Pow(1f - k, 3f);
            float z = Mathf.Lerp(startZ, startZ + targetZ, e);
            if (_wheel != null) _wheel.localEulerAngles = new Vector3(0f, 0f, z);
            yield return null;
        }
        if (_wheel != null) _wheel.localEulerAngles = new Vector3(0f, 0f, Mathf.Repeat(startZ + targetZ, 360f));

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

    // ─── Построение (Mini UI) ──────────────────────────────────────────────────
    private void Build()
    {
        UiKit.Canvas(transform, 322, "WheelCanvas");
        var canvasT = transform.GetChild(transform.childCount - 1);

        _dim = UiKit.Dim(canvasT, Close);
        _dim.SetActive(false);

        var panel = UiKit.Panel(canvasT, new Vector2(0.3f, 0.14f), new Vector2(0.7f, 0.86f), false, "WheelPanel");
        _panel = panel.gameObject;

        UiKit.Text(_panel.transform, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.98f),
            Loc.T("Колесо удачи", "Wheel of luck"), 30, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.5f), FontStyles.Bold, "Hdr");

        // Колесо (спрайт 5052447) — квадрат в центре.
        var s = UiSkin.Get();
        var wheelGo = new GameObject("Wheel", typeof(RectTransform), typeof(Image));
        wheelGo.transform.SetParent(_panel.transform, false);
        _wheel = (RectTransform)wheelGo.transform;
        _wheel.anchorMin = new Vector2(0.5f, 0.56f); _wheel.anchorMax = new Vector2(0.5f, 0.56f);
        _wheel.sizeDelta = new Vector2(520, 520);
        _wheel.anchoredPosition = Vector2.zero;
        var wImg = wheelGo.GetComponent<Image>();
        if (s != null && s.wheelSprite != null) { wImg.sprite = s.wheelSprite; wImg.preserveAspect = true; }
        else wImg.color = new Color(0.3f, 0.3f, 0.4f, 1f);
        wImg.raycastTarget = false;

        // Указатель сверху (маленький треугольник-маркер).
        var ptr = new GameObject("Pointer", typeof(RectTransform), typeof(Image));
        ptr.transform.SetParent(_panel.transform, false);
        var prt = (RectTransform)ptr.transform;
        prt.anchorMin = new Vector2(0.5f, 0.86f); prt.anchorMax = new Vector2(0.5f, 0.86f);
        prt.sizeDelta = new Vector2(36, 44); prt.anchoredPosition = Vector2.zero;
        var pImg = ptr.GetComponent<Image>();
        pImg.sprite = UiKit.White(); pImg.color = new Color(1f, 0.85f, 0.3f); pImg.raycastTarget = false;

        _status = UiKit.Text(_panel.transform, new Vector2(0.05f, 0.2f), new Vector2(0.95f, 0.27f),
            "", 22, TextAlignmentOptions.Center, new Color(0.75f, 0.97f, 0.75f), FontStyles.Bold, "Status");

        _spinBtn = UiKit.Button(_panel.transform, new Vector2(0.1f, 0.1f), new Vector2(0.9f, 0.185f),
            Loc.T("Крутить (бесплатно)", "Spin (free)"), () => DoSpin(true), true);
        _spinLbl = UiKit.Label(_spinBtn);
        _adBtn = UiKit.Button(_panel.transform, new Vector2(0.1f, 0.02f), new Vector2(0.66f, 0.09f),
            Loc.T("Ещё спин — реклама", "Extra spin — ad"), OnAdSpin);
        _adLbl = UiKit.Label(_adBtn);
        var close = UiKit.Button(_panel.transform, new Vector2(0.68f, 0.02f), new Vector2(0.9f, 0.09f),
            Loc.T("Закрыть", "Close"), Close);

        _panel.SetActive(false);
    }
}
