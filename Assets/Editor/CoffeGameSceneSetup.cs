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

        // ── 3D-ПРЕДМЕТЫ НА СТОЛАХ (вместо кнопок) ───────────────────────────
        //  Игрок выбирает напиток кликом по предмету на столе. Контейнеры-станции
        //  ставятся у маркеров камеры; предметы можно потом перетащить на нужные места.
        Transform ingStation = MakeStation("Items_Ingredients", FindMarker("Ingridients"));
        Transform macStation = MakeStation("Items_Machine",     FindMarker("Cofemachine", "CofeMashine"));
        Transform topStation = MakeStation("Items_Toppings",    FindMarker("CofeBasis"));

        // Ингредиенты — 11 типов (порядок enum CoffeeType)
        string[] ingNames = { "Эспрессо", "Американо", "Капучино", "Латте", "Мокко", "Травяной чай", "Зелёный чай", "Вода", "Горячий шоколад", "Чёрный кофе", "Кофе Правды" };
        for (int i = 0; i < ingNames.Length; i++)
        {
            var it = MakeItem(ingStation, "Drink_" + i, ingNames[i], i, new Color(0.5f, 0.3f, 0.15f));
            it.kind = IngredientItem.ItemKind.Drink;
            it.drinkType = (CoffeeType)i;
        }

        // Машина — объёмы (3) + сладость (4) + заварить (1)
        string[] volN = { "Маленький", "Средний", "Большой" };
        for (int i = 0; i < volN.Length; i++)
        {
            var it = MakeItem(macStation, "Vol_" + i, volN[i], i, new Color(0.2f, 0.4f, 0.7f));
            it.kind = IngredientItem.ItemKind.Volume;
            it.volume = (Volume)i;
        }
        string[] swN = { "Без сахара", "Слабо", "Средне", "Сладко" };
        for (int i = 0; i < swN.Length; i++)
        {
            var it = MakeItem(macStation, "Sweet_" + i, swN[i], i + 4, new Color(0.7f, 0.7f, 0.75f));
            it.kind = IngredientItem.ItemKind.Sweetness;
            it.sweetness = (SweetnessLevel)i;
        }
        var brewItem = MakeItem(macStation, "Brew", "ЗАВАРИТЬ", 9, new Color(0.7f, 0.25f, 0.15f));
        brewItem.kind = IngredientItem.ItemKind.Brew;

        // Топпинги — 6 (порядок enum Topping)
        string[] topNames = { "Без топпинга", "Сливки", "Корица", "Карамель", "Шоколад", "Мята" };
        for (int i = 0; i < topNames.Length; i++)
        {
            var it = MakeItem(topStation, "Top_" + i, topNames[i], i, new Color(0.4f, 0.6f, 0.3f));
            it.kind = IngredientItem.ItemKind.Topping;
            it.topping = (Topping)i;
        }

        // ── Кружка игрока (прикреплена к Main Camera, ездит с игроком) ───────
        CupController cupController = MakeCup(mainCam);

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

        // CustomerController + захват размера/анимации существующего бота (пункт 4)
        Vector3 botScale; RuntimeAnimatorController botCtrl;
        CaptureAndDeleteSceneBots(out botScale, out botCtrl);
        new W(custCtrl)
            .Ref("_processVisitor", pv)
            .Ref("_visitorRoot", visitorRoot)
            .Ref("_satisfactionBarPrefab", sbComp)
            .Ref("_botController", botCtrl)
            .Apply();
        SetVector3(custCtrl, "_botScale", botScale);

        // CoffeeCraftingSystem (3D-предметы + кружка)
        new W(craft)
            .Ref("_stages", stages)
            .Ref("_cup", cupController)
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

    // Контейнер-станция ПЕРЕД камерой-маркером (чтобы предметы были в кадре),
    // ориентирован так же, как камера. Если маркер не найден — в начале координат.
    static Transform MakeStation(string name, Transform marker)
    {
        DestroyIfExists(name);
        var go = new GameObject(name);
        if (marker != null)
        {
            go.transform.position = marker.position + marker.forward * 2.5f;
            go.transform.rotation = marker.rotation;
        }
        return go.transform;
    }

    // Кликабельный предмет-кубик с подписью. index — позиция в сетке станции.
    static IngredientItem MakeItem(Transform station, string goName, string label, int index, Color color)
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); // BoxCollider уже есть → OnMouseDown работает
        cube.name = goName;
        cube.transform.SetParent(station, false);

        // Сетка в плоскости, обращённой к камере: x — столбцы, y — ряды (вниз)
        int col = index % 4, rowi = index / 4;
        cube.transform.localPosition = new Vector3(-0.55f + col * 0.36f, 0.4f - rowi * 0.36f, 0f);
        cube.transform.localScale = Vector3.one * 0.28f;

        var mr = cube.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = color;
            mat.EnableKeyword("_EMISSION");
            mr.sharedMaterial = mat;
        }

        // Подпись над предметом
        var lbl = new GameObject("Label");
        lbl.transform.SetParent(cube.transform, false);
        lbl.transform.localPosition = new Vector3(0f, 0.9f, 0f);
        lbl.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // текстом к камере
        lbl.transform.localScale = Vector3.one * 0.12f;
        var tm = lbl.AddComponent<TextMesh>();
        tm.text = label;
        tm.fontSize = 48;
        tm.characterSize = 0.1f;
        tm.anchor = TextAnchor.LowerCenter;
        tm.alignment = TextAlignment.Center;
        tm.color = Color.white;
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        if (font != null)
        {
            tm.font = font;
            var lblMr = lbl.GetComponent<MeshRenderer>();
            if (lblMr != null) lblMr.sharedMaterial = font.material;
        }

        return cube.AddComponent<IngredientItem>();
    }

    // Кружка, прикреплённая к камере (ездит с игроком)
    static CupController MakeCup(Camera cam)
    {
        DestroyIfExists("PlayerCup");
        var cup = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        cup.name = "PlayerCup";
        Object.DestroyImmediate(cup.GetComponent<Collider>()); // кружке коллайдер не нужен
        cup.transform.localScale = new Vector3(0.12f, 0.1f, 0.12f);

        var mr = cup.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            var mat = new Material(Shader.Find("Standard"));
            mat.color = new Color(0.95f, 0.95f, 0.95f);
            mr.sharedMaterial = mat;
        }

        if (cam != null)
        {
            cup.transform.SetParent(cam.transform, false);
            cup.transform.localPosition = new Vector3(0.35f, -0.28f, 0.9f); // нижний правый угол обзора
            cup.transform.localRotation = Quaternion.identity;
        }

        var anchor = new GameObject("ContentAnchor");
        anchor.transform.SetParent(cup.transform, false);
        anchor.transform.localPosition = new Vector3(0f, 1.1f, 0f);

        var ctrl = cup.AddComponent<CupController>();
        var so = new SerializedObject(ctrl);
        var p = so.FindProperty("_contentAnchor");
        if (p != null) p.objectReferenceValue = anchor.transform;
        so.ApplyModifiedPropertiesWithoutUndo();
        return ctrl;
    }

    // Снимает размер + контроллер анимации с существующего в сцене stickman-а
    // и удаляет ВСЕ такие объекты из сцены (пункт 4).
    static void CaptureAndDeleteSceneBots(out Vector3 scale, out RuntimeAnimatorController controller)
    {
        scale = Vector3.one;
        controller = null;

        var found = new List<GameObject>();
        foreach (var go in Object.FindObjectsOfType<GameObject>())
        {
            // корневой объект-инстанс stickman (имя содержит "stickman")
            if (go.name.ToLower().Contains("stickman") && go.transform.parent == null)
                found.Add(go);
        }
        // запасной вариант: ищем по имени среди всех (если в иерархии)
        if (found.Count == 0)
        {
            foreach (var go in Object.FindObjectsOfType<GameObject>())
                if (go.name.ToLower().Contains("stickman"))
                    found.Add(go);
        }

        if (found.Count > 0)
        {
            var template = found[0];
            scale = template.transform.localScale;
            var anim = template.GetComponentInChildren<Animator>();
            if (anim != null) controller = anim.runtimeAnimatorController;
            Debug.Log($"CoffeGameSetup: шаблон бота — '{template.name}', scale={scale}, удалено из сцены: {found.Count}");
        }
        else
        {
            Debug.LogWarning("CoffeGameSetup: существующий бот (stickman) в сцене не найден — используется scale=1, контроллер из префаба.");
        }

        foreach (var go in found)
            Object.DestroyImmediate(go);
    }

    static void SetVector3(Object target, string field, Vector3 value)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p != null) p.vector3Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
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
