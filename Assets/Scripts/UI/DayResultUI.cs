/// <summary>
/// Экран результатов дня. Показывает номер дня, заработок, текст итогов.
/// Сцена: MainScene
/// Зависимости: GameManager
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class DayResultUI : MonoBehaviour
{
    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Панель")]
    [SerializeField] private GameObject     _resultPanel;
    [SerializeField] private CanvasGroup    _canvasGroup;

    [Header("Текст")]
    [SerializeField] private TextMeshProUGUI _dayNumberText;
    [SerializeField] private TextMeshProUGUI _coinsEarnedText;
    [SerializeField] private TextMeshProUGUI _totalCoinsText;
    [SerializeField] private TextMeshProUGUI _dayEndText;

    [Header("Кнопки")]
    [SerializeField] private Button _btnContinue;

    [Header("×2 заработок за рекламу (Батч 2)")]
    [SerializeField] private Button _btnDouble;
    [SerializeField] private TextMeshProUGUI _doubleLabel;

    [Header("Сохранить комбо за рекламу (Батч 6)")]
    [Tooltip("Переносит серию комбо на следующий день. Доступна только при серии ≥ порога.")]
    [SerializeField] private Button _btnSaveCombo;
    [SerializeField] private TextMeshProUGUI _saveComboLabel;
    [SerializeField] private int _comboSaveMinStreak = 3;

    [Header("Магазин апгрейдов (Батч 3)")]
    [SerializeField] private Button _btnShop;
    [SerializeField] private UpgradeShopUI _upgradeShop;

    [Header("Дополнительно")]
    [SerializeField] private Image   _dayStarImage;    // Звёзда (полная/неполная)
    [SerializeField] private Sprite  _starFull;
    [SerializeField] private Sprite  _starEmpty;

    [Header("Трекер «Путь к 10 000» (Батч 6)")]
    [Tooltip("Опциональные поля: если не заданы — трекер просто не показывается.")]
    [SerializeField] private TextMeshProUGUI _journeyProgressText; // «1234 / 10000»
    [SerializeField] private TextMeshProUGUI _journeyForecastText; // строка-прогноз темпа
    [SerializeField] private Image           _journeyFill;         // полоса прогресса (Filled, Horizontal)

    [Header("Предупреждение о стрике (Батч 6)")]
    [Tooltip("Опц.: строка-напоминание «стрик сгорит» при стрике входа ≥3.")]
    [SerializeField] private TextMeshProUGUI _streakWarningText;

    // ─── Состояние ───────────────────────────────────────────────────────────

    public bool IsShowing { get; private set; } = false;

    private const string RewardId = "day_double";
    private int  _lastEarned;
    private bool _doubled;
    private bool _waitingAd;

    private const string ComboRewardId = "combo_save";
    private int  _lastCombo;
    private bool _comboSaved;
    private bool _waitingCombo;

    // ─── Инициализация ───────────────────────────────────────────────────────

    private void Awake()
    {
        _resultPanel?.SetActive(false);
        _btnContinue?.onClick.AddListener(OnContinueClicked);
        _btnDouble?.onClick.AddListener(OnDoubleClicked);
        _btnShop?.onClick.AddListener(OnShopClicked);
        _btnSaveCombo?.onClick.AddListener(OnSaveComboClicked);
#if RewardedAdv_yg
        YG2.onRewardAdv += OnReward;
#else
        if (_btnDouble != null) _btnDouble.gameObject.SetActive(false);
        if (_btnSaveCombo != null) _btnSaveCombo.gameObject.SetActive(false);
#endif
    }

    private void OnDestroy()
    {
#if RewardedAdv_yg
        YG2.onRewardAdv -= OnReward;
#endif
    }

    // ─── Публичное API ───────────────────────────────────────────────────────

    /// <summary>Показывает экран результатов дня.</summary>
    public void Show(int dayNumber, int coinsEarned, string summaryText, int comboCount = 0)
    {
        IsShowing = true;

        if (_dayNumberText  != null) _dayNumberText.text  = Loc.T($"ДЕНЬ {dayNumber} ЗАВЕРШЁН", $"DAY {dayNumber} COMPLETE");
        if (_coinsEarnedText!= null) _coinsEarnedText.text= "+" + coinsEarned + Loc.T(" монет", " coins");
        if (_totalCoinsText != null) _totalCoinsText.text = Loc.T("Всего: ", "Total: ") + (GameManager.Instance?.TotalCoins ?? 0) + Loc.T(" монет", " coins");
        if (_dayEndText     != null) _dayEndText.text     = summaryText;

        if (_dayStarImage != null && _starFull != null)
            _dayStarImage.sprite = coinsEarned > 0 ? _starFull : _starEmpty;

        UpdateJourneyTracker(dayNumber); // Батч 6: «Путь к 10 000» с прогнозом темпа

        // Батч 6: предупреждение о риске сгорания daily-стрика (loss aversion).
        if (_streakWarningText != null)
        {
            int streak = GameManager.Instance != null ? GameManager.Instance.DailyBonusStreak : 0;
            bool warn = streak >= 3;
            _streakWarningText.gameObject.SetActive(warn);
            if (warn)
                _streakWarningText.text = Loc.T($"Стрик входа: {streak} дней Загляни завтра — иначе сгорит!",
                                                $"Login streak: {streak} days Come back tomorrow or lose it!");
        }

        // ×2 за рекламу: доступно только если за день заработано > 0 (Батч 2).
        _lastEarned = coinsEarned;
        _doubled = false;
        _waitingAd = false;
        if (_btnDouble != null)
        {
#if RewardedAdv_yg
            _btnDouble.gameObject.SetActive(coinsEarned > 0);
            _btnDouble.interactable = coinsEarned > 0;
            if (_doubleLabel != null)
                _doubleLabel.text = Loc.T($"Удвоить (+{coinsEarned}) — реклама", $"Double (+{coinsEarned}) — ad");
#else
            _btnDouble.gameObject.SetActive(false);
#endif
        }
        StartDoublePulse(); // Батч 6: дожим — мягкая пульсация «Удвоить»

        // Батч 6: «Сохранить комбо» — доступно только при достойной серии (≥ порога),
        // иначе кнопка не эмоциональна. Переносит серию на следующий день (rewarded).
        _lastCombo   = comboCount;
        _comboSaved  = false;
        _waitingCombo = false;
        if (_btnSaveCombo != null)
        {
#if RewardedAdv_yg
            bool eligible = comboCount >= _comboSaveMinStreak;
            _btnSaveCombo.gameObject.SetActive(eligible);
            _btnSaveCombo.interactable = eligible;
            if (eligible && _saveComboLabel != null)
                _saveComboLabel.text = Loc.T($"Сохранить серию ×{comboCount}", $"Keep streak ×{comboCount}");
#else
            _btnSaveCombo.gameObject.SetActive(false);
#endif
        }

        _resultPanel?.SetActive(true);
        StartCoroutine(FadeIn());
    }

    private void OnSaveComboClicked()
    {
#if RewardedAdv_yg
        if (_comboSaved || _waitingCombo || _lastCombo < _comboSaveMinStreak) return;
        _waitingCombo = true;
        YG2.RewardedAdvShow(ComboRewardId);
#endif
    }

    // ─── Батч 6: трекер «Путь к 10 000» с прогнозом темпа ─────────────────────
    private void UpdateJourneyTracker(int day)
    {
        int total    = GameManager.Instance?.TotalCoins ?? 0;
        int goal     = CoinsUI.JourneyGoal;      // 10000
        int finalDay = Difficulty.FinalDay;      // 40

        if (_journeyFill != null)
            _journeyFill.fillAmount = Mathf.Clamp01((float)total / Mathf.Max(1, goal));
        if (_journeyProgressText != null)
            _journeyProgressText.text = total + " / " + goal;

        if (_journeyForecastText == null) return;

        // Цель уже достигнута
        if (total >= goal)
        {
            _journeyForecastText.color = new Color(0.35f, 0.85f, 0.40f);
            _journeyForecastText.text  = Loc.T("Цель достигнута!", "Goal reached!");
            return;
        }

        // Ранние дни: EarlyEase (×1.5→1.0 к дню 6) искажает прогноз — не пугаем.
        if (day < 7)
        {
            _journeyForecastText.color = new Color(0.80f, 0.80f, 0.85f);
            _journeyForecastText.text  = Loc.T("Разгоняемся…", "Warming up…");
            return;
        }

        int   daysLeft     = Mathf.Max(1, finalDay - day);
        float needPerDay   = (goal - total) / (float)daysLeft;
        float actualPerDay = total / (float)Mathf.Max(1, day);

        if (actualPerDay >= needPerDay * 1.05f)
        {
            _journeyForecastText.color = new Color(0.35f, 0.85f, 0.40f);
            _journeyForecastText.text  = Loc.T("Опережаешь график", "Ahead of schedule");
        }
        else if (actualPerDay >= needPerDay * 0.90f)
        {
            _journeyForecastText.color = new Color(0.95f, 0.85f, 0.30f);
            _journeyForecastText.text  = Loc.T("Идёшь впритык", "Right on the edge");
        }
        else
        {
            int gap = Mathf.CeilToInt(needPerDay - actualPerDay);
            _journeyForecastText.color = new Color(0.90f, 0.40f, 0.40f);
            _journeyForecastText.text  = Loc.T($"Отставание ~{gap}/день — удвой выручку",
                                               $"Behind ~{gap}/day — double your earnings");
        }
    }

    private void OnDoubleClicked()
    {
#if RewardedAdv_yg
        if (_doubled || _waitingAd || _lastEarned <= 0) return;
        _waitingAd = true;
        YG2.RewardedAdvShow(RewardId);
#endif
    }

#if RewardedAdv_yg
    private void OnReward(string id)
    {
        // ×2 дневной заработок (Батч 2)
        if (id == RewardId && _waitingAd)
        {
            _waitingAd = false;
            _doubled = true;

            GameManager.Instance?.AddCoins(_lastEarned);   // удваиваем дневной заработок
            GameManager.Instance?.SaveGame();              // сохраняем сразу (требование 1.9)
            AudioController.Instance?.PlayCoin();
            UiEffects.Instance?.CoinBurst(_lastEarned);

            if (_totalCoinsText != null)
                _totalCoinsText.text = Loc.T("Всего: ", "Total: ") + (GameManager.Instance?.TotalCoins ?? 0) + Loc.T(" монет", " coins");
            if (_btnDouble != null) _btnDouble.interactable = false;
            if (_doubleLabel != null) _doubleLabel.text = Loc.T("Удвоено!", "Doubled!");
            StopDoublePulse();
            return;
        }

        // Сохранить комбо на следующий день (Батч 6)
        if (id == ComboRewardId && _waitingCombo)
        {
            _waitingCombo = false;
            _comboSaved = true;

            GameManager.Instance?.SetCarriedCombo(_lastCombo); // перенос серии на завтра
            GameManager.Instance?.SaveGame();
            AudioController.Instance?.PlayStar();

            if (_btnSaveCombo != null) _btnSaveCombo.interactable = false;
            if (_saveComboLabel != null) _saveComboLabel.text = Loc.T("Серия сохранена!", "Streak saved!");
        }
    }
#endif

    private void OnShopClicked()
    {
        _upgradeShop?.Open(); // Батч 3: магазин апгрейдов кофейни
    }

    private void OnContinueClicked()
    {
        StartCoroutine(FadeOutAndClose());
    }

    private IEnumerator FadeIn()
    {
        if (_canvasGroup == null) yield break;

        _canvasGroup.alpha = 0f;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 3f;
            _canvasGroup.alpha = Mathf.Clamp01(t);
            yield return null;
        }
    }

    // ─── Батч 6: пульс-дожим кнопки «Удвоить» ─────────────────────────────────
    private Coroutine _doublePulseCo;

    private void StartDoublePulse()
    {
        StopDoublePulse();
        if (_btnDouble != null && _btnDouble.gameObject.activeSelf)
            _doublePulseCo = StartCoroutine(DoublePulseRoutine());
    }

    private void StopDoublePulse()
    {
        if (_doublePulseCo != null) { StopCoroutine(_doublePulseCo); _doublePulseCo = null; }
        if (_btnDouble != null) _btnDouble.transform.localScale = Vector3.one;
    }

    private IEnumerator DoublePulseRoutine()
    {
        Transform tr = _btnDouble.transform;
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * 3f;
            float s = 1f + 0.06f * Mathf.Sin(t);
            tr.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }

    private IEnumerator FadeOutAndClose()
    {
        StopDoublePulse();

        if (_canvasGroup != null)
        {
            float t = 1f;
            while (t > 0f)
            {
                t -= Time.deltaTime * 4f;
                _canvasGroup.alpha = Mathf.Clamp01(t);
                yield return null;
            }
        }

        _resultPanel?.SetActive(false);
        IsShowing = false;
    }
}
