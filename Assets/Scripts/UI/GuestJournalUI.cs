/// <summary>
/// Батч 6: экран журнала гостей «Завсегдатаи». Делает видимой уже существующую память
/// отношений (симпатию по типу клиента) как коллекционную мета-цель. Данные берутся из
/// GameManager (симпатия, визиты, лучшие звёзды) — новых ассетов не требует.
/// Карточки клонируются из шаблона (_cardTemplate) внутрь _content.
/// Сцена: MainScene (UI). Кнопку «Журнал» удобно разместить на экране результата дня.
/// Зависимости: GameManager, JournalCard, Loc, TMPro
/// SDK: Нет
/// </summary>
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GuestJournalUI : MonoBehaviour
{
    [Header("Панель")]
    [SerializeField] private GameObject _panel;

    [Header("Список карточек")]
    [SerializeField] private Transform   _content;      // контейнер карточек (Content у ScrollView)
    [SerializeField] private JournalCard _cardTemplate; // шаблон карточки внутри _content (выключен)
    [SerializeField] private TextMeshProUGUI _progressText; // «Знакомств: X / N»

    [Header("Кнопки")]
    [SerializeField] private Button _btnOpen;
    [SerializeField] private Button _btnClose;

    private readonly List<JournalCard> _spawned = new List<JournalCard>();

    private void Awake()
    {
        if (_panel != null) _panel.SetActive(false);
        if (_cardTemplate != null) _cardTemplate.gameObject.SetActive(false);
        if (_btnOpen  != null) _btnOpen.onClick.AddListener(Open);
        if (_btnClose != null) _btnClose.onClick.AddListener(Close);
    }

    public void Open()
    {
        Rebuild();
        if (_panel != null) _panel.SetActive(true);

        // Пункт 5: игрок увидел журнал — сбрасываем бейдж «новые гости».
        if (GameManager.Instance != null && YG.YG2.saves != null)
        {
            YG.YG2.saves.journalSeenCount = GameManager.Instance.JournalKeys.Count;
            GameManager.Instance.SaveGame();
        }
    }

    public void Close()
    {
        if (_panel != null) _panel.SetActive(false);
    }

    private void Rebuild()
    {
        var gm = GameManager.Instance;
        if (gm == null || _cardTemplate == null || _content == null) return;

        foreach (var c in _spawned) if (c != null) Destroy(c.gameObject);
        _spawned.Clear();

        List<int> keys = gm.JournalKeys;
        int total = System.Enum.GetValues(typeof(CharacterType)).Length;

        foreach (int key in keys)
        {
            var type = (CharacterType)key;
            JournalCard card = Instantiate(_cardTemplate, _content);
            card.gameObject.SetActive(true);
            card.Bind(type, gm.GetClientSatisfaction(type), gm.GetVisits(type), gm.GetBestStars(type));
            _spawned.Add(card);
        }

        if (_progressText != null)
            _progressText.text = Loc.T("Знакомств: ", "Met: ") + keys.Count + " / " + total;
    }
}
