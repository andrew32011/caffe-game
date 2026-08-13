/// <summary>
/// Переиспользуемый промпт вовлечения «вопрос + Да/Нет» (оценка игры, ярлык на рабочий стол).
/// Строит свой Canvas/панель В КОДЕ (как AdRemovalPrompt) — не требует сборки сцены билдером,
/// достаточно обычной сборки кода. Весь текст берётся через локализацию (UiTranslations/Loc),
/// поэтому кнопки переведены на все языки.
///
/// Шрифт подхватывается из любого TextMeshProUGUI в сцене (единый шрифт проекта Nunito),
/// чтобы не зависеть от ручной привязки.
///
/// Сцена: MainScene (создаётся GameManager в рантайме). Зависимости: Loc/UiTranslations, TMPro.
/// SDK: нет (сам по себе; вызовы ReviewShow/Shortcut делают вызывающие через onAccept).
/// </summary>
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class EngagementPrompt : MonoBehaviour
{
    public static EngagementPrompt Instance { get; private set; }

    public bool IsShowing { get; private set; }

    private GameObject _panel;
    private TextMeshProUGUI _titleText;
    private TextMeshProUGUI _acceptLabel;
    private TextMeshProUGUI _declineLabel;
    private TMP_FontAsset _font;
    private Action _onAccept;
    private Coroutine _autoHideCo;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    /// <summary>Гарантирует существование синглтона (ленивое создание из кода).</summary>
    public static EngagementPrompt Ensure()
    {
        if (Instance == null)
        {
            var go = new GameObject("EngagementPrompt");
            Instance = go.AddComponent<EngagementPrompt>();
        }
        return Instance;
    }

    // Локализация «плашками» на все языки (через общую таблицу UiTranslations), с падением
    // на русский ключ. Вызовы передают русские ключи, заведённые в UiTranslations.
    private static string Tr(string ru) =>
        UiTranslations.Has(ru) ? UiTranslations.Get(ru, Loc.Lang) : ru;

    /// <summary>Показывает вопрос с двумя кнопками. Аргументы — РУССКИЕ ключи из UiTranslations;
    /// перевод на язык игрока делается здесь. onAccept вызывается при «Да».</summary>
    public void Show(string titleRu, string acceptRu, string declineRu, Action onAccept)
    {
        if (_panel == null) Build();

        _onAccept = onAccept;
        if (_titleText   != null) _titleText.text   = Tr(titleRu);
        if (_acceptLabel != null) _acceptLabel.text = Tr(acceptRu);
        if (_declineLabel!= null) _declineLabel.text= Tr(declineRu);

        _panel.SetActive(true);
        _panel.transform.SetAsLastSibling();
        IsShowing = true;

        if (_autoHideCo != null) StopCoroutine(_autoHideCo);
        _autoHideCo = StartCoroutine(AutoHide(20f)); // не блокируем поток навсегда
    }

    private void OnAccept()
    {
        AudioController.Instance?.PlayClick();
        var cb = _onAccept;
        Hide();
        cb?.Invoke();
    }

    private void Hide()
    {
        if (_autoHideCo != null) { StopCoroutine(_autoHideCo); _autoHideCo = null; }
        _onAccept = null;
        if (_panel != null) _panel.SetActive(false);
        IsShowing = false;
    }

    private IEnumerator AutoHide(float seconds)
    {
        yield return new WaitForSecondsRealtime(seconds);
        Hide();
    }

    // ─── Построение UI в коде ──────────────────────────────────────────────────

    private void Build()
    {
        // Шрифт проекта — из любого существующего TMP в сцене (единый Nunito).
        var probe = FindObjectOfType<TextMeshProUGUI>();
        _font = probe != null ? probe.font : null;

        var canvasGo = new GameObject("EngagementCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 320; // поверх обычного UI
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        // Затемняющий фон (модальность).
        var dim = new GameObject("Dim", typeof(RectTransform), typeof(Image));
        dim.transform.SetParent(canvasGo.transform, false);
        var drt = (RectTransform)dim.transform;
        drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
        drt.offsetMin = drt.offsetMax = Vector2.zero;
        dim.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

        // Панель по центру.
        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = new Vector2(0.3f, 0.36f);
        prt.anchorMax = new Vector2(0.7f, 0.64f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.11f, 0.10f, 0.17f, 0.98f);

        // Заголовок-вопрос.
        _titleText = MakeText("Title", _panel.transform,
            new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.94f), 30);
        _titleText.color = new Color(1f, 0.96f, 0.85f);

        // Кнопка «Да» (акцент).
        _acceptLabel = MakeButton("AcceptBtn", _panel.transform,
            new Vector2(0.08f, 0.10f), new Vector2(0.49f, 0.34f),
            new Color(0.22f, 0.48f, 0.28f, 1f), OnAccept);

        // Кнопка «Нет/Позже».
        _declineLabel = MakeButton("DeclineBtn", _panel.transform,
            new Vector2(0.51f, 0.10f), new Vector2(0.92f, 0.34f),
            new Color(0.28f, 0.25f, 0.32f, 1f), Hide);

        _panel.SetActive(false);
    }

    private TextMeshProUGUI MakeText(string name, Transform parent, Vector2 aMin, Vector2 aMax, int size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var t = go.GetComponent<TextMeshProUGUI>();
        if (_font != null) t.font = _font;
        t.fontSize = size;
        t.alignment = TextAlignmentOptions.Center;
        t.color = Color.white;
        t.raycastTarget = false;
        t.enableWordWrapping = true;
        t.enableAutoSizing = true;
        t.fontSizeMin = 12; t.fontSizeMax = size;
        return t;
    }

    private TextMeshProUGUI MakeButton(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg, Action onClick)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = aMin; rt.anchorMax = aMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        go.GetComponent<Image>().color = bg;
        go.GetComponent<Button>().onClick.AddListener(() => onClick());

        var label = MakeText("Label", go.transform, Vector2.zero, Vector2.one, 24);
        label.raycastTarget = false;
        return label;
    }
}
