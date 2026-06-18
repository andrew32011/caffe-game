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

    private const string RewardId = "daily_double";

    private bool _claimed;
    private bool _doubled;
    private bool _waitingAd;
    private int  _reward;

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

        if (_titleText  != null) _titleText.text  = Loc.T($"Бонус за вход — день {streak}", $"Login bonus — day {streak}");
        if (_rewardText != null) _rewardText.text = "+" + _reward + Loc.T(" монет", " coins");
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
