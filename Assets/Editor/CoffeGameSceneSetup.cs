// <summary>
// Автонастройка сцены кофейни «Междумирье».
// Меню: Tools → CoffeGame → Build Scene Systems + UI
//
// Что делает за один клик:
//  1. Создаёт системные объекты (GameManager, DayController, CustomerController,
//     CoffeeCraftingSystem, DialogueDisplayer, TutorialController, HintManager,
//     DayResultUI, VisualEffectsController, YandexManager, SpeechMixer, AudioController).
//  2. Строит Canvas со всеми панелями и кнопками (11 ингредиентов, 6 топпингов,
//     зоны машины, кружка, диалог, подсказки, результат дня, оверлеи эффектов).
//  3. Привязывает все ссылки SerializeField, включая существующие объекты сцены
//     (Stages, ProcessVisitor, VisitorBasis, Main Camera) и ассеты
//     (StoryDatabase, SatisfactionBar.prefab, stickman_1..9, Пер3.ogg).
//
// Повторный запуск удаляет ранее созданные "GAME SYSTEMS" и "GameCanvas" и строит заново.
// </summary>
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;

public static class CoffeGameSceneSetup
{
    const string SystemsRootName = "--- GAME SYSTEMS ---";
    const string CanvasName       = "GameCanvas";

    [MenuItem("Tools/CoffeGame/Build Scene Systems + UI")]
    public static void Build()
    {
        // ── Удаляем прошлый результат (идемпотентность) ─────────────────────
        DestroyIfExists(SystemsRootName);
        DestroyIfExists(CanvasName);

        // ── Существующие объекты сцены ──────────────────────────────────────
        var stages = Object.FindObjectOfType<Stages>();
        var pv     = Object.FindObjectOfType<ProcessVisitor>();
        Transform visitorRoot = pv != null ? pv.targetObject : null;
        if (visitorRoot == null)
        {
            var vb = GameObject.Find("VisitorBasis");
            if (vb != null) visitorRoot = vb.transform;
        }
        Camera mainCam = Camera.main;

        if (stages == null) Debug.LogWarning("CoffeGameSetup: в сцене не найден объект с компонентом Stages (StagesScripts).");
        if (pv == null)     Debug.LogWarning("CoffeGameSetup: в сцене не найден ProcessVisitor (ProcessVisitorManager).");
        if (visitorRoot == null) Debug.LogWarning("CoffeGameSetup: не найден VisitorBasis.");

        // ── Ассеты ──────────────────────────────────────────────────────────
        var storyDB   = LoadFirst<StoryDatabase>("t:StoryDatabase");
        var sbPrefab  = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SatisfactionBar.prefab");
        var sbComp    = sbPrefab != null ? sbPrefab.GetComponent<SatisfactionBar>() : null;
        var murmur    = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Пер3.ogg");
        var stickmen  = LoadStickmen();

        if (storyDB == null)  Debug.LogWarning("CoffeGameSetup: StoryDatabase.asset не найден.");
        if (sbComp == null)   Debug.LogWarning("CoffeGameSetup: SatisfactionBar.prefab не найден.");

        // ── EventSystem ─────────────────────────────────────────────────────
        if (Object.FindObjectOfType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        // ── Корень систем ───────────────────────────────────────────────────
        var root = new GameObject(SystemsRootName);

        var goGame    = Child(root, "GameManager");
        var goDay     = Child(root, "DayController");
        var goCust    = Child(root, "CustomerController");
        var goCraft   = Child(root, "CoffeeCraftingSystem");
        var goDlg     = Child(root, "DialogueSystem");
        var goTut     = Child(root, "TutorialController");
        var goHint    = Child(root, "HintManager");
        var goResult  = Child(root, "DayResultUI");
        var goVfx     = Child(root, "VisualEffectsController");
        var goYandex  = Child(root, "YandexManager");
        var goSpeech  = Child(root, "SpeechMixer");
        var goAudio   = Child(root, "AudioController");

        var gameMgr   = goGame.AddComponent<GameManager>();
        var dayCtrl   = goDay.AddComponent<DayController>();
        var custCtrl  = goCust.AddComponent<CustomerController>();
        var craft     = goCraft.AddComponent<CoffeeCraftingSystem>();
        var dlg        = goDlg.AddComponent<DialogueDisplayer>();
        var tut       = goTut.AddComponent<TutorialController>();
        var hint      = goHint.AddComponent<HintManager>();
        var result    = goResult.AddComponent<DayResultUI>();
        var vfx       = goVfx.AddComponent<VisualEffectsController>();
        goYandex.AddComponent<YandexManager>();
        var speech    = goSpeech.AddComponent<SpeechMixer>();
        var audio     = goAudio.AddComponent<AudioController>();

        // ── Аудио-источники ─────────────────────────────────────────────────
        var music = goAudio.AddComponent<AudioSource>(); music.playOnAwake = false; music.loop = true;
        var sfx   = goAudio.AddComponent<AudioSource>(); sfx.playOnAwake = false;

        // ── SpeechMixer (публичные поля) ────────────────────────────────────
        speech.compressedMurmur = murmur;
        speech.fragments = new List<SpeechFragment>
        {
            new SpeechFragment { name = "f0", startTime = 0.00f, endTime = 0.10f },
            new SpeechFragment { name = "f1", startTime = 0.12f, endTime = 0.24f },
            new SpeechFragment { name = "f2", startTime = 0.26f, endTime = 0.40f },
            new SpeechFragment { name = "f3", startTime = 0.42f, endTime = 0.56f },
            new SpeechFragment { name = "f4", startTime = 0.58f, endTime = 0.72f },
            new SpeechFragment { name = "f5", startTime = 0.74f, endTime = 0.90f },
        };
        EditorUtility.SetDirty(speech);

        // ════════════════════════════════════════════════════════════════════
        //  CANVAS
        // ════════════════════════════════════════════════════════════════════
        var canvasGO = new GameObject(CanvasName, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        var ct = canvasGO.transform;

        // ── Диалоговая панель (низ экрана) ──────────────────────────────────
        var dialoguePanel = Panel("DialoguePanel", ct, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.27f), new Color(0.05f, 0.05f, 0.1f, 0.85f));
        var speakerName   = Text("SpeakerName", dialoguePanel.transform, "Имя", 30, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.7f), new Vector2(0.6f, 0.98f));
        var dialogueText  = Text("DialogueText", dialoguePanel.transform, "Текст реплики...", 28, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.68f));
        var continueHint  = Text("ContinueHint", dialoguePanel.transform, "▼ далее", 22, TextAlignmentOptions.BottomRight, new Vector2(0.6f, 0.0f), new Vector2(0.98f, 0.18f)).gameObject;

