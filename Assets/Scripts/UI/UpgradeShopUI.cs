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
using System.Collections;
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

    [Header("Батч 11: шкалы мастерства «сейчас/станет» (опц.)")]
    [Tooltip("Заполнение = текущий уровень / максимум. Показывает накопленный эффект.")]
    [SerializeField] private Image[] _effectFills;  // 3 — «сейчас»
    [Tooltip("Полупрозрачный слой = уровень после покупки. Разница = прибавка от улучшения.")]
    [SerializeField] private Image[] _effectGhosts; // 3 — «станет»

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
            SetText(_infoTexts, i, EffectLine(type, level));

            if (_buyButtons != null && i < _buyButtons.Length && _buyButtons[i] != null)
                _buyButtons[i].interactable = cost >= 0 && gm.TotalCoins >= cost;

            SetText(_buyLabels, i, cost < 0 ? Loc.T("Максимум", "Max")
                                            : Loc.T($"Купить — {cost}", $"Buy — {cost}"));

            UpdateBar(i, level); // Батч 11: шкала «сейчас/станет»
        }
    }

    // ─── Батч 11: визуализация мастерства (шкала «сейчас/станет») ──────────────
    private Coroutine[] _fillCo;

    private void UpdateBar(int i, int level)
    {
        float max  = GameManager.UpgradeMaxLevel;
        float cur  = Mathf.Clamp01(level / max);
        bool  maxed = level >= GameManager.UpgradeMaxLevel;
        float next = maxed ? cur : Mathf.Clamp01((level + 1) / max);

        if (_effectGhosts != null && i < _effectGhosts.Length && _effectGhosts[i] != null)
        {
            _effectGhosts[i].gameObject.SetActive(!maxed);       // «станет» скрыто на максимуме
            _effectGhosts[i].fillAmount = next;
        }

        if (_effectFills != null && i < _effectFills.Length && _effectFills[i] != null)
        {
            if (_fillCo == null) _fillCo = new Coroutine[RowCount];
            if (i < _fillCo.Length)
            {
                if (_fillCo[i] != null) StopCoroutine(_fillCo[i]);
                _fillCo[i] = StartCoroutine(AnimateFill(_effectFills[i], cur));
            }
        }
    }

    private IEnumerator AnimateFill(Image img, float target)
    {
        float from = img.fillAmount;
        // При заметном росте (покупка) — плавная анимация; иначе выставляем сразу.
        if (Mathf.Abs(target - from) < 0.001f) { img.fillAmount = target; yield break; }
        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime * 2.5f;
            img.fillAmount = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }
        img.fillAmount = target;
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

    // ─── Показ выгоды в ПРОЦЕНТАХ (насколько лучше работает после улучшения) ──────
    // Вместо «Уровень N/5» показываем понятный игроку эффект: текущий накопленный
    // процент и — если не максимум — насколько станет лучше после покупки.

    // Прирост эффекта за один уровень (в дружелюбных процентах для игрока).
    private static int PercentPerLevel(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.Beans:   return 12; // ровно как в экономике (+12% к оплате/ур)
            case UpgradeType.Machine: return 20; // «легче попасть» — шире зона на кофемашине
            default:                  return 15; // щедрее чаевые от гостей
        }
    }

    private static string EffectWord(UpgradeType t)
    {
        switch (t)
        {
            case UpgradeType.Beans:   return Loc.T("Оплата за напиток", "Payment per drink");
            case UpgradeType.Machine: return Loc.T("Точность на кофемашине", "Machine accuracy");
            default:                  return Loc.T("Чаевые от гостей", "Guest tips");
        }
    }

    private static string EffectLine(UpgradeType t, int level)
    {
        int per    = PercentPerLevel(t);
        int curPct = level * per;
        string word = EffectWord(t);

        if (level >= GameManager.UpgradeMaxLevel)
            return Loc.T($"{word}: +{curPct}% Улучшено полностью", $"{word}: +{curPct}% Fully upgraded");

        int nextPct = (level + 1) * per;
        return Loc.T($"{word}: +{curPct}% Станет +{nextPct}%",
                     $"{word}: +{curPct}% Will be +{nextPct}%");
    }
}
