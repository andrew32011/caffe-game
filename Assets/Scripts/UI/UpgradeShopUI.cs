/// <summary>
/// Батч 3: магазин улучшений кофейни. Открывается с экрана итогов дня — естественный
/// момент потратить заработок. Три постоянных апгрейда за монеты:
///   • Зёрна высшего сорта — +оплата за напиток;
///   • Профи-кофемашина    — шире допуск на минигейме (легче попасть);
///   • Программа лояльности — щедрее чаевые от гостей.
/// Уровни и баланс хранятся в облачном сейве (GameManager → SavesYG), покупка
/// сохраняется сразу (требование 1.9). Каждый ряд: название, эффект+уровень, кнопка покупки.
/// Сцена: MainScene (UI на Canvas)
/// Зависимости: GameManager, AudioController, UiEffects, Loc, TMPro
/// SDK: Нет
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeShopUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _closeButton;

    [Header("Ряды апгрейдов (порядок = UpgradeType: Beans, Machine, Loyalty)")]
    [SerializeField] private TextMeshProUGUI[] _titleTexts; // 3
    [SerializeField] private TextMeshProUGUI[] _infoTexts;  // 3
    [SerializeField] private Button[]          _buyButtons; // 3
    [SerializeField] private TextMeshProUGUI[] _buyLabels;  // 3

    private const int RowCount = 3;

    private void Awake()
    {
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_buyButtons != null)
        {
            for (int i = 0; i < _buyButtons.Length; i++)
            {
                int idx = i; // захват по значению
                if (_buyButtons[i] != null) _buyButtons[i].onClick.AddListener(() => OnBuy(idx));
            }
        }
        if (_panel != null) _panel.SetActive(false);
    }

    /// <summary>Открывает магазин и обновляет цены/уровни.</summary>
    public void Open()
    {
        if (_panel != null) _panel.SetActive(true);
        Refresh();
    }

    public void Close()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    private void OnBuy(int index)
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        if (gm.TryBuyUpgrade((UpgradeType)index))
        {
            AudioController.Instance?.PlayCoin();
            UiEffects.Instance?.FloatingText(Loc.T("Улучшено!", "Upgraded!"), new Color(0.6f, 1f, 0.6f));
        }
        else
        {
            AudioController.Instance?.PlayWrongOrder(); // не хватает монет или уже максимум
        }
        Refresh();
    }

    private void Refresh()
    {
        var gm = GameManager.Instance;
        if (gm == null) return;

        for (int i = 0; i < RowCount; i++)
        {
            var type  = (UpgradeType)i;
            int level = gm.GetUpgradeLevel(type);
            int cost  = gm.GetUpgradeCost(type);

            SetText(_titleTexts, i, UpgradeName(type));
            SetText(_infoTexts, i, UpgradeDesc(type) + "\n" +
                Loc.T($"Уровень {level}/{GameManager.UpgradeMaxLevel}", $"Level {level}/{GameManager.UpgradeMaxLevel}"));

            if (_buyButtons != null && i < _buyButtons.Length && _buyButtons[i] != null)
                _buyButtons[i].interactable = cost >= 0 && gm.TotalCoins >= cost;

            SetText(_buyLabels, i, cost < 0 ? Loc.T("Максимум", "Max")
                                            : Loc.T($"Купить — {cost}", $"Buy — {cost}"));
        }
    }

    private static void SetText(TextMeshProUGUI[] arr, int i, string text)
    {
        if (arr != null && i < arr.Length && arr[i] != null) arr[i].text = text;
    }

    private static string UpgradeName(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.Beans:   return Loc.T("Зёрна высшего сорта", "Premium beans");
            case UpgradeType.Machine: return Loc.T("Профи-кофемашина", "Pro machine");
            default:                  return Loc.T("Программа лояльности", "Loyalty program");
        }
    }

    private static string UpgradeDesc(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.Beans:   return Loc.T("+12% к оплате за каждый уровень", "+12% payment per level");
            case UpgradeType.Machine: return Loc.T("Шире допуск на кофемашине", "Wider machine tolerance");
            default:                  return Loc.T("Щедрее чаевые от гостей", "More generous tips");
        }
    }
}
