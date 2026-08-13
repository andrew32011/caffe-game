/// <summary>
/// Пункт 1: «гейт путешествия» перед финалом. Если игрок не накопил нужную сумму
/// (CoinsUI.JourneyGoal), предлагаем два пути:
///   • начать заново с 1-го дня, сохранив накопленное, и продолжить копить;
///   • купить монеты за реальные деньги (Yandex IAP, модуль Payments YG2).
/// Сцена: MainScene (UI на Canvas)
/// Зависимости: GameManager, Loc, TMPro
/// SDK: YG2 Payments (опциональный модуль). Без модуля кнопка покупки начисляет
///      монеты напрямую — только для теста в редакторе.
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class JourneyGateUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private Button _restartButton;
    [SerializeField] private Button _buyButton;

    [Header("Покупка монет (Yandex IAP)")]
    [Tooltip("ID товара в каталоге Яндекс Игр (создаётся в консоли разработчика).")]
    [SerializeField] private string _coinProductId = "coins_pack";
    [Tooltip("Сколько монет начислять за одну покупку.")]
    [SerializeField] private int _coinAmountPerPurchase = 2000;

    /// <summary>Игрок выбрал «начать заново» (а не докупил монеты).</summary>
    public bool RestartChosen { get; private set; }

    /// <summary>Показывает гейт и ждёт, пока игрок либо начнёт заново, либо докупит
    /// монет до цели. Awaitable.</summary>
    public IEnumerator Run(int goal)
    {
        RestartChosen = false;
        bool restart = false;

        UnityEngine.Events.UnityAction onRestart = () => restart = true;
        UnityEngine.Events.UnityAction onBuy = BuyCoins;
        if (_restartButton != null) _restartButton.onClick.AddListener(onRestart);
        if (_buyButton != null)     _buyButton.onClick.AddListener(onBuy);
#if Payments_yg
        YG2.onPurchaseSuccess += OnPurchaseSuccess;
#endif

        if (_text != null)
            _text.text = Loc.T(
                $"Чтобы отправиться в путешествие за ним, нужно {goal} монет.\n\nНачать заново с первого дня и продолжить копить — или купить монеты?",
                $"To set out on the journey after him you need {goal} coins.\n\nStart over from day one and keep saving — or buy coins?");
        UpdateBuyLabel();
        if (_panel != null) _panel.SetActive(true);

        while (!restart && (GameManager.Instance == null || GameManager.Instance.TotalCoins < goal))
            yield return null;

        RestartChosen = restart;

        if (_panel != null) _panel.SetActive(false);
        if (_restartButton != null) _restartButton.onClick.RemoveListener(onRestart);
        if (_buyButton != null)     _buyButton.onClick.RemoveListener(onBuy);
#if Payments_yg
        YG2.onPurchaseSuccess -= OnPurchaseSuccess;
#endif
    }

    private void BuyCoins()
    {
#if Payments_yg
        YG2.BuyPayments(_coinProductId);
#else
        // Модуль Payments не установлен — для теста начисляем монеты напрямую.
        GameManager.Instance?.AddCoins(_coinAmountPerPurchase);
        GameManager.Instance?.SaveGame();
        Debug.Log("JourneyGate: модуль Payments YG2 не установлен — монеты начислены напрямую (тест). " +
                  "Установите модуль Payments и создайте товар в консоли Яндекс Игр для реальных покупок.");
#endif
    }

#if Payments_yg
    private void OnPurchaseSuccess(string id)
    {
        if (id == _coinProductId)
        {
            Analytics.Bought(_coinProductId); // метрика: покупка пакета монет
            GameManager.Instance?.AddCoins(_coinAmountPerPurchase);
            GameManager.Instance?.SaveGame();          // сохраняем покупку сразу (требование 1.13.3)
            YG2.ConsumePurchaseByID(_coinProductId);   // консумируем товар (требование 1.13.1) — иначе нельзя купить повторно
        }
    }
#endif

    private void UpdateBuyLabel()
    {
        if (_buyButton == null) return;
        var label = _buyButton.GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
            label.text = Loc.T($"Купить {_coinAmountPerPurchase} монет",
                               $"Buy {_coinAmountPerPurchase} coins");
    }
}