        // ── Заставка дня (центр) ────────────────────────────────────────────
        var dayIntroPanel = Panel("DayIntroPanel", ct, new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.6f), new Color(0f, 0f, 0f, 0.8f));
        var dayIntroText  = Text("DayIntroText", dayIntroPanel.transform, "ДЕНЬ 1", 64, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        // ── Сообщение (центр, CanvasGroup) ──────────────────────────────────
        var messagePanel = Panel("MessagePanel", ct, new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.58f), new Color(0.1f, 0f, 0f, 0.8f));
        var messageGroup = messagePanel.AddComponent<CanvasGroup>();
        var messageText  = Text("MessageText", messagePanel.transform, "Сообщение", 34, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        // ── Заказ гостя (верх центр) ────────────────────────────────────────
        var orderText = Text("OrderDisplayText", ct, "Заказ: ...", 30, TextAlignmentOptions.Center, new Vector2(0.3f, 0.9f), new Vector2(0.7f, 0.98f));

        // ── Панель готовки (правый низ) ─────────────────────────────────────
        var craftingPanel = Panel("CraftingPanel", ct, new Vector2(0.6f, 0.3f), new Vector2(0.98f, 0.98f), new Color(0.08f, 0.08f, 0.12f, 0.85f));
        // Кнопки зон (верхний ряд)
        var zoneRow = HRow("ZoneButtons", craftingPanel.transform, new Vector2(0.0f, 0.88f), new Vector2(1f, 1f));
        var btnIngredients = Btn("BtnIngredients", zoneRow, "Ингредиенты");
        var btnMachine     = Btn("BtnMachine", zoneRow, "Машина");
        var btnToppings    = Btn("BtnToppings", zoneRow, "Топпинги");
        var btnServe       = Btn("BtnServe", zoneRow, "Подать");

        // Зона ингредиентов (11 кнопок, порядок enum CoffeeType)
        var ingredientsPanel = Panel("IngredientsPanel", craftingPanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.86f), new Color(0f, 0f, 0f, 0.4f));
        Grid(ingredientsPanel, 3, new Vector2(150, 48));
        string[] ingNames = { "Эспрессо", "Американо", "Капучино", "Латте", "Мокко", "Травяной чай", "Зелёный чай", "Вода", "Горячий шоколад", "Чёрный кофе", "Кофе Правды" };
        var ingButtons = new Button[ingNames.Length];
        for (int i = 0; i < ingNames.Length; i++)
            ingButtons[i] = Btn("Ing_" + i, ingredientsPanel.transform, ingNames[i]);

        // Зона машины
        var machinePanel = Panel("MachinePanel", craftingPanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.86f), new Color(0f, 0f, 0f, 0.4f));
        machinePanel.AddComponent<VerticalLayoutGroup>().spacing = 8;
        var volRow = HRow("VolumeRow", machinePanel.transform, Vector2.zero, Vector2.one);
        var btnSmall  = Btn("BtnSmall", volRow, "Маленький");
        var btnMedium = Btn("BtnMedium", volRow, "Средний");
        var btnLarge  = Btn("BtnLarge", volRow, "Большой");
        var sweetRow = HRow("SweetRow", machinePanel.transform, Vector2.zero, Vector2.one);
        var btnSweetNone = Btn("BtnSweetNone", sweetRow, "Без сахара");
        var btnSweetLow  = Btn("BtnSweetLow", sweetRow, "Слабо");
        var btnSweetMed  = Btn("BtnSweetMed", sweetRow, "Средне");
        var btnSweetHigh = Btn("BtnSweetHigh", sweetRow, "Сладко");
        var btnBrew = Btn("BtnBrew", machinePanel.transform, "ЗАВАРИТЬ");

        // Зона топпингов (6 кнопок, порядок enum Topping)
        var toppingsPanel = Panel("ToppingsPanel", craftingPanel.transform, new Vector2(0.02f, 0.02f), new Vector2(0.98f, 0.86f), new Color(0f, 0f, 0f, 0.4f));
        Grid(toppingsPanel, 3, new Vector2(150, 48));
        string[] topNames = { "Без топпинга", "Сливки", "Корица", "Карамель", "Шоколад", "Мята" };
        var topButtons = new Button[topNames.Length];
        for (int i = 0; i < topNames.Length; i++)
            topButtons[i] = Btn("Top_" + i, toppingsPanel.transform, topNames[i]);

        // ── Кружка (левый низ) ──────────────────────────────────────────────
        var cupUI = Panel("CupUI", ct, new Vector2(0.02f, 0.3f), new Vector2(0.45f, 0.45f), new Color(0.08f, 0.06f, 0.04f, 0.85f));
        var cupStatus = Text("CupStatusText", cupUI.transform, "☕ [тип?] • [объём?]", 26, TextAlignmentOptions.Center, new Vector2(0f, 0.4f), new Vector2(1f, 1f));
        var cupFillGO = new GameObject("CupFillImage", typeof(RectTransform), typeof(Image));
        cupFillGO.transform.SetParent(cupUI.transform, false);
        SetRect((RectTransform)cupFillGO.transform, new Vector2(0.1f, 0.05f), new Vector2(0.9f, 0.35f));
        var cupFill = cupFillGO.GetComponent<Image>();
        cupFill.color = new Color(0.6f, 0.4f, 0.1f);
        cupFill.type = Image.Type.Filled;
        cupFill.fillMethod = Image.FillMethod.Horizontal;
        cupFill.sprite = BuiltinSprite();

        // ── Кнопка «Подсказка» (левый нижний угол, видна всегда) ────────────
        var btnHint = Btn("BtnHint", ct, "Подсказка");
        SetRect(btnHint.GetComponent<RectTransform>(), new Vector2(0.02f, 0.02f), new Vector2(0.12f, 0.08f));

        // ── Панель подсказок (центр, модальная) ─────────────────────────────
        var hintPanel = Panel("HintPanel", ct, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), new Color(0.05f, 0.08f, 0.05f, 0.92f));
        var hintText    = Text("HintText", hintPanel.transform, "Нужна подсказка?", 30, TextAlignmentOptions.Top, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.98f));
        var hintResult  = Text("ResultText", hintPanel.transform, "", 28, TextAlignmentOptions.Center, new Vector2(0.05f, 0.45f), new Vector2(0.95f, 0.75f));
        var coinsDisplay = Text("CoinsDisplay", hintPanel.transform, "У тебя: 0 монет", 22, TextAlignmentOptions.Center, new Vector2(0.05f, 0.35f), new Vector2(0.95f, 0.45f));
        var hintRow = HRow("HintButtons", hintPanel.transform, new Vector2(0.05f, 0.16f), new Vector2(0.95f, 0.32f));
        var btnCoinHint = Btn("BtnCoinHint", hintRow, "За монеты");
        var coinCost    = Text("CoinCost", btnCoinHint.transform, "10 монет", 16, TextAlignmentOptions.Bottom, new Vector2(0f, 0f), new Vector2(1f, 0.4f));
        var btnAdHint   = Btn("BtnAdHint", hintRow, "За рекламу");
        var btnHintClose = Btn("BtnClose", hintPanel.transform, "Закрыть");
        SetRect(btnHintClose.GetComponent<RectTransform>(), new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.14f));

        // ── Экран результатов дня (центр, CanvasGroup) ──────────────────────
        var resultPanel = Panel("DayResultPanel", ct, new Vector2(0.25f, 0.25f), new Vector2(0.75f, 0.75f), new Color(0.05f, 0.05f, 0.12f, 0.95f));
        var resultGroup = resultPanel.AddComponent<CanvasGroup>();
        var resDayNum   = Text("DayNumberText", resultPanel.transform, "ДЕНЬ 1 ЗАВЕРШЁН", 38, TextAlignmentOptions.Top, new Vector2(0.05f, 0.78f), new Vector2(0.95f, 0.98f));
        var resCoins    = Text("CoinsEarnedText", resultPanel.transform, "+0 монет", 30, TextAlignmentOptions.Center, new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.76f));
        var resTotal    = Text("TotalCoinsText", resultPanel.transform, "Всего: 0 монет", 24, TextAlignmentOptions.Center, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.6f));
        var resEnd      = Text("DayEndText", resultPanel.transform, "Итоги дня...", 22, TextAlignmentOptions.Top, new Vector2(0.05f, 0.18f), new Vector2(0.95f, 0.48f));
        var btnContinue = Btn("BtnContinue", resultPanel.transform, "Продолжить");
        SetRect(btnContinue.GetComponent<RectTransform>(), new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.15f));

        // ── Оверлеи эффектов (на весь экран, поверх всего) ──────────────────
        var blackOverlay = Overlay("BlackOverlay", ct, Color.black);
        var redOverlay   = Overlay("RedOverlay", ct, Color.red);
        var whiteOverlay = Overlay("WhiteOverlay", ct, Color.white);

        // Начальные состояния скрытых панелей (скрипты тоже прячут, но без мигания)
        craftingPanel.SetActive(false);
        cupUI.SetActive(false);
        machinePanel.SetActive(false);
        toppingsPanel.SetActive(false);

        // ════════════════════════════════════════════════════════════════════
        //  ПРИВЯЗКА ССЫЛОК
        // ════════════════════════════════════════════════════════════════════

        // DialogueDisplayer (публичные поля → напрямую)
        dlg.dialogBox      = dialoguePanel;
        dlg.dialogTextTMP  = dialogueText;
        dlg.speakerNameText = speakerName;
        dlg.continueHint   = continueHint;
        dlg.dayIntroPanel  = dayIntroPanel;
        dlg.dayIntroText   = dayIntroText;
        dlg.messageText    = messageText;
        dlg.messageGroup   = messageGroup;
        dlg.speechMixer    = speech;
        dlg.manager        = Object.FindObjectOfType<DialogueManager>();
        EditorUtility.SetDirty(dlg);

        // GameManager
        new W(gameMgr)
            .Ref("_storyDatabase", storyDB)
            .Ref("_dayController", dayCtrl)
            .Ref("_dialogue", dlg)
            .Ref("_tutorialController", tut)
            .Ref("_vfxController", vfx)
            .Ref("_dayResultUI", result)
            .Ref("_audioController", audio)
            .Ref("_hintManager", hint)
            .Ref("_stages", stages)
            .Apply();

        // DayController
        new W(dayCtrl)
            .Ref("_stages", stages)
            .Ref("_customerController", custCtrl)
            .Ref("_craftingSystem", craft)
            .Ref("_dialogue", dlg)
            .Ref("_hintManager", hint)
            .Ref("_vfxController", vfx)
            .Arr("_customerPrefabs", stickmen)
            .Apply();

        // CustomerController
        new W(custCtrl)
            .Ref("_processVisitor", pv)
            .Ref("_visitorRoot", visitorRoot)
            .Ref("_satisfactionBarPrefab", sbComp)
            .Apply();

        // CoffeeCraftingSystem
        new W(craft)
            .Ref("_stages", stages)
            .Ref("_craftingPanel", craftingPanel)
            .Ref("_btnIngredients", btnIngredients)
            .Ref("_btnMachine", btnMachine)
            .Ref("_btnToppings", btnToppings)
            .Ref("_btnServe", btnServe)
            .Ref("_ingredientsPanel", ingredientsPanel)
            .Arr("_ingredientButtons", ingButtons)
            .Ref("_machinePanel", machinePanel)
            .Ref("_btnSmall", btnSmall)
            .Ref("_btnMedium", btnMedium)
            .Ref("_btnLarge", btnLarge)
            .Ref("_btnSweetNone", btnSweetNone)
            .Ref("_btnSweetLow", btnSweetLow)
            .Ref("_btnSweetMed", btnSweetMed)
            .Ref("_btnSweetHigh", btnSweetHigh)
            .Ref("_btnBrew", btnBrew)
            .Ref("_toppingsPanel", toppingsPanel)
            .Arr("_toppingButtons", topButtons)
            .Ref("_cupUI", cupUI)
            .Ref("_cupStatusText", cupStatus)
            .Ref("_cupFillImage", cupFill)
            .Ref("_orderDisplayText", orderText)
            .Apply();

        // TutorialController
        new W(tut)
            .Ref("_dialogue", dlg)
            .Ref("_craftingSystem", craft)
            .Ref("_stages", stages)
            .Ref("_ingredientsZoneObject", GameObject.Find("Ingridients"))
            .Ref("_machineZoneObject", GameObject.Find("Cofemachine"))
            .Ref("_toppingsZoneObject", GameObject.Find("CofeBasis"))
            .Ref("_counterObject", GameObject.Find("InternalTable"))
            .Apply();

        // VisualEffectsController
        new W(vfx)
            .Ref("_cameraTransform", mainCam != null ? mainCam.transform : null)
            .Ref("_blackOverlay", blackOverlay)
            .Ref("_redOverlay", redOverlay)
            .Ref("_whiteOverlay", whiteOverlay)
            .Apply();

        // HintManager
        new W(hint)
            .Ref("_hintPanel", hintPanel)
            .Ref("_hintText", hintText)
            .Ref("_btnCoinHint", btnCoinHint)
            .Ref("_btnAdHint", btnAdHint)
            .Ref("_btnClose", btnHintClose)
            .Ref("_coinCostText", coinCost)
            .Ref("_coinsDisplay", coinsDisplay)
            .Ref("_resultText", hintResult)
            .Apply();

        // Кнопка «Подсказка» → HintManager.OpenHintPanel
        AddPersistentClick(btnHint, hint, "OpenHintPanel");

        // DayResultUI
        new W(result)
            .Ref("_resultPanel", resultPanel)
            .Ref("_canvasGroup", resultGroup)
            .Ref("_dayNumberText", resDayNum)
            .Ref("_coinsEarnedText", resCoins)
            .Ref("_totalCoinsText", resTotal)
            .Ref("_dayEndText", resEnd)
            .Ref("_btnContinue", btnContinue)
            .Apply();

        // AudioController
        new W(audio)
            .Ref("_musicSource", music)
            .Ref("_sfxSource", sfx)
            .Apply();

        // ── Готово ──────────────────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("CoffeGameSetup: сцена собрана. Проверьте ссылки на GameManager и сохраните сцену (Ctrl+S).\n" +
                  "Не забудьте: камеры этапов (cameraTarget) в Stages и установленные модули YG2.");
        Selection.activeGameObject = goGame;
    }

    // ════════════════════════════════════════════════════════════════════════
    //  ХЕЛПЕРЫ
    // ════════════════════════════════════════════════════════════════════════

    // Привязка ссылок через SerializedObject (для [SerializeField] private)
    class W
    {
        readonly SerializedObject so;
        public W(Object c) { so = new SerializedObject(c); }
        public W Ref(string field, Object value)
        {
            var p = so.FindProperty(field);
            if (p != null) p.objectReferenceValue = value;
            else Debug.LogWarning($"CoffeGameSetup: поле '{field}' не найдено на {so.targetObject.GetType().Name}");
            return this;
        }
        public W Arr(string field, Object[] values)
        {
            var p = so.FindProperty(field);
            if (p != null)
            {
                p.arraySize = values.Length;
                for (int i = 0; i < values.Length; i++)
                    p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            else Debug.LogWarning($"CoffeGameSetup: массив '{field}' не найден на {so.targetObject.GetType().Name}");
            return this;
        }
        public void Apply() => so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void DestroyIfExists(string name)
    {
        var go = GameObject.Find(name);
        if (go != null) Object.DestroyImmediate(go);
    }

    static GameObject Child(GameObject parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static T LoadFirst<T>(string filter) where T : Object
    {
        var guids = AssetDatabase.FindAssets(filter);
        if (guids.Length == 0) return null;
        return AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static GameObject[] LoadStickmen()
    {
        const string dir = "Assets/PrefsAll/Hyper Casual Characters/Prefab/";
        var names = new[]
        {
            "stickman_1 1", "stickman_2", "stickman_3", "stickman_4", "stickman_5",
            "stickman_6", "stickman_7", "stickman_8", "stickman_9"
        };
        var list = new List<GameObject>();
        foreach (var n in names)
        {
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(dir + n + ".prefab");
            if (go != null) list.Add(go);
        }
        if (list.Count == 0)
            Debug.LogWarning("CoffeGameSetup: префабы stickman не найдены в " + dir);
        return list.ToArray();
    }

    // Панель с фоном, заякоренная по долям экрана
    static GameObject Panel(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color bg)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, aMin, aMax);
        go.GetComponent<Image>().color = bg;
        return go;
    }

    static TextMeshProUGUI Text(string name, Transform parent, string content, int size, TextAlignmentOptions align)
        => Text(name, parent, content, size, align, Vector2.zero, Vector2.one);

    static TextMeshProUGUI Text(string name, Transform parent, string content, int size, TextAlignmentOptions align, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var t = go.AddComponent<TextMeshProUGUI>();
        t.text = content;
        t.fontSize = size;
        t.alignment = align;
        t.color = Color.white;
        t.enableWordWrapping = true;
        SetRect(t.rectTransform, aMin, aMax);
        return t;
    }

    static Button Btn(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = new Color(0.2f, 0.2f, 0.28f, 0.95f);
        ((RectTransform)go.transform).sizeDelta = new Vector2(150, 48);
        var txt = Text("Label", go.transform, label, 20, TextAlignmentOptions.Center);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        return btn;
    }

    static Image Overlay(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, Vector2.zero, Vector2.one);
        var img = go.GetComponent<Image>();
        img.color = new Color(color.r, color.g, color.b, 0f);
        img.raycastTarget = false;
        go.SetActive(false);
        return img;
    }

    // Контейнер с горизонтальной раскладкой
    static Transform HRow(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, aMin, aMax);
        var h = go.AddComponent<HorizontalLayoutGroup>();
        h.spacing = 6;
        h.childForceExpandWidth = true;
        h.childForceExpandHeight = true;
        h.childControlWidth = true;
        h.childControlHeight = true;
        return go.transform;
    }

    static void Grid(GameObject panel, int columns, Vector2 cell)
    {
        var g = panel.AddComponent<GridLayoutGroup>();
        g.cellSize = cell;
        g.spacing = new Vector2(8, 8);
        g.padding = new RectOffset(8, 8, 8, 8);
        g.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        g.constraintCount = columns;
    }

    static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax)
    {
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.pivot = new Vector2(0.5f, 0.5f);
    }

    static Sprite BuiltinSprite()
    {
        // Встроенный UISprite Unity (белый закруглённый квадрат)
        return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
    }

    static void AddPersistentClick(Button btn, Object target, string method)
    {
        var action = System.Delegate.CreateDelegate(typeof(UnityEngine.Events.UnityAction), target, method, false, false)
                     as UnityEngine.Events.UnityAction;
        if (action != null)
            UnityEditor.Events.UnityEventTools.AddPersistentListener(btn.onClick, action);
    }
}
#endif
