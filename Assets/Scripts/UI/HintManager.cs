/// <summary>
/// Система подсказок. Показывает заказ гостя за монеты или за просмотр рекламы.
/// Сцена: MainScene
/// Зависимости: GameManager, Loc
/// SDK: YG2 RewardedAdv (опциональный модуль)
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class HintManager : MonoBehaviour
{
    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("UI")]
    [SerializeField] private GameObject          _hintPanel;
    [SerializeField] private TextMeshProUGUI     _hintText;
    [SerializeField] private Button              _btnCoinHint;    // Подсказка за монеты
    [SerializeField] private Button              _btnAdHint;      // Подсказка за рекламу
    [SerializeField] private Button              _btnClose;
    [SerializeField] private TextMeshProUGUI     _coinCostText;
    [SerializeField] private TextMeshProUGUI     _coinsDisplay;   // Текущий баланс
    [SerializeField] private TextMeshProUGUI     _resultText;     // Показываем заказ

    [Header("Настройки")]
    [SerializeField] private int  _coinHintCost  = 10; // Стоимость подсказки в монетах
    [SerializeField] private bool _showAdButton  = true;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private CoffeeOrder _currentOrder;
    private bool        _isWaitingForAd = false;

    // ─── Жизненный цикл ──────────────────────────────────────────────────────

    private void Awake()
    {
        _hintPanel?.SetActive(false);

        _btnCoinHint?.onClick.AddListener(OnCoinHintClicked);
        _btnAdHint  ?.onClick.AddListener(OnAdHintClicked);
        _btnClose   ?.onClick.AddListener(ClosePanel);

        if (_coinCostText != null)
            _coinCostText.text = _coinHintCost + Loc.T(" монет", " coins");

#if RewardedAdv_yg
        // Подписываемся на событие награды
        YG2.onRewardAdv += OnRewardReceived;
#else
        // Если модуль рекламы не установлен — скрываем кнопку
        if (_btnAdHint != null) _btnAdHint.gameObject.SetActive(false);
#endif
    }

    private void OnDestroy()
    {
#if RewardedAdv_yg
        YG2.onRewardAdv -= OnRewardReceived;
#endif
    }

    // ─── Публичное API ───────────────────────────────────────────────────────

    /// <summary>Сообщаем систему о текущем заказе гостя.</summary>
    public void SetCurrentOrder(CoffeeOrder order)
    {
        _currentOrder = order;
    }

    /// <summary>Открывает панель подсказок.</summary>
    public void OpenHintPanel()
    {
        if (_hintPanel == null) return;

        UpdateCoinsDisplay();
        _hintText  ?.SetText(Loc.T("Нужна подсказка?", "Need a hint?"));
        _resultText?.SetText("");

        bool canAfford = GameManager.Instance.TotalCoins >= _coinHintCost;
        if (_btnCoinHint != null) _btnCoinHint.interactable = canAfford;

        _hintPanel.SetActive(true);
    }

    private void ClosePanel()
    {
        _hintPanel?.SetActive(false);
    }

    // ─── Обработчики кнопок ───────────────────────────────────────────────────

    private void OnCoinHintClicked()
    {
        int coins = GameManager.Instance.TotalCoins;

        if (coins < _coinHintCost)
        {
            _hintText?.SetText(Loc.T("Недостаточно монет!", "Not enough coins!"));
            return;
        }

        // Списываем монеты (через отрицательное добавление)
        GameManager.Instance.AddCoins(-_coinHintCost);

        ShowHintText();
        UpdateCoinsDisplay();

        // Кнопку после использования деактивируем
        if (_btnCoinHint != null) _btnCoinHint.interactable = false;
    }

    private void OnAdHintClicked()
    {
#if RewardedAdv_yg
        if (_btnAdHint != null) _btnAdHint.interactable = false;
        _hintText?.SetText(Loc.T("Загружается реклама...", "Loading ad..."));
        _isWaitingForAd = true;
        YG2.RewardedAdvShow("hint_reward");
#else
        _hintText?.SetText(Loc.T("Реклама недоступна.", "Ads unavailable."));
#endif
    }

#if RewardedAdv_yg
    private void OnRewardReceived(string id)
    {
        if (id != "hint_reward" || !_isWaitingForAd) return;
        _isWaitingForAd = false;

        ShowHintText();

        if (_btnAdHint != null) _btnAdHint.interactable = true;
        _hintText?.SetText(Loc.T("Спасибо за просмотр!", "Thanks for watching!"));
    }
#endif

    // ─── Отображение подсказки ────────────────────────────────────────────────

    private void ShowHintText()
    {
        if (_currentOrder == null)
        {
            _resultText?.SetText(Loc.T("Заказ пока неизвестен.", "The order is not known yet."));
            return;
        }

        // Пункты 2,3: описываем заказ по РЕАЛЬНОМУ сосуду-ингредиенту и с топпингом
        // на текущем языке (берём готовое описание из системы готовки).
        string wish = CoffeeCraftingSystem.Instance != null
            ? CoffeeCraftingSystem.Instance.DescribeOrderForHint()
            : _currentOrder.GetDisplayName();
        if (string.IsNullOrEmpty(wish)) wish = _currentOrder.GetDisplayName();

        string hint = Loc.T("Гость хочет:", "The guest wants:") + $"\n<b>{wish}</b>";
        _resultText?.SetText(hint);
    }

    private void UpdateCoinsDisplay()
    {
        if (_coinsDisplay != null)
            _coinsDisplay.text = Loc.T("У тебя: ", "You have: ") + (GameManager.Instance?.TotalCoins ?? 0) + Loc.T(" монет", " coins");
    }
}
