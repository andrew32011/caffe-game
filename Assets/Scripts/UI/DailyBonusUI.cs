/// <summary>
/// Батч 2: ежедневный бонус за вход — ключевая механика удержания на Яндекс Играх.
/// Раз в календарный день показывает растущий бонус (за серию заходов подряд) и
/// предлагает удвоить его за просмотр rewarded-видео. Дата и серия хранятся в облачном
/// сейве (GameManager → SavesYG), начисление сохраняется сразу (требование 1.9).
/// Сцена: MainScene (UI на Canvas)
/// Зависимости: GameManager, Loc, TMPro
/// SDK: YG2 RewardedAdv (опционально — без модуля кнопка ×2 скрыта)
/// </summary>
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class DailyBonusUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _titleText;
    [SerializeField] private TextMeshProUGUI _rewardText;
    [SerializeField] private Button _claimButton;
    [SerializeField] private TextMeshProUGUI _claimLabel;
    [SerializeField] private Button _doubleButton;
    [SerializeField] private TextMeshProUGUI _doubleLabel;

    [Header("Награда")]
    [SerializeField] private int _baseReward = 50;
    [SerializeField] private int _perDayStep = 25;
    [SerializeField] private int _maxReward  = 250;

    [Header("Батч 13: 7-дневный календарь + джекпот")]
    [Tooltip("Кристаллы-джекпот на 7-й день цикла серии заходов.")]
    [SerializeField] private int _jackpotGems = 25;

    private const string RewardId = "daily_double";
    private const int CycleLen = 7;

    private bool _claimed;
    private bool _doubled;
    private bool _waitingAd;
    private int  _reward;
    private int  _cycleDay;   // позиция в 7-дневном цикле (1..7)
    private bool _jackpot;    // сегодня 7-й день → кристаллы

    // Календарь (строится в коде один раз, поверх панели билдера).
    private bool _calBuilt;
    private Image[] _cellBg = new Image[CycleLen];

    /// <summary>Показывает бонус, если сегодня его ещё не получали. Awaitable.</summary>
    public IEnumerator RunIfDue()
    {
        var gm = GameManager.Instance;
        if (gm == null || _panel == null) yield break;

        string today = DateTime.Now.ToString("yyyyMMdd");
        if (gm.DailyBonusLastDate == today) yield break; // уже получали сегодня

        // Серия: +1, если заходили вчера; иначе серия сбрасывается.
        int streak = 1;
        if (!string.IsNullOrEmpty(gm.DailyBonusLastDate) &&
            DateTime.TryParseExact(gm.DailyBonusLastDate, "yyyyMMdd", null,
                System.Globalization.DateTimeStyles.None, out var last))
        {
            int days = (DateTime.Now.Date - last.Date).Days;
            streak = days == 1 ? gm.DailyBonusStreak + 1 : 1;
        }

        _reward  = Mathf.Min(_maxReward, _baseReward + (streak - 1) * _perDayStep);
        _claimed = _doubled = _waitingAd = false;

        // Батч 13: позиция в 7-дневном цикле; 7-й день — джекпот-кристаллы.
        _cycleDay = ((streak - 1) % CycleLen) + 1;
        _jackpot  = _cycleDay == CycleLen;
        BuildCalendar();
        HighlightCalendar(_cycleDay);

        if (_titleText  != null) _titleText.text  = Loc.T($"Бонус за вход — день {streak}", $"Login bonus — day {streak}");
        if (_rewardText != null)
            _rewardText.text = _jackpot
                ? "+" + _reward + Loc.T(" монет", " coins") + "  +" + _jackpotGems + Loc.T(" кристаллов!", " gems!")
                : "+" + _reward + Loc.T(" монет", " coins");
        if (_claimLabel != null) _claimLabel.text = Loc.T("Забрать", "Claim");
        if (_doubleLabel!= null) _doubleLabel.text= Loc.T($"Удвоить (+{_reward}) — реклама", $"Double (+{_reward}) — ad");

        bool adAvailable = false;
#if RewardedAdv_yg
        adAvailable = true;
        YG2.onRewardAdv += OnReward;
#endif
        if (_doubleButton != null) _doubleButton.gameObject.SetActive(adAvailable);

        UnityEngine.Events.UnityAction onClaim  = () => _claimed = true;
        UnityEngine.Events.UnityAction onDouble = OnDoubleClicked;
        _claimButton ?.onClick.AddListener(onClaim);
        _doubleButton?.onClick.AddListener(onDouble);

        _panel.SetActive(true);
        AudioController.Instance?.PlayBonus();

        // Ждём, пока игрок заберёт бонус (после удвоения тоже закрываем по «Забрать»).
        while (!_claimed) yield return null;

        // Фиксируем дату/серию ТОЛЬКО при получении (закрыл вкладку до «Забрать» —
        // бонус не сгорит, покажем снова), затем начисляем и сохраняем.
        gm.DailyBonusLastDate = today;
        gm.DailyBonusStreak   = streak;
        gm.AddCoins(_reward);
        if (_doubled) gm.AddCoins(_reward);
        // Батч 13: джекпот-кристаллы на 7-й день цикла (не-платный источник премиума).
        if (_jackpot)
        {
            gm.AddGems(_jackpotGems);
            UiEffects.Instance?.FloatingText("+" + _jackpotGems + Loc.T(" кристаллов", " gems"), new Color(0.5f, 0.8f, 1f));
        }
        gm.SaveGame();
        AudioController.Instance?.PlayCoin();
        UiEffects.Instance?.FloatingText("+" + (_doubled ? _reward * 2 : _reward), new Color(1f, 0.85f, 0.25f));

        _panel.SetActive(false);
        _claimButton ?.onClick.RemoveListener(onClaim);
        _doubleButton?.onClick.RemoveListener(onDouble);
#if RewardedAdv_yg
        YG2.onRewardAdv -= OnReward;
#endif
    }

    // ─── Батч 13: 7-дневный календарь (строится в коде поверх панели билдера) ──
    private void BuildCalendar()
    {
        if (_calBuilt || _panel == null) return;
        _calBuilt = true;

        var probe = _titleText != null ? _titleText.font : null;
        const float x0 = 0.06f, x1 = 0.94f, yb = 0.62f, yt = 0.77f;
        float w = (x1 - x0) / CycleLen;

        for (int i = 0; i < CycleLen; i++)
        {
            bool last = i == CycleLen - 1;
            var cell = new GameObject($"CalCell{i}", typeof(RectTransform), typeof(Image));
            cell.transform.SetParent(_panel.transform, false);
            var rt = (RectTransform)cell.transform;
            rt.anchorMin = new Vector2(x0 + w * i + 0.006f, yb);
            rt.anchorMax = new Vector2(x0 + w * (i + 1) - 0.006f, yt);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var img = cell.GetComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.10f);
            img.raycastTarget = false;
            _cellBg[i] = img;

            // Верх: номер дня. Низ: тип награды (кристаллы на 7-й день).
            MakeCellText($"Num{i}", cell.transform, new Vector2(0.04f, 0.44f), new Vector2(0.96f, 0.98f), 18,
                (i + 1).ToString(), last ? new Color(0.6f, 0.85f, 1f) : Color.white, probe, FontStyles.Bold);
            MakeCellText($"Rw{i}", cell.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.44f), 13,
                last ? Loc.T("крист.", "gems") : Loc.T("монеты", "coins"),
                last ? new Color(0.6f, 0.85f, 1f) : new Color(0.85f, 0.85f, 0.9f), probe, FontStyles.Normal);
        }
    }

    private void HighlightCalendar(int cycleDay)
    {
        for (int i = 0; i < CycleLen; i++)
        {
            if (_cellBg[i] == null) continue;
            bool current = (i + 1) == cycleDay;
            bool jackpot = i == CycleLen - 1;
            _cellBg[i].color = current
                ? new Color(0.95f, 0.8f, 0.3f, 0.85f)                 // текущий день — золотой
                : (jackpot ? new Color(0.25f, 0.45f, 0.7f, 0.45f)     // джекпот-ячейка — синеватая
                           : new Color(1f, 1f, 1f, 0.10f));
        }
    }

    private void MakeCellText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size,
        string content, Color color, TMP_FontAsset font, FontStyles style)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (font != null) t.font = font;
        t.text = content; t.fontSize = size; t.alignment = TextAlignmentOptions.Center; t.color = color;
        t.fontStyle = style; t.raycastTarget = false; t.enableAutoSizing = true; t.fontSizeMin = 8; t.fontSizeMax = size;
    }

    private void OnDoubleClicked()
    {
#if RewardedAdv_yg
        if (_doubled || _waitingAd) return;
        _waitingAd = true;
        YG2.RewardedAdvShow(RewardId);
#endif
    }

#if RewardedAdv_yg
    private void OnReward(string id)
    {
        if (id != RewardId || !_waitingAd) return;
        _waitingAd = false;
        _doubled = true;
        if (_doubleButton != null) _doubleButton.interactable = false;
        if (_rewardText != null) _rewardText.text = "+" + (_reward * 2) + Loc.T(" монет ×2!", " coins ×2!");
        AudioController.Instance?.PlayBonus();
    }
#endif
}
