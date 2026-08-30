/// <summary>
/// Батч 11: HUD «Часа пик» — компактная плашка сверху с надписью «Час пик», счётчиком
/// очереди («Очередь: N») и убывающей шкалой темпа на текущего гостя. Строится В КОДЕ
/// (как AdRemovalPrompt/EngagementPrompt), поэтому не требует ссылок из билдера; если
/// компонент не создан билдером — механика темпа всё равно работает (DayController
/// проверяет Instance на null), просто без визуала.
///
/// Таймер идёт по МАСШТАБИРОВАННОМУ времени (Time.time/deltaTime), поэтому пауза настроек
/// (Time.timeScale = 0) честно замораживает и шкалу, и подсчёт бонуса в DayController.
///
/// Сцена: MainScene (singleton). Зависимости: Loc, TMPro, UnityEngine.UI. SDK: нет.
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RushHudUI : MonoBehaviour
{
    public static RushHudUI Instance { get; private set; }

    [SerializeField] private TMP_FontAsset _font;

    private GameObject _panel;
    private TextMeshProUGUI _label;
    private Image _barFill;
    private Coroutine _timerCo;
    private int _queue;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    // ─── Публичное API (вызывает DayController) ───────────────────────────────

    /// <summary>Включает плашку часа пик на весь день. queueLeft — сколько гостей впереди.</summary>
    public void BeginRush(int queueLeft)
    {
        if (_panel == null) Build();
        _queue = Mathf.Max(0, queueLeft);
        _panel.SetActive(true);
        if (_barFill != null) _barFill.fillAmount = 1f;
        UpdateLabel();
    }

    /// <summary>Запускает шкалу темпа для текущего гостя (длительность — RushController.RushSeconds).</summary>
    public void StartTimer(float seconds)
    {
        if (_panel == null || !_panel.activeSelf) return;
        if (_timerCo != null) StopCoroutine(_timerCo);
        _timerCo = StartCoroutine(TimerRoutine(Mathf.Max(0.1f, seconds)));
    }

    /// <summary>Останавливает шкалу (гость обслужен).</summary>
    public void StopTimer()
    {
        if (_timerCo != null) { StopCoroutine(_timerCo); _timerCo = null; }
        if (_barFill != null) _barFill.fillAmount = 0f;
    }

    /// <summary>Обновляет число гостей в очереди.</summary>
    public void SetQueue(int queueLeft)
    {
        _queue = Mathf.Max(0, queueLeft);
        UpdateLabel();
    }

    /// <summary>Выключает плашку (день закончился / день не час-пиковый).</summary>
    public void EndRush()
    {
        StopTimer();
        if (_panel != null) _panel.SetActive(false);
    }

    // ─── Внутреннее ───────────────────────────────────────────────────────────

    private IEnumerator TimerRoutine(float seconds)
    {
        float t = 0f;
        while (t < seconds)
        {
            t += Time.deltaTime; // масштабируемое время — пауза замораживает шкалу
            if (_barFill != null) _barFill.fillAmount = Mathf.Clamp01(1f - t / seconds);
            // Цвет от зелёного к красному по мере утекания темпа.
            if (_barFill != null)
                _barFill.color = Color.Lerp(new Color(0.9f, 0.35f, 0.3f), new Color(0.4f, 0.85f, 0.45f),
                                            _barFill.fillAmount);
            yield return null;
        }
        if (_barFill != null) _barFill.fillAmount = 0f;
        _timerCo = null;
    }

    private void UpdateLabel()
    {
        if (_label == null) return;
        _label.text = _queue > 0
            ? Loc.T($"Час пик · очередь: {_queue}", $"Rush hour · queue: {_queue}")
            : Loc.T("Час пик", "Rush hour");
    }

    private void Build()
    {
        var canvasGo = new GameObject("RushHudCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.transform.SetParent(transform, false);
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;

        _panel = new GameObject("Panel", typeof(RectTransform), typeof(Image));
        _panel.transform.SetParent(canvasGo.transform, false);
        var prt = (RectTransform)_panel.transform;
        prt.anchorMin = new Vector2(0.35f, 0.855f);
        prt.anchorMax = new Vector2(0.65f, 0.925f);
        prt.offsetMin = prt.offsetMax = Vector2.zero;
        _panel.GetComponent<Image>().color = new Color(0.10f, 0.09f, 0.16f, 0.92f);

        // Надпись «Час пик · очередь: N».
        var txtGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtGo.transform.SetParent(_panel.transform, false);
        var trt = (RectTransform)txtGo.transform;
        trt.anchorMin = new Vector2(0.04f, 0.42f);
        trt.anchorMax = new Vector2(0.96f, 0.97f);
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        _label = txtGo.GetComponent<TextMeshProUGUI>();
        if (_font == null) _font = TMP_Settings.defaultFontAsset;
        if (_font != null) _label.font = _font;
        _label.text = Loc.T("Час пик", "Rush hour");
        _label.fontSize = 26;
        _label.alignment = TextAlignmentOptions.Center;
        _label.color = new Color(1f, 0.9f, 0.6f);
        _label.raycastTarget = false;
        _label.enableAutoSizing = true;
        _label.fontSizeMin = 12; _label.fontSizeMax = 28;

        // Фон шкалы темпа.
        var barBg = new GameObject("BarBG", typeof(RectTransform), typeof(Image));
        barBg.transform.SetParent(_panel.transform, false);
        var brt = (RectTransform)barBg.transform;
        brt.anchorMin = new Vector2(0.06f, 0.10f);
        brt.anchorMax = new Vector2(0.94f, 0.36f);
        brt.offsetMin = brt.offsetMax = Vector2.zero;
        barBg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);
        barBg.GetComponent<Image>().raycastTarget = false;

        // Заполнение шкалы (Filled Horizontal).
        var fillGo = new GameObject("BarFill", typeof(RectTransform), typeof(Image));
        fillGo.transform.SetParent(barBg.transform, false);
        var frt = (RectTransform)fillGo.transform;
        frt.anchorMin = Vector2.zero; frt.anchorMax = Vector2.one;
        frt.offsetMin = frt.offsetMax = Vector2.zero;
        _barFill = fillGo.GetComponent<Image>();
        _barFill.color = new Color(0.4f, 0.85f, 0.45f);
        _barFill.sprite = WhiteSprite(); // Filled-режиму нужен спрайт, иначе не рисуется
        _barFill.type = Image.Type.Filled;
        _barFill.fillMethod = Image.FillMethod.Horizontal;
        _barFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        _barFill.fillAmount = 1f;
        _barFill.raycastTarget = false;

        _panel.SetActive(false);
    }

    // Плоский белый спрайт для Filled-шкалы (в рантайме нет доступа к спрайтам билдера).
    private static Sprite _white;
    private static Sprite WhiteSprite()
    {
        if (_white != null) return _white;
        var tex = new Texture2D(4, 4);
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px);
        tex.Apply();
        _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        return _white;
    }
}
