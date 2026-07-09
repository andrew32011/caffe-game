// <summary>
// Собирает полноэкранный UI вступления в первой сцене (SampleScene): затемнённая
// панель на весь экран, текст истории Миры и кнопка «Продолжить» (серая → через 2с
// активная). Логику ведёт IntroStoryUI; камера вызывает его на финальной точке.
//
// Меню: Tools → CoffeGame → Build Intro (SampleScene)
// Открой SampleScene и нажми пункт меню; повторный запуск пересобирает UI.
// </summary>
#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class IntroSceneSetup
{
    const string CanvasName = "IntroCanvas";

    [MenuItem("Tools/CoffeGame/Build Intro (SampleScene)")]
    public static void Build()
    {
        var existing = GameObject.Find(CanvasName);
        if (existing != null) Object.DestroyImmediate(existing);

        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ── Canvas (адаптивный масштаб — требования 1.6/1.10) ──────────────────
        var canvasGO = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var ct = canvasGO.transform;

        // ── Полноэкранная панель с CanvasGroup ─────────────────────────────────
        var panel = Panel("IntroPanel", ct, Vector2.zero, Vector2.one, new Color(0.03f, 0.03f, 0.06f, 1f));
        ApplyPanelSprite(panel);
        var group = panel.AddComponent<CanvasGroup>();

        // ── Текст истории (на весь центр, по центру) ────────────────────────────
        var story = Text("IntroStoryText", panel.transform, "…", 40, TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.2f), new Vector2(0.9f, 0.86f));

        // ── Заголовок (имя кофейни) сверху ──────────────────────────────────────
        Text("IntroTitle", panel.transform, "Междумирье", 64, TextAlignmentOptions.Center,
            new Vector2(0.1f, 0.86f), new Vector2(0.9f, 0.96f));

        // ── Кнопка «Продолжить» (низ по центру) ────────────────────────────────
        var btn = Btn("BtnContinueIntro", panel.transform, "Продолжить");
        SetRect(btn.GetComponent<RectTransform>(), new Vector2(0.38f, 0.06f), new Vector2(0.62f, 0.15f));
        btn.transition = Selectable.Transition.None; // цвет ведём вручную (серая/активная)
        var label = btn.GetComponentInChildren<TextMeshProUGUI>();

        // ── Логика ─────────────────────────────────────────────────────────────
        var intro = canvasGO.AddComponent<IntroStoryUI>();
        var so = new SerializedObject(intro);
        SetRef(so, "_group", group);
        SetRef(so, "_storyText", story);
        SetRef(so, "_continueButton", btn);
        SetRef(so, "_continueLabel", label);
        so.ApplyModifiedPropertiesWithoutUndo();

        // ВСЕ кнопки/панели ВСЕЙ сцены интро → pixelsPerUnitMultiplier = 4
        // (одинаковая толщина рамок у всех кнопок и панелей).
        foreach (var img in Object.FindObjectsOfType<Image>(true))
            if (img != null && img.type == Image.Type.Sliced)
                img.pixelsPerUnitMultiplier = 8f;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        var cam = Object.FindObjectOfType<SmoothCameraWaypointController>();
        Debug.Log("IntroSceneSetup: вступление собрано в текущей сцене." +
                  (cam == null ? " ВНИМАНИЕ: SmoothCameraWaypointController не найден — открой SampleScene." :
                   " Камера найдена — на финальной точке покажется история. Сохрани сцену (Ctrl+S)."));
        Selection.activeGameObject = canvasGO;
    }

    // ── Хелперы (мини-копия из CoffeGameSceneSetup) ────────────────────────────

    static TMP_FontAsset _font;
    static TMP_FontAsset Font()
    {
        if (_font == null)
            _font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ofont.ru_Nunito SDF.asset");
        return _font;
    }

    static Sprite _panelSprite;
    static void ApplyPanelSprite(GameObject panel)
    {
        if (_panelSprite == null)
            _panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Mini UI/9 Splice Panels/Dark Theme RoundEdge Panels/Dark Theme RoundEdge DARK.png");
        var img = panel.GetComponent<Image>();
        if (_panelSprite != null && img != null)
        {
            img.sprite = _panelSprite; img.type = Image.Type.Sliced; img.color = Color.white; img.pixelsPerUnitMultiplier = 8f;
        }
    }

    static Sprite _buttonSprite;
    static void ApplyButtonSprite(Image img)
    {
        if (img == null) return;
        if (_buttonSprite == null)
            _buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Mini UI/Buttons/Dark Theme Border Buttons/192Px Round DarkBorder/Small Round Button DARK.png");
        if (_buttonSprite != null)
        {
            img.sprite = _buttonSprite; img.type = Image.Type.Sliced; img.color = Color.white; img.pixelsPerUnitMultiplier = 8f;
        }
        else img.color = new Color(0.2f, 0.2f, 0.28f, 0.95f);
    }

    static GameObject Panel(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, aMin, aMax);
        go.GetComponent<Image>().color = bg;
        return go;
    }

    static TextMeshProUGUI Text(string name, Transform parent, string content, int size, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        var f = Font(); if (f != null) t.font = f;
        t.fontSize = size; t.fontSizeMax = size; t.fontSizeMin = Mathf.Max(10f, size * 0.5f);
        t.enableAutoSizing = true;
        t.alignment = align; t.color = Color.white; t.enableWordWrapping = true;
        t.margin = new Vector4(16, 10, 16, 10);
        SetRect(t.rectTransform, aMin, aMax);
        return t;
    }

    static Button Btn(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        ApplyButtonSprite(img);
        ((RectTransform)go.transform).sizeDelta = new Vector2(220, 64);
        Text("Label", go.transform, label, 24, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax)
    {
        rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero; rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = value;
    }
}
#endif
