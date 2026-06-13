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
        // Чистим объекты прошлого дизайна (кубы-станции)
        DestroyIfExists("Items_Ingredients");
        DestroyIfExists("Items_Machine");
        DestroyIfExists("Items_Toppings");

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
        var machine   = goCraft.AddComponent<MachineMinigame>();
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

        // ── Ачивка («Отлично!»/«В точку!») сверху ───────────────────────────
        var achievement = Text("AchievementText", ct, "В точку!", 48, TextAlignmentOptions.Center, new Vector2(0.25f, 0.78f), new Vector2(0.75f, 0.9f));
        achievement.color = new Color(0.4f, 1f, 0.5f);
        achievement.gameObject.SetActive(false);

        // ── Кнопка «Подать» (низ по центру; пункт 10) ───────────────────────
        var serveBtn = Btn("BtnServe", ct, "Подать ☕");
        SetRect(serveBtn.GetComponent<RectTransform>(), new Vector2(0.4f, 0.04f), new Vector2(0.6f, 0.12f));
        serveBtn.gameObject.SetActive(false);

        // ── Панель машины: 2 вертикальных заполнения (температура, объём) ────
        var machinePanel = Panel("MachinePanel", ct, new Vector2(0.35f, 0.25f), new Vector2(0.65f, 0.75f), new Color(0.06f, 0.06f, 0.1f, 0.9f));
        var tempFill   = VerticalBar("TempBar",   machinePanel.transform, new Vector2(0.12f, 0.18f), new Vector2(0.42f, 0.82f), new Color(0.9f, 0.4f, 0.2f));
        var tempLabel  = Text("TempLabel", machinePanel.transform, "Температура", 22, TextAlignmentOptions.Center, new Vector2(0.0f, 0.02f), new Vector2(0.5f, 0.16f));
        var volFill    = VerticalBar("VolumeBar", machinePanel.transform, new Vector2(0.58f, 0.18f), new Vector2(0.88f, 0.82f), new Color(0.2f, 0.5f, 0.9f));
        var volLabel   = Text("VolumeLabel", machinePanel.transform, "Объём", 22, TextAlignmentOptions.Center, new Vector2(0.5f, 0.02f), new Vector2(1.0f, 0.16f));
        machinePanel.SetActive(false);

        // ── Полоса удовлетворённости сверху (пункт 4) ───────────────────────
        var satBarBG = Panel("SatisfactionBG", ct, new Vector2(0.3f, 0.93f), new Vector2(0.7f, 0.97f), new Color(0f, 0f, 0f, 0.5f));
        var satFillGO = new GameObject("SatisfactionFill", typeof(RectTransform), typeof(Image));
        satFillGO.transform.SetParent(satBarBG.transform, false);
        SetRect((RectTransform)satFillGO.transform, Vector2.zero, Vector2.one);
        var satFill = satFillGO.GetComponent<Image>();
        satFill.color = new Color(0.3f, 0.85f, 0.4f);
        satFill.sprite = WhiteSprite();
        satFill.type = Image.Type.Filled;
        satFill.fillMethod = Image.FillMethod.Horizontal;
        satFill.fillAmount = 0.5f;
        satFill.raycastTarget = false;

        // Комментарий после шага («Супер!/Не то»)
        var commentText = Text("CommentText", ct, "", 34, TextAlignmentOptions.Center, new Vector2(0.3f, 0.86f), new Vector2(0.7f, 0.92f));
        commentText.color = new Color(1f, 0.95f, 0.6f);
        commentText.gameObject.SetActive(false);

        // ── Деньги кофейни (сверху слева, пункт 5) ──────────────────────────
        var coinsText = Text("CoinsText", ct, "Касса: 0", 28, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.9f), new Vector2(0.25f, 0.98f));
        coinsText.color = new Color(1f, 0.9f, 0.4f);
        var coinsUI = coinsText.gameObject.AddComponent<CoinsUI>();
        new W(coinsUI).Ref("_text", coinsText).Apply();

        // ── Выбор ингредиента + кнопка «Подтвердить» (пункт 2) ──────────────
        var selectedText = Text("SelectedText", ct, "Выбрано: …", 28, TextAlignmentOptions.Center, new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.24f));
        selectedText.gameObject.SetActive(false);
        var confirmBtn = Btn("BtnConfirm", ct, "Подтвердить ✓");
        SetRect(confirmBtn.GetComponent<RectTransform>(), new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.13f));
        confirmBtn.gameObject.SetActive(false);

        // ── КЛИКАБЕЛЬНЫЕ ПРЕДМЕТЫ НА РЕАЛЬНЫХ ОБЪЕКТАХ СЦЕНЫ ────────────────
        //  Ингредиенты — дети Ingridients1; топпинги — дети ShelfItems.
        //  Вешаем на каждый IngredientItem + Collider (без замены визуала).
        int ingCount = MakeIngredientItems("Ingridients1");
        MakeToppingItems("ShelfItems");

        // ── Кружка: используем существующий PlayerCup + якоря зон ───────────
        CupController cupController = SetupCup("PlayerCup",
            FindMarker("Ingridients"), FindMarker("Cofemachine", "CofeMashine"),
            FindMarker("CofeBasis"),   FindMarker("PointCashier", "PointCashierForDialog"));

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

        // CustomerController — переиспользуем существующего ходячего гостя.
        // Анимацию/размер с него снимет и исходную модель удалит сам CustomerController (Awake).
        var existingGuest = FindSceneStickman();
        new W(custCtrl)
            .Ref("_processVisitor", pv)
            .Ref("_visitorRoot", visitorRoot)
            .Ref("_satisfactionBarPrefab", sbComp)
            .Ref("_existingGuest", existingGuest)
            .Apply();

        // MachineMinigame — UI вертикальных шкал
        new W(machine)
            .Ref("_panel", machinePanel)
            .Ref("_tempFill", tempFill)
            .Ref("_tempLabel", tempLabel)
            .Ref("_volumeFill", volFill)
            .Ref("_volumeLabel", volLabel)
            .Apply();

        // CoffeeCraftingSystem (предметы + кружка + минигейм + подача + UI)
        new W(craft)
            .Ref("_stages", stages)
            .Ref("_cup", cupController)
            .Ref("_machine", machine)
            .Ref("_orderDisplayText", orderText)
            .Ref("_selectedText", selectedText)
            .Ref("_confirmButton", confirmBtn)
            .Ref("_serveButton", serveBtn)
            .Ref("_achievementText", achievement)
            .Ref("_satisfactionFill", satFill)
            .Ref("_commentText", commentText)
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

    // ── 3D-предметы и станции ───────────────────────────────────────────────

    static Transform FindMarker(params string[] names)
    {
        foreach (var n in names)
        {
            var go = GameObject.Find(n);
            if (go != null) return go.transform;
        }
        return null;
    }

    // Навешивает IngredientItem (kind=Ingredient) на детей объекта-родителя (Ingridients1).
    // Возвращает количество ингредиентов.
    static int MakeIngredientItems(string parentName)
    {
        var parent = GameObject.Find(parentName);
        if (parent == null) { Debug.LogWarning($"CoffeGameSetup: не найден {parentName} (ингредиенты)."); return 0; }

        int i = 0;
        foreach (Transform child in parent.transform)
        {
            EnsureCollider(child.gameObject);
            var it = child.GetComponent<IngredientItem>();
            if (it == null) it = child.gameObject.AddComponent<IngredientItem>();
            it.kind = IngredientItem.ItemKind.Ingredient;
            it.ingredientIndex = i;
            AddWorldLabel(child.gameObject, Loc.T("Основа ", "Base ") + (i + 1)); // подпись над предметом (пункт 2)
            i++;
        }
        Debug.Log($"CoffeGameSetup: ингредиентов (дети {parentName}): {i}");
        return i;
    }

    // Навешивает IngredientItem (kind=Topping) на детей ShelfItems. Топпинг по порядку enum.
    static void MakeToppingItems(string parentName)
    {
        var parent = GameObject.Find(parentName);
        if (parent == null) { Debug.LogWarning($"CoffeGameSetup: не найден {parentName} (топпинги)."); return; }

        var tops = (Topping[])System.Enum.GetValues(typeof(Topping));
        int i = 0;
        foreach (Transform child in parent.transform)
        {
            EnsureCollider(child.gameObject);
            var it = child.GetComponent<IngredientItem>();
            if (it == null) it = child.gameObject.AddComponent<IngredientItem>();
            it.kind = IngredientItem.ItemKind.Topping;
            // Пропускаем Topping.None (0) — реальным предметам даём Cream, Cinnamon, ...
            it.topping = tops[Mathf.Min(i + 1, tops.Length - 1)];
            i++;
        }
        Debug.Log($"CoffeGameSetup: топпингов (дети {parentName}): {i}");
    }

    // Подпись (TextMesh) над 3D-объектом
    static void AddWorldLabel(GameObject go, string text)
    {
        if (go.transform.Find("ItemLabel") != null) return; // не дублируем
        var rend = go.GetComponentInChildren<Renderer>();
        float top = rend != null ? rend.bounds.size.y : 1f;

        var lbl = new GameObject("ItemLabel");
        lbl.transform.SetParent(go.transform, false);
        lbl.transform.localPosition = new Vector3(0f, top * 0.6f + 0.4f, 0f);
        lbl.transform.localScale = Vector3.one * 0.2f;
        var tm = lbl.AddComponent<TextMesh>();
        tm.text = text;
        tm.fontSize = 48;
        tm.characterSize = 0.08f;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            var mr = lbl.GetComponent<MeshRenderer>();
            if (mr != null) mr.sharedMaterial = font.material;
        }
    }

    static void EnsureCollider(GameObject go)
    {
        if (go.GetComponentInChildren<Collider>() != null) return;
        var rend = go.GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            var bc = go.AddComponent<BoxCollider>();
            // Подгоняем коллайдер под видимые границы (приблизительно)
            var b = rend.bounds;
            bc.center = go.transform.InverseTransformPoint(b.center);
            bc.size = go.transform.InverseTransformVector(b.size);
        }
        else
        {
            go.AddComponent<BoxCollider>();
        }
    }

    // Настраивает существующую кружку (PlayerCup): CupController + якоря зон.
    static CupController SetupCup(string cupName, Transform ingA, Transform macA, Transform topA, Transform countA)
    {
        var cup = GameObject.Find(cupName);
        if (cup == null)
        {
            cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cup.name = cupName;
            Object.DestroyImmediate(cup.GetComponent<Collider>());
            cup.transform.localScale = new Vector3(0.12f, 0.1f, 0.12f);
            Debug.LogWarning("CoffeGameSetup: PlayerCup не найден — создан цилиндр-заглушка.");
        }
        cup.transform.SetParent(null, true); // кружка живёт в мире, не под камерой

        Transform Anchor(string n, Transform near, Vector3 fallback)
        {
            var a = new GameObject(n).transform;
            a.position = near != null ? near.position : fallback;
            if (near != null) a.rotation = near.rotation;
            return a;
        }
        var aIng   = Anchor("CupAnchor_Ingredients", ingA,   cup.transform.position);
        var aMac   = Anchor("CupAnchor_Machine",     macA,   cup.transform.position);
        var aTop   = Anchor("CupAnchor_Toppings",    topA,   cup.transform.position);
        var aCount = Anchor("CupAnchor_Counter",     countA, cup.transform.position);

        var content = new GameObject("ContentAnchor").transform;
        content.SetParent(cup.transform, false);
        content.localPosition = new Vector3(0f, 0.6f, 0f);

        var ctrl = cup.GetComponent<CupController>();
        if (ctrl == null) ctrl = cup.AddComponent<CupController>();

        var so = new SerializedObject(ctrl);
        SetRef(so, "_ingredientsAnchor", aIng);
        SetRef(so, "_machineAnchor",     aMac);
        SetRef(so, "_toppingsAnchor",    aTop);
        SetRef(so, "_counterAnchor",     aCount);
        SetRef(so, "_contentAnchor",     content);
        so.ApplyModifiedPropertiesWithoutUndo();

        // ставим кружку на стол ингредиентов
        cup.transform.position = aIng.position;
        return ctrl;
    }

    static void SetRef(SerializedObject so, string field, Object value)
    {
        var p = so.FindProperty(field);
        if (p != null) p.objectReferenceValue = value;
    }

    // Вертикальная шкала-заполнение (Image: Filled, Vertical) на UI-панели.
    static Image VerticalBar(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color color)
    {
        // Фон
        var bg = new GameObject(name + "_BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        SetRect((RectTransform)bg.transform, aMin, aMax);
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        // Заполнение
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(bg.transform, false);
        SetRect((RectTransform)go.transform, Vector2.zero, Vector2.one);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.sprite = WhiteSprite();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Vertical;
        img.fillOrigin = (int)Image.OriginVertical.Bottom;
        img.fillAmount = 0.5f;
        img.raycastTarget = false;
        return img;
    }

    // Находит ходячего гостя (НЕ главного героя!). Приоритет — stickman под VisitorBasis,
    // т.к. именно его двигает ProcessVisitor. Так мы не заденем главного героя за стойкой.
    static GameObject FindSceneStickman()
    {
        var vb = GameObject.Find("VisitorBasis");
        if (vb != null)
        {
            foreach (Transform ch in vb.transform)
                if (ch.name.ToLower().Contains("stickman"))
                    return ch.gameObject;
            // любой видимый ребёнок VisitorBasis — это и есть модель гостя
            if (vb.transform.childCount > 0)
                return vb.transform.GetChild(0).gameObject;
        }
        Debug.LogWarning("CoffeGameSetup: модель ходячего гостя не найдена под VisitorBasis — " +
                         "назначь _existingGuest вручную (и НЕ выбирай главного героя!).");
        return null;
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

    private static Sprite _whiteSprite;
    // Плоский белый спрайт без 9-slice — чёткие шкалы (пункт 3)
    static Sprite WhiteSprite()
    {
        if (_whiteSprite != null) return _whiteSprite;
        var tex = Texture2D.whiteTexture;
        _whiteSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        return _whiteSprite;
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
