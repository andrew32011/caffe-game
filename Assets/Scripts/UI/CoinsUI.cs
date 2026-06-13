/// <summary>
/// Индикатор денег кофейни (сверху сбоку, пункт 5). Каждый кадр показывает
/// текущий баланс GameManager.TotalCoins.
/// Сцена: MainScene (UI на Canvas)
/// Зависимости: GameManager, TMPro
/// SDK: Нет
/// </summary>
using UnityEngine;
using TMPro;

public class CoinsUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text;

    private int _last = int.MinValue;

    private void Reset() { _text = GetComponent<TextMeshProUGUI>(); }

    private void Update()
    {
        if (_text == null || GameManager.Instance == null) return;
        int coins = GameManager.Instance.TotalCoins;
        if (coins == _last) return;
        _last = coins;
        _text.text = Loc.T("Касса: ", "Cash: ") + coins;
    }
}
