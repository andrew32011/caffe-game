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

    [Header("Дополнительно")]
    [SerializeField] private Image   _dayStarImage;    // Звёзда (полная/неполная)
    [SerializeField] private Sprite  _starFull;
    [SerializeField] private Sprite  _starEmpty;

    // ─── Состояние ───────────────────────────────────────────────────────────

    public bool IsShowing { get; private set; } = false;

    // ─── Инициализация ───────────────────────────────────────────────────────

    private void Awake()
    {
        _resultPanel?.SetActive(false);
        _btnContinue?.onClick.AddListener(OnContinueClicked);
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

        _resultPanel?.SetActive(true);
        StartCoroutine(FadeIn());
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
