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

    [Header("Магазин апгрейдов (Батч 3)")]
    [SerializeField] private Button _btnShop;
    [SerializeField] private UpgradeShopUI _upgradeShop;

    [Header("Дополнительно")]
    [SerializeField] private Image   _dayStarImage;    // Звёзда (полная/неполная)
    [SerializeField] private Sprite  _starFull;
    [SerializeField] private Sprite  _starEmpty;

    // ─── Состояние ───────────────────────────────────────────────────────────

    public bool IsShowing { get; private set; } = false;

    private const string RewardId = "day_double";
    private int  _lastEarned;
    private bool _doubled;
    private bool _waitingAd;

    // ─── Инициализация ───────────────────────────────────────────────────────

    private void Awake()
    {
        _resultPanel?.SetActive(false);
        _btnContinue?.onClick.AddListener(OnContinueClicked);
        _btnDouble?.onClick.AddListener(OnDoubleClicked);
        _btnShop?.onClick.AddListener(OnShopClicked);
#if RewardedAdv_yg
        YG2.onRewardAdv += OnReward;
#else
        if (_btnDouble != null) _btnDouble.gameObject.SetActive(false);
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
    public void Show(int dayNumber, int coinsEarned, string summaryText)
    {
        IsShowing = true;

        if (_dayNumberText  != null) _dayNumberText.text  = Loc.T($"ДЕНЬ {dayNumber} ЗАВЕРШЁН", $"DAY {dayNumber} COMPLETE");
        if (_coinsEarnedText!= null) _coinsEarnedText.text= "+" + coinsEarned + Loc.T(" монет", " coins");
        if (_totalCoinsText != null) _totalCoinsText.text = Loc.T("Всего: ", "Total: ") + (GameManager.Instance?.TotalCoins ?? 0) + Loc.T(" монет", " coins");
        if (_dayEndText     != null) _dayEndText.text     = summaryText;

        if (_dayStarImage != null && _starFull != null)
            _dayStarImage.sprite = coinsEarned > 0 ? _starFull : _starEmpty;

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

        _resultPanel?.SetActive(true);
        StartCoroutine(FadeIn());
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
        if (id != RewardId || !_waitingAd) return;
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

    private IEnumerator FadeOutAndClose()
    {
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
