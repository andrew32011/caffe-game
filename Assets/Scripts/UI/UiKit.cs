/// <summary>
/// Батч 16: статические билдеры UI из ассетов Mini UI (через UiSkin). Все код-построенные окна
/// используют эти помощники, чтобы панели/кнопки были в рамках Mini UI, а не плоскими
/// прямоугольниками. Если UiSkin отсутствует — мягкий откат на плоский стиль (ничего не падает).
/// Сцена: MainScene (рантайм). Зависимости: UiSkin, TMPro, UnityEngine.UI. SDK: нет.
/// </summary>
using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class UiKit
{
    // ─── Шрифт ────────────────────────────────────────────────────────────────
    public static TMP_FontAsset Font
    {
        get
        {
            var s = UiSkin.Get();
            if (s != null && s.font != null) return s.font;
            var probe = UnityEngine.Object.FindObjectOfType<TextMeshProUGUI>();
            return probe != null ? probe.font : null;
        }
    }

    // ─── Overlay-Canvas ─────────────────────────────────────────────────────────
    public static Canvas Canvas(Transform parent, int sortingOrder, string name = "Canvas")
    {
        var go = new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        if (parent != null) go.transform.SetParent(parent, false);
        var canvas = go.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        var scaler = go.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        return canvas;
    }

    // ─── Затемняющая подложка (клик закрывает) ─────────────────────────────────
    public static GameObject Dim(Transform parent, Action onClick)
    {
        var go = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Fill((RectTransform)go.transform);
        go.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);
        if (onClick != null) go.GetComponent<Button>().onClick.AddListener(() => onClick());
        return go;
    }

    // ─── Панель (рамка Mini UI) ────────────────────────────────────────────────
    public static Image Panel(Transform parent, Vector2 aMin, Vector2 aMax, bool accent = false, string name = "Panel")
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Anchor((RectTransform)go.transform, aMin, aMax);
        var img = go.GetComponent<Image>();
        var s = UiSkin.Get();
        var spr = s != null ? (accent ? s.panelAccentSprite : s.panelSprite) : null;
        if (spr != null)
        {
            img.sprite = spr; img.type = Image.Type.Sliced; img.color = Color.white;
            img.pixelsPerUnitMultiplier = 8f;
        }
        else img.color = accent ? new Color(1f, 1f, 1f, 0.06f) : new Color(0.07f, 0.06f, 0.12f, 0.97f);
        return img;
    }

    // ─── Кнопка (спрайт Mini UI + TMP-лейбл) ───────────────────────────────────
    public static Button Button(Transform parent, Vector2 aMin, Vector2 aMax, string text, Action onClick,
        bool accent = false, int fontSize = 24, string name = "Button")
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        Anchor((RectTransform)go.transform, aMin, aMax);
        var img = go.GetComponent<Image>();
        var s = UiSkin.Get();
        var spr = s != null ? (accent ? s.buttonAccentSprite : s.buttonSprite) : null;
        if (spr == null && s != null) spr = s.buttonSprite;
        if (spr != null)
        {
            img.sprite = spr; img.type = Image.Type.Sliced; img.color = Color.white;
            img.pixelsPerUnitMultiplier = 8f;
        }
        else img.color = accent ? new Color(0.22f, 0.45f, 0.28f, 1f) : new Color(0.22f, 0.3f, 0.45f, 1f);
        var btn = go.GetComponent<Button>();
        btn.transition = Selectable.Transition.ColorTint;
        if (onClick != null) btn.onClick.AddListener(() => onClick());
        var label = Text(go.transform, Vector2.zero, Vector2.one, text, fontSize, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        return btn;
    }

    /// <summary>Возвращает TMP-лейбл кнопки (первый TextMeshProUGUI в детях).</summary>
    public static TextMeshProUGUI Label(Button b) => b != null ? b.GetComponentInChildren<TextMeshProUGUI>() : null;

    // ─── Текст ──────────────────────────────────────────────────────────────────
    public static TextMeshProUGUI Text(Transform parent, Vector2 aMin, Vector2 aMax, string content, int size,
        TextAlignmentOptions align, Color color, FontStyles style = FontStyles.Normal, string name = "Text")
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        Anchor((RectTransform)go.transform, aMin, aMax);
        var t = go.GetComponent<TextMeshProUGUI>();
        var f = Font; if (f != null) t.font = f;
        t.text = content; t.fontSize = size; t.alignment = align; t.color = color; t.fontStyle = style;
        t.raycastTarget = false; t.enableWordWrapping = true;
        t.enableAutoSizing = true; t.fontSizeMin = 10; t.fontSizeMax = size;
        return t;
    }

    // ─── Иконка ──────────────────────────────────────────────────────────────────
    public static Image Icon(Transform parent, Vector2 aMin, Vector2 aMax, Sprite sprite, string name = "Icon")
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Anchor((RectTransform)go.transform, aMin, aMax);
        var img = go.GetComponent<Image>();
        if (sprite != null) img.sprite = sprite; else img.color = new Color(1f, 1f, 1f, 0.2f);
        img.preserveAspect = true; img.raycastTarget = false;
        return img;
    }

    // ─── Значок «!» (индикатор) ───────────────────────────────────────────────
    public static Image Badge(Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var s = UiSkin.Get();
        var go = new GameObject("Badge", typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Anchor((RectTransform)go.transform, aMin, aMax);
        var img = go.GetComponent<Image>();
        if (s != null && s.badgeSprite != null) { img.sprite = s.badgeSprite; img.preserveAspect = true; }
        else img.color = new Color(0.95f, 0.25f, 0.25f, 1f);
        img.raycastTarget = false;
        return img;
    }

    // ─── Заливная шкала (для сундука/прогресса) ────────────────────────────────
    public static Image Fill(Transform parent, Vector2 aMin, Vector2 aMax, Color color,
        Image.FillMethod method = Image.FillMethod.Horizontal, int origin = 0, string name = "Fill")
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Anchor((RectTransform)go.transform, aMin, aMax);
        var img = go.GetComponent<Image>();
        img.sprite = White(); img.color = color; img.type = Image.Type.Filled;
        img.fillMethod = method; img.fillOrigin = origin; img.fillAmount = 0f; img.raycastTarget = false;
        return img;
    }

    // ─── Rect-утилиты ──────────────────────────────────────────────────────────
    public static void Anchor(RectTransform rt, Vector2 aMin, Vector2 aMax)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
    public static void Fill(RectTransform rt) => Anchor(rt, Vector2.zero, Vector2.one);

    // ─── Белый спрайт (для заливок, если в скине нет) ──────────────────────────
    private static Sprite _white;
    public static Sprite White()
    {
        var s = UiSkin.Get();
        if (s != null && s.whiteSprite != null) return s.whiteSprite;
        if (_white != null) return _white;
        var tex = new Texture2D(4, 4);
        var px = new Color32[16];
        for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
        tex.SetPixels32(px); tex.Apply();
        _white = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
        return _white;
    }
}
