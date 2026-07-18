/// <summary>
/// Бейдж «новые гости» на кнопке журнала (пункт 5). Показывает красный кружок с числом
/// ещё не просмотренных записей журнала и мягко пульсирует кнопкой, приглашая заглянуть.
/// Сбрасывается, когда игрок открывает журнал (GuestJournalUI.Open → journalSeenCount).
/// Бейдж строится в коде — вешается билдером на кнопку BtnJournal.
/// Сцена: MainScene
/// Зависимости: GameManager (JournalKeys), YG2.saves.journalSeenCount, TMPro
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

[RequireComponent(typeof(RectTransform))]
public class JournalBadge : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset _font;

    private GameObject _badge;
    private TextMeshProUGUI _text;
    private Coroutine _pulseCo;
    private int _shown = -1;

    private void Start()
    {
        BuildBadge();
        Refresh(NewCount());
    }

    private void OnEnable()
    {
        // При повторном включении кнопки — пересчитать сразу.
        _shown = -1;
    }

    private void Update()
    {
        int n = NewCount();
        if (n != _shown) Refresh(n);
    }

    private int NewCount()
    {
        var gm = GameManager.Instance;
        if (gm == null || YG2.saves == null) return 0;
        return Mathf.Max(0, gm.JournalKeys.Count - YG2.saves.journalSeenCount);
    }

    private void Refresh(int n)
    {
        _shown = n;
        if (_badge == null) return;
        bool show = n > 0;
        _badge.SetActive(show);
        if (show)
        {
            if (_text != null) _text.text = n > 9 ? "9+" : n.ToString();
            StartPulse();
        }
        else StopPulse();
    }

    private void BuildBadge()
    {
        _badge = new GameObject("JournalBadge", typeof(RectTransform), typeof(Image));
        _badge.transform.SetParent(transform, false);
        var rt = (RectTransform)_badge.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.sizeDelta = new Vector2(40, 40);
        rt.anchoredPosition = new Vector2(4f, 4f);
        var img = _badge.GetComponent<Image>();
        img.color = new Color(0.9f, 0.2f, 0.2f, 1f);
        img.raycastTarget = false;

        var txtGo = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(_badge.transform, false);
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        _text = txtGo.GetComponent<TextMeshProUGUI>();
        if (_font != null) _text.font = _font;
        _text.fontSize = 24;
        _text.fontStyle = FontStyles.Bold;
        _text.alignment = TextAlignmentOptions.Center;
        _text.color = Color.white;
        _text.raycastTarget = false;

        _badge.SetActive(false);
    }

    private void StartPulse()
    {
        if (_pulseCo != null) return;
        _pulseCo = StartCoroutine(PulseRoutine());
    }

    private void StopPulse()
    {
        if (_pulseCo != null) { StopCoroutine(_pulseCo); _pulseCo = null; }
        transform.localScale = Vector3.one;
    }

    private IEnumerator PulseRoutine()
    {
        float t = 0f;
        while (true)
        {
            t += Time.unscaledDeltaTime * 3f;
            float s = 1f + 0.06f * Mathf.Sin(t);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }
}
