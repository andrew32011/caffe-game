/// <summary>
/// Батч 16: «сундук дня» — крупный праздничный ритуал в конце дня, ПЕРВЫМ по итогам (до экрана
/// результатов). Спрайт сундука (Mini UI) «наполняется» цветом снизу вверх (вертикальная маска
/// заливки), затем вскрывается pop-анимацией и показывает награду. Показывается через UiQueue,
/// поэтому не наслаивается на другие окна. Награду начисляет LootSystem (крупнее и вариативнее).
/// Сцена: MainScene (рантайм). Зависимости: UiKit/UiSkin, UiQueue, AudioController, Loc, TMPro.
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DayChestUI : MonoBehaviour
{
    public static DayChestUI Instance { get; private set; }

    private GameObject _panel, _dim, _chest;
    private Image _chestBase, _chestFill;
    private TextMeshProUGUI _title, _body;
    private bool _open;

    private void Awake() { if (Instance != null && Instance != this) { Destroy(this); return; } Instance = this; }

    public static DayChestUI Ensure()
    {
        if (Instance == null) Instance = new GameObject("DayChest").AddComponent<DayChestUI>();
        return Instance;
    }

    /// <summary>Ставит показ сундука в очередь окон.</summary>
    public void Show(string title, string body, Color color)
    {
        UiQueue.Enqueue(() => ShowRoutine(title, body, color));
    }

    private IEnumerator ShowRoutine(string title, string body, Color color)
    {
        if (_panel == null) Build();
        if (_title != null) _title.text = title;
        if (_body != null) { _body.text = body; _body.gameObject.SetActive(false); }
        if (_chestFill != null) { _chestFill.color = new Color(color.r, color.g, color.b, 1f); _chestFill.fillAmount = 0f; }

        _dim.SetActive(true); _dim.transform.SetAsLastSibling();
        _panel.SetActive(true); _panel.transform.SetAsLastSibling();
        _open = true;
        AudioController.Instance?.PlayUiOpen();

        // Заливка снизу вверх.
        float t = 0f, dur = 1.0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            if (_chestFill != null) _chestFill.fillAmount = Mathf.Clamp01(t / dur);
            yield return null;
        }
        if (_chestFill != null) _chestFill.fillAmount = 1f;

        // Вскрытие: pop-scale.
        if (_chest != null) yield return StartCoroutine(Pop(_chest.transform));
        AudioController.Instance?.PlayBonus();
        if (_body != null) _body.gameObject.SetActive(true);

        // Ждём, пока игрок закроет (кнопка/подложка).
        while (_open) yield return null;
    }

    private IEnumerator Pop(Transform tr)
    {
        float t = 0f, dur = 0.35f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = t / dur;
            float s = 1f + 0.18f * Mathf.Sin(k * Mathf.PI); // 1 → 1.18 → 1
            tr.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        tr.localScale = Vector3.one;
    }

    private void Close()
    {
        AudioController.Instance?.PlayUiClose();
        _open = false;
        if (_panel != null) _panel.SetActive(false);
        if (_dim != null) _dim.SetActive(false);
    }

    private void Build()
    {
        UiKit.Canvas(transform, 330, "DayChestCanvas");
        var canvasT = transform.GetChild(transform.childCount - 1);

        _dim = UiKit.Dim(canvasT, Close);
        _dim.SetActive(false);

        var panel = UiKit.Panel(canvasT, new Vector2(0.30f, 0.20f), new Vector2(0.70f, 0.80f), false, "ChestPanel");
        _panel = panel.gameObject;

        _title = UiKit.Text(_panel.transform, new Vector2(0.06f, 0.86f), new Vector2(0.94f, 0.97f),
            Loc.T("Сундук дня!", "Daily chest!"), 32, TextAlignmentOptions.Center, new Color(1f, 0.9f, 0.45f), FontStyles.Bold, "Title");

        // Контейнер сундука (квадрат по центру).
        _chest = new GameObject("Chest", typeof(RectTransform));
        _chest.transform.SetParent(_panel.transform, false);
        var crt = (RectTransform)_chest.transform;
        crt.anchorMin = new Vector2(0.5f, 0.52f); crt.anchorMax = new Vector2(0.5f, 0.52f);
        crt.sizeDelta = new Vector2(300, 300); crt.anchoredPosition = Vector2.zero;

        var s = UiSkin.Get();
        var chestSpr = s != null ? s.chestSprite : null;

        // Силуэт (тёмная база).
        var baseGo = new GameObject("Base", typeof(RectTransform), typeof(Image));
        baseGo.transform.SetParent(_chest.transform, false);
        UiKit.Fill((RectTransform)baseGo.transform);
        _chestBase = baseGo.GetComponent<Image>();
        if (chestSpr != null) { _chestBase.sprite = chestSpr; _chestBase.preserveAspect = true; }
        _chestBase.color = new Color(0.18f, 0.18f, 0.22f, 1f);
        _chestBase.raycastTarget = false;

        // Заливка снизу вверх (цветная копия сундука).
        var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(_chest.transform, false);
        UiKit.Fill((RectTransform)fillGo.transform);
        _chestFill = fillGo.GetComponent<Image>();
        if (chestSpr != null) _chestFill.sprite = chestSpr; else _chestFill.sprite = UiKit.White();
        _chestFill.color = new Color(0.95f, 0.8f, 0.3f, 1f);
        _chestFill.type = Image.Type.Filled;
        _chestFill.fillMethod = Image.FillMethod.Vertical;
        _chestFill.fillOrigin = (int)Image.OriginVertical.Bottom;
        _chestFill.fillAmount = 0f;
        _chestFill.raycastTarget = false;

        _body = UiKit.Text(_panel.transform, new Vector2(0.06f, 0.18f), new Vector2(0.94f, 0.30f),
            "", 26, TextAlignmentOptions.Center, new Color(0.95f, 0.97f, 1f), FontStyles.Bold, "Body");

        UiKit.Button(_panel.transform, new Vector2(0.28f, 0.04f), new Vector2(0.72f, 0.15f),
            Loc.T("Забрать", "Claim"), Close, true);

        _panel.SetActive(false);
    }
}
