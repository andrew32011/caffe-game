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
using UnityEditor.Animations;
using TMPro;

public static class CoffeGameSceneSetup
{
    const string SystemsRootName = "--- GAME SYSTEMS ---";
    const string CanvasName       = "GameCanvas";

    // Авто-локализация статических подписей: если русский текст есть в общей таблице
    // UiTranslations (все языки), вешаем на надпись LocalizeYG. Кнопки покрыты тоже —
    // их подпись создаётся через Text(). Динамические тексты (счётчики, диалоги) в таблицу
    // не входят и переводятся рантайм-кодом (Loc.T).
    static void TryLocalize(GameObject go, string ru)
    {
        if (go != null && UiTranslations.Has(ru))
            go.AddComponent<LocalizeYG>().Set(ru, UiTranslations.Get(ru, "en"));
    }

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
        var goChallenge = Child(root, "DailyChallenge"); // Батч 6

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
        var dailyChallenge = goChallenge.AddComponent<DailyChallenge>(); // Батч 6

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
        // Адаптив под ПК и горизонтальный телефон, без деформации (требования 1.6/1.10).
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var ct = canvasGO.transform;

        // ── Диалоговая панель (низ экрана) ──────────────────────────────────
        var dialoguePanel = Panel("DialoguePanel", ct, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.27f), new Color(0.05f, 0.05f, 0.1f, 0.85f));
        var speakerName   = Text("SpeakerName", dialoguePanel.transform, "Имя", 30, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.7f), new Vector2(0.6f, 0.98f));
        var dialogueText  = Text("DialogueText", dialoguePanel.transform, "Текст реплики...", 28, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.68f));
        dialogueText.enableAutoSizing = false; // печатная машинка: фикс. размер, без «прыжков»
        var continueHintTmp = Text("ContinueHint", dialoguePanel.transform, "нажмите для продолжения", 20, TextAlignmentOptions.BottomRight, new Vector2(0.45f, 0.0f), new Vector2(0.98f, 0.18f));
        continueHintTmp.gameObject.AddComponent<BlinkText>();                       // мигание
        continueHintTmp.gameObject.AddComponent<LocalizeYG>().Set("нажмите для продолжения", "click to continue");
        var continueHint = continueHintTmp.gameObject;

        // ── Заставка дня (центр) ────────────────────────────────────────────
        var dayIntroPanel = Panel("DayIntroPanel", ct, new Vector2(0.3f, 0.4f), new Vector2(0.7f, 0.6f), new Color(0f, 0f, 0f, 0.8f));
        // Текст дня занимает всю панель и центрируется (пункт 5: больше не уезжает вниз).
        var dayIntroText  = Text("DayIntroText", dayIntroPanel.transform, "ДЕНЬ 1", 64, TextAlignmentOptions.Center, new Vector2(0f, 0f), new Vector2(1f, 1f));
        // Иконка сна (луна/звёзды) — видна только на заставке «Сон»; висит НАД панелью,
        // чтобы не сдвигать надпись (пункт 1, пункт 5).
        var sleepIcon = IconImage("SleepIcon", dayIntroPanel.transform, "Assets/Mini UI/UI Icons/MoonStars.png",
            new Vector2(0.4f, 1.02f), new Vector2(0.6f, 1.4f));
        sleepIcon.gameObject.SetActive(false);

        // ── Сообщение (центр, CanvasGroup) ──────────────────────────────────
        var messagePanel = Panel("MessagePanel", ct, new Vector2(0.2f, 0.42f), new Vector2(0.8f, 0.58f), new Color(0.1f, 0f, 0f, 0.8f));
        var messageGroup = messagePanel.AddComponent<CanvasGroup>();
        var messageText  = Text("MessageText", messagePanel.transform, "Сообщение", 34, TextAlignmentOptions.Center, Vector2.zero, Vector2.one);

        // ── Заказ гостя (верх центр) ────────────────────────────────────────
        // Пункт 6: НИЖЕ шкалы удовлетворённости (она в 0.93–0.97), чтобы текст
        // намёка/заказа не оказывался на заднем фоне шкалы.
        var orderText = Text("OrderDisplayText", ct, "Заказ: ...", 30, TextAlignmentOptions.Center, new Vector2(0.28f, 0.865f), new Vector2(0.72f, 0.915f));

        // ── HUD «Заказ дня» (Батч 6, слева под кассой) ──────────────────────
        // Пункт 5: цель дня была слишком мелкой — крупнее и шире, чтобы читалась.
        var challengeHud = Text("DailyChallengeHud", ct, "", 30, TextAlignmentOptions.Left, new Vector2(0.02f, 0.80f), new Vector2(0.40f, 0.89f));
        challengeHud.color = new Color(0.95f, 0.88f, 0.5f);
        challengeHud.enableWordWrapping = true;

        // ── Ачивка («Отлично!»/«В точку!») сверху ───────────────────────────
        var achievement = Text("AchievementText", ct, "В точку!", 48, TextAlignmentOptions.Center, new Vector2(0.25f, 0.72f), new Vector2(0.75f, 0.79f));
        achievement.color = new Color(0.4f, 1f, 0.5f);
        achievement.gameObject.SetActive(false);

        // ── Кнопка «Подать» (низ по центру; пункт 10) ───────────────────────
        var serveBtn = Btn("BtnServe", ct, "Подать");
        SetRect(serveBtn.GetComponent<RectTransform>(), new Vector2(0.4f, 0.04f), new Vector2(0.6f, 0.12f));
        Juice(serveBtn, pulse: true, shine: true); // главный CTA — пульс + блеск
        serveBtn.gameObject.SetActive(false);

        // ── Кнопка рекламной подсказки (после 2 провалов подряд, пункт 4.1) ──
        var adHintBtn = Btn("BtnAdHint", ct, "Подсказка (реклама)");
        SetRect(adHintBtn.GetComponent<RectTransform>(), new Vector2(0.73f, 0.86f), new Vector2(0.985f, 0.93f));
        adHintBtn.gameObject.SetActive(false);

        // ── Кнопка «Комплимент за монеты» (пункт 2: поднять настроение клиента) ──
        var complimentBtn = Btn("BtnCompliment", ct, "Комплимент");
        SetRect(complimentBtn.GetComponent<RectTransform>(), new Vector2(0.38f, 0.14f), new Vector2(0.62f, 0.215f));
        complimentBtn.gameObject.SetActive(false);

        // ── 2D-эффекты достижений (UiEffects, пункт 5) ──────────────────────
        var uiFx = canvasGO.AddComponent<UiEffects>();
        new W(uiFx)
            .Ref("_root", canvasGO.GetComponent<RectTransform>())
            .Ref("_coinSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mini UI/Icons/Bronze Coin.png"))
            .Ref("_starSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mini UI/Icons/Star Yellow.png"))
            .Ref("_heartSprite", AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mini UI/Icons/Heart.png"))
            .Ref("_font", UiFont())
            .Apply();

        // ── Напоминание «Убрать рекламу» (органично, после рекламы) ─────────────────
        // Вместо постоянной кнопки-«бельма» — всплывашка ПОСЛЕ рекламы с покупкой отключения
        // (YG2 Payments) и поддержкой автора. Подписи — из UiTranslations (все языки).
        var adPrompt = canvasGO.AddComponent<AdRemovalPrompt>();
        new W(adPrompt).Ref("_font", UiFont()).Apply();

        // ── HUD «Часа пик» (Батч 11): плашка темпа + очередь; строит свой UI в коде ──
        var rushHud = canvasGO.AddComponent<RushHudUI>();
        new W(rushHud).Ref("_font", UiFont()).Apply();

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

        // Комментарий после шага («Супер!/Не то») — отдельной строкой ниже заказа,
        // тоже не на фоне шкалы (пункт 6).
        var commentText = Text("CommentText", ct, "", 34, TextAlignmentOptions.Center, new Vector2(0.28f, 0.80f), new Vector2(0.72f, 0.855f));
        commentText.color = new Color(1f, 0.95f, 0.6f);
        commentText.gameObject.SetActive(false);

        // ── Деньги кофейни (сверху слева, пункт 5) с иконкой монеты (пункт 7) ─
        var coinIcon = IconImage("CoinIcon", ct, "Assets/Mini UI/Icons/Bronze Coin.png",
            new Vector2(0.02f, 0.9f), new Vector2(0.05f, 0.96f));
        var coinsText = Text("CoinsText", ct, "Касса: 0", 28, TextAlignmentOptions.Left, new Vector2(0.055f, 0.9f), new Vector2(0.28f, 0.96f));
        coinsText.color = new Color(1f, 0.9f, 0.4f);
        var coinsUI = coinsText.gameObject.AddComponent<CoinsUI>();
        new W(coinsUI).Ref("_text", coinsText).Apply();

        // ── Выбор ингредиента + кнопка «Подтвердить» (пункт 2) ──────────────
        var selectedText = Text("SelectedText", ct, "Выбрано: …", 28, TextAlignmentOptions.Center, new Vector2(0.3f, 0.18f), new Vector2(0.7f, 0.24f));
        selectedText.gameObject.SetActive(false);
        var confirmBtn = Btn("BtnConfirm", ct, "Подтвердить");
        SetRect(confirmBtn.GetComponent<RectTransform>(), new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.13f));
        Juice(confirmBtn, pulse: true);
        confirmBtn.gameObject.SetActive(false);

        // ── Экран «нет денег» с рекламой за монеты (пункт 5) ────────────────
        var noMoneyPanel = Panel("NoMoneyPanel", ct, new Vector2(0.32f, 0.32f), new Vector2(0.68f, 0.68f), new Color(0.06f, 0.05f, 0.04f, 0.95f));
        Text("NoMoneyText", noMoneyPanel.transform, "Не хватает монет на ингредиент!", 28, TextAlignmentOptions.Top, new Vector2(0.05f, 0.6f), new Vector2(0.95f, 0.95f));
        var adBtn    = Btn("BtnWatchAd", noMoneyPanel.transform, "Смотреть рекламу (+60)");
        SetRect(adBtn.GetComponent<RectTransform>(), new Vector2(0.1f, 0.34f), new Vector2(0.9f, 0.5f));
        var noMoneyClose = Btn("BtnNoMoneyClose", noMoneyPanel.transform, "Закрыть");
        SetRect(noMoneyClose.GetComponent<RectTransform>(), new Vector2(0.3f, 0.12f), new Vector2(0.7f, 0.26f));
        var adForCoins = noMoneyPanel.AddComponent<AdForCoins>();
        new W(adForCoins).Ref("_panel", noMoneyPanel).Apply();
        AddPersistentClick(adBtn, adForCoins, "WatchAd");
        AddPersistentClick(noMoneyClose, adForCoins, "Close");
        ApplyPanelSprite(noMoneyPanel);
        noMoneyPanel.SetActive(false);

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
        // HUD-иконка «Подсказка» (нижняя в правом доке). Пульс — приглашает при затыке.
        var btnHint = IconBtn("BtnHint", ct, "Assets/Mini UI/Icons/Blue Energy.png", "Подсказка");
        SetRect(btnHint.GetComponent<RectTransform>(), new Vector2(0.905f, 0.64f), new Vector2(0.99f, 0.74f));
        Juice(btnHint, pulse: true);

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
        var resultPanel = Panel("DayResultPanel", ct, new Vector2(0.22f, 0.20f), new Vector2(0.78f, 0.80f), new Color(0.05f, 0.05f, 0.12f, 0.95f));
        var resultGroup = resultPanel.AddComponent<CanvasGroup>();
        var resDayNum   = Text("DayNumberText", resultPanel.transform, "ДЕНЬ 1 ЗАВЕРШЁН", 36, TextAlignmentOptions.Top, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f));
        var resCoins    = Text("CoinsEarnedText", resultPanel.transform, "+0 монет", 28, TextAlignmentOptions.Center, new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.875f));

        // Батч 6: предупреждение о сгорании daily-стрика (loss aversion)
        var resStreak   = Text("StreakWarningText", resultPanel.transform, "", 18, TextAlignmentOptions.Center, new Vector2(0.05f, 0.745f), new Vector2(0.95f, 0.79f));
        resStreak.color = new Color(1f, 0.6f, 0.3f);
        resStreak.gameObject.SetActive(false);

        // Батч 6: трекер «Путь к 10 000» с прогнозом темпа
        var resJourneyProg = Text("JourneyProgressText", resultPanel.transform, "0 / 10000", 18, TextAlignmentOptions.Center, new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.745f));
        var resJourneyFill = HorizontalFill("JourneyFill", resultPanel.transform, new Vector2(0.10f, 0.675f), new Vector2(0.90f, 0.70f), new Color(1f, 0.85f, 0.4f));
        var resJourneyFore = Text("JourneyForecastText", resultPanel.transform, "", 18, TextAlignmentOptions.Center, new Vector2(0.05f, 0.63f), new Vector2(0.95f, 0.675f));

        var resTotal    = Text("TotalCoinsText", resultPanel.transform, "Всего: 0 монет", 22, TextAlignmentOptions.Center, new Vector2(0.05f, 0.575f), new Vector2(0.95f, 0.63f));
        var resEnd      = Text("DayEndText", resultPanel.transform, "Итоги дня...", 20, TextAlignmentOptions.Top, new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.565f));

        var btnContinue = Btn("BtnContinue", resultPanel.transform, "Продолжить");
        SetRect(btnContinue.GetComponent<RectTransform>(), new Vector2(0.06f, 0.29f), new Vector2(0.48f, 0.385f));
        Juice(btnContinue, pulse: true);
        // Батч 2: «Удвоить заработок — реклама» (rewarded)
        var btnDouble = Btn("BtnDoubleEarnings", resultPanel.transform, "Удвоить — реклама");
        SetRect(btnDouble.GetComponent<RectTransform>(), new Vector2(0.52f, 0.29f), new Vector2(0.94f, 0.385f));
        var btnDoubleLabel = btnDouble.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        btnDouble.gameObject.SetActive(false);
        // Батч 6: «Сохранить комбо — реклама» (rewarded, перенос серии на завтра)
        var btnSaveCombo = Btn("BtnSaveCombo", resultPanel.transform, "Сохранить комбо");
        SetRect(btnSaveCombo.GetComponent<RectTransform>(), new Vector2(0.06f, 0.18f), new Vector2(0.48f, 0.275f));
        var btnSaveComboLabel = btnSaveCombo.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        btnSaveCombo.gameObject.SetActive(false);
        // Батч 3: «Улучшить кофейню» — открывает магазин апгрейдов прямо с итогов дня.
        var btnShop = Btn("BtnUpgradeShop", resultPanel.transform, "Улучшить кофейню");
        SetRect(btnShop.GetComponent<RectTransform>(), new Vector2(0.52f, 0.18f), new Vector2(0.94f, 0.275f));

        // ── Магазин апгрейдов кофейни (Батч 3) ──────────────────────────────
        var shopPanel = Panel("UpgradeShopPanel", ct, new Vector2(0.2f, 0.14f), new Vector2(0.8f, 0.86f), new Color(0.05f, 0.05f, 0.12f, 0.97f));
        Text("UpgradeShopTitle", shopPanel.transform, "Кофейня · улучшения", 34, TextAlignmentOptions.Top, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f));
        var shopTitles = new TextMeshProUGUI[3];
        var shopInfos  = new TextMeshProUGUI[3];
        var shopBuys   = new Button[3];
        var shopBuyLbl = new TextMeshProUGUI[3];
        var shopFill   = new Image[3]; // Батч 11: текущий эффект (шкала «сейчас»)
        var shopGhost  = new Image[3]; // Батч 11: эффект после покупки (полупрозрачный «станет»)
        for (int i = 0; i < 3; i++)
        {
            float top = 0.84f - i * 0.235f;
            float bot = top - 0.205f;
            var row = Panel($"UpgradeRow{i}", shopPanel.transform, new Vector2(0.05f, bot), new Vector2(0.95f, top), new Color(1f, 1f, 1f, 0.04f));
            shopTitles[i] = Text($"UpgTitle{i}", row.transform, "—", 26, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.56f), new Vector2(0.63f, 0.97f));
            shopInfos[i]  = Text($"UpgInfo{i}",  row.transform, "—", 18, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.28f), new Vector2(0.63f, 0.55f));
            // Батч 11: визуализация мастерства — шкала «сейчас/станет» (до/после апгрейда).
            var barBg = Panel($"UpgBarBG{i}", row.transform, new Vector2(0.03f, 0.07f), new Vector2(0.63f, 0.23f), new Color(0f, 0f, 0f, 0.5f));
            shopGhost[i] = RowFill($"UpgGhost{i}", barBg.transform, new Color(0.42f, 0.85f, 0.45f, 0.35f)); // «станет» (сзади)
            shopFill[i]  = RowFill($"UpgFill{i}",  barBg.transform, new Color(0.42f, 0.85f, 0.45f, 1f));    // «сейчас» (спереди)
            var buy = Btn($"BtnUpg{i}", row.transform, "Купить");
            SetRect(buy.GetComponent<RectTransform>(), new Vector2(0.66f, 0.2f), new Vector2(0.97f, 0.8f));
            shopBuys[i]   = buy;
            shopBuyLbl[i] = buy.GetComponentInChildren<TextMeshProUGUI>();
        }
        var btnShopClose = Btn("BtnUpgradeShopClose", shopPanel.transform, "Закрыть");
        SetRect(btnShopClose.GetComponent<RectTransform>(), new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.1f));
        var upgradeShop = shopPanel.AddComponent<UpgradeShopUI>();
        new W(upgradeShop)
            .Ref("_panel", shopPanel)
            .Ref("_closeButton", btnShopClose)
            .Arr("_titleTexts", shopTitles)
            .Arr("_infoTexts", shopInfos)
            .Arr("_buyButtons", shopBuys)
            .Arr("_buyLabels", shopBuyLbl)
            .Arr("_effectFills", shopFill)
            .Arr("_effectGhosts", shopGhost)
            .Apply();
        ApplyPanelSprite(shopPanel);
        shopPanel.SetActive(false);

        // ── Гейт путешествия (пункт 1): не хватило денег — начать заново / купить ──
        var journeyPanel = Panel("JourneyGatePanel", ct, new Vector2(0.25f, 0.28f), new Vector2(0.75f, 0.72f), new Color(0.05f, 0.05f, 0.12f, 0.97f));
        var journeyText  = Text("JourneyGateText", journeyPanel.transform, "Не хватает на путешествие…", 28, TextAlignmentOptions.Top, new Vector2(0.06f, 0.42f), new Vector2(0.94f, 0.94f));
        var btnRestart   = Btn("BtnJourneyRestart", journeyPanel.transform, "Начать заново");
        SetRect(btnRestart.GetComponent<RectTransform>(), new Vector2(0.1f, 0.24f), new Vector2(0.9f, 0.38f));
        var btnBuy       = Btn("BtnJourneyBuy", journeyPanel.transform, "Купить монеты");
        SetRect(btnBuy.GetComponent<RectTransform>(), new Vector2(0.1f, 0.07f), new Vector2(0.9f, 0.21f));
        var journeyGate  = journeyPanel.AddComponent<JourneyGateUI>();
        new W(journeyGate)
            .Ref("_panel", journeyPanel)
            .Ref("_text", journeyText)
            .Ref("_restartButton", btnRestart)
            .Ref("_buyButton", btnBuy)
            .Apply();
        ApplyPanelSprite(journeyPanel);
        journeyPanel.SetActive(false);

        // ── Ежедневный бонус (Батч 2) + 7-дневный календарь (Батч 13) ────────
        // Панель выше: в полосе y≈0.62–0.77 DailyBonusUI строит календарь в коде.
        var bonusPanel = Panel("DailyBonusPanel", ct, new Vector2(0.28f, 0.24f), new Vector2(0.72f, 0.78f), new Color(0.05f, 0.05f, 0.12f, 0.97f));
        var bonusGift  = IconImage("DailyBonusIcon", bonusPanel.transform, "Assets/Mini UI/Icons/Gift.png",
            new Vector2(0.42f, 0.80f), new Vector2(0.58f, 0.96f));
        var bonusTitle = Text("DailyBonusTitle", bonusPanel.transform, "Бонус за вход", 28, TextAlignmentOptions.Center, new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.615f));
        var bonusReward= Text("DailyBonusReward", bonusPanel.transform, "+50 монет", 34, TextAlignmentOptions.Center, new Vector2(0.05f, 0.40f), new Vector2(0.95f, 0.51f));
        var btnClaim   = Btn("BtnBonusClaim", bonusPanel.transform, "Забрать");
        SetRect(btnClaim.GetComponent<RectTransform>(), new Vector2(0.15f, 0.24f), new Vector2(0.85f, 0.35f));
        var btnClaimLabel = btnClaim.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        var btnBonusDouble = Btn("BtnBonusDouble", bonusPanel.transform, "Удвоить — реклама");
        SetRect(btnBonusDouble.GetComponent<RectTransform>(), new Vector2(0.15f, 0.09f), new Vector2(0.85f, 0.20f));
        var btnBonusDoubleLabel = btnBonusDouble.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        var dailyBonus = bonusPanel.AddComponent<DailyBonusUI>();
        new W(dailyBonus)
            .Ref("_panel", bonusPanel)
            .Ref("_titleText", bonusTitle)
            .Ref("_rewardText", bonusReward)
            .Ref("_claimButton", btnClaim)
            .Ref("_claimLabel", btnClaimLabel)
            .Ref("_doubleButton", btnBonusDouble)
            .Ref("_doubleLabel", btnBonusDoubleLabel)
            .Apply();
        ApplyPanelSprite(bonusPanel);
        bonusPanel.SetActive(false);

        // ── Меню/настройки + пауза (Батч 4) ─────────────────────────────────
        // Кнопка-«Меню» в правом верхнем углу (всегда видна). Открывает настройки и паузу.
        // HUD-иконка «Меню» (верхняя в правом доке).
        var settingsBtn = IconBtn("BtnSettings", ct, "Assets/Mini UI/Icons/Settings.png", "Меню");
        SetRect(settingsBtn.GetComponent<RectTransform>(), new Vector2(0.905f, 0.88f), new Vector2(0.99f, 0.98f));
        Juice(settingsBtn, pulse: false);

        var settingsPanel = Panel("SettingsPanel", ct, new Vector2(0.3f, 0.22f), new Vector2(0.7f, 0.82f), new Color(0.05f, 0.05f, 0.12f, 0.97f));
        Text("SettingsTitle", settingsPanel.transform, "Настройки", 34, TextAlignmentOptions.Top, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f));
        Text("MusicLabel", settingsPanel.transform, "Музыка", 24, TextAlignmentOptions.Left, new Vector2(0.08f, 0.80f), new Vector2(0.5f, 0.87f));
        var musicSlider = MakeSlider("MusicSlider", settingsPanel.transform, new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.79f), 0.5f);
        Text("SfxLabel", settingsPanel.transform, "Звуки", 24, TextAlignmentOptions.Left, new Vector2(0.08f, 0.655f), new Vector2(0.5f, 0.725f));
        var sfxSlider = MakeSlider("SfxSlider", settingsPanel.transform, new Vector2(0.08f, 0.585f), new Vector2(0.92f, 0.645f), 0.8f);
        // Отдельный ползунок «Голоса» — громкость бубнёжа героев, независимо от эффектов.
        Text("VoiceLabel", settingsPanel.transform, "Голоса", 24, TextAlignmentOptions.Left, new Vector2(0.08f, 0.51f), new Vector2(0.5f, 0.58f));
        var voiceSlider = MakeSlider("VoiceSlider", settingsPanel.transform, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.50f), 0.9f);
        var fullscreenBtn = Btn("BtnFullscreen", settingsPanel.transform, "Полный экран: выкл");
        SetRect(fullscreenBtn.GetComponent<RectTransform>(), new Vector2(0.1f, 0.335f), new Vector2(0.9f, 0.41f));
        var fullscreenLabel = fullscreenBtn.GetComponentInChildren<TextMeshProUGUI>();
        var leaderboardBtn = Btn("BtnLeaderboard", settingsPanel.transform, "Таблица лидеров");
        SetRect(leaderboardBtn.GetComponent<RectTransform>(), new Vector2(0.1f, 0.245f), new Vector2(0.9f, 0.315f));
        // Батч 11: рекорды Бесконечного режима (кнопка активна лишь после прохождения сюжета — SettingsUI сам скрывает).
        var endlessLbBtn = Btn("BtnEndlessLeaderboard", settingsPanel.transform, "Рекорды: бесконечный режим");
        SetRect(endlessLbBtn.GetComponent<RectTransform>(), new Vector2(0.1f, 0.15f), new Vector2(0.9f, 0.22f));
        endlessLbBtn.gameObject.SetActive(false);
        var settingsClose = Btn("BtnSettingsClose", settingsPanel.transform, "Закрыть");
        SetRect(settingsClose.GetComponent<RectTransform>(), new Vector2(0.3f, 0.04f), new Vector2(0.7f, 0.13f));
        ApplyPanelSprite(settingsPanel);
        settingsPanel.SetActive(false);

        // ── Таблица лидеров (Батч 4): компонент YG2 LeaderboardYG, счёт = монеты ──
        var lbPanel = Panel("LeaderboardPanel", ct, new Vector2(0.3f, 0.16f), new Vector2(0.7f, 0.86f), new Color(0.05f, 0.05f, 0.12f, 0.98f));
        Text("LeaderboardTitle", lbPanel.transform, "Таблица лидеров", 32, TextAlignmentOptions.Top, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.99f));
        var lbEntries = LegacyText("LBEntries", lbPanel.transform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.88f));
        var lbClose = Btn("BtnLeaderboardClose", lbPanel.transform, "Закрыть");
        SetRect(lbClose.GetComponent<RectTransform>(), new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.14f));
        var lbComp = lbPanel.AddComponent<YG.LeaderboardYG>();
        lbComp.nameLB          = GameManager.LeaderboardName; // "coins" — создать таблицу в консоли Яндекса
        lbComp.entriesText     = lbEntries;
        lbComp.advanced        = false;
        lbComp.updateLBMethod  = YG.LeaderboardYG.UpdateLBMethod.OnEnable; // грузит при открытии
        lbComp.quantityTop     = 3;
        lbComp.quantityAround  = 6;
        lbComp.maxQuantityPlayers = 20;
        lbComp.playerPhoto     = YG.LeaderboardYG.PlayerPhoto.NonePhoto;   // без фото — без ассетов
        EditorUtility.SetDirty(lbComp);
        ApplyPanelSprite(lbPanel);
        lbPanel.SetActive(false);

        // ── Таблица лидеров Бесконечного режима (Батч 11): счёт = самый дальний день ──
        var elbPanel = Panel("EndlessLeaderboardPanel", ct, new Vector2(0.3f, 0.16f), new Vector2(0.7f, 0.86f), new Color(0.05f, 0.05f, 0.12f, 0.98f));
        Text("EndlessLeaderboardTitle", elbPanel.transform, "Рекорды: бесконечный режим", 30, TextAlignmentOptions.Top, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.99f));
        var elbEntries = LegacyText("ELBEntries", elbPanel.transform, new Vector2(0.08f, 0.18f), new Vector2(0.92f, 0.88f));
        var elbClose = Btn("BtnEndlessLeaderboardClose", elbPanel.transform, "Закрыть");
        SetRect(elbClose.GetComponent<RectTransform>(), new Vector2(0.35f, 0.04f), new Vector2(0.65f, 0.14f));
        var elbComp = elbPanel.AddComponent<YG.LeaderboardYG>();
        elbComp.nameLB          = GameManager.EndlessLeaderboardName; // "endless" — создать таблицу в консоли Яндекса
        elbComp.entriesText     = elbEntries;
        elbComp.advanced        = false;
        elbComp.updateLBMethod  = YG.LeaderboardYG.UpdateLBMethod.OnEnable;
        elbComp.quantityTop     = 3;
        elbComp.quantityAround  = 6;
        elbComp.maxQuantityPlayers = 20;
        elbComp.playerPhoto     = YG.LeaderboardYG.PlayerPhoto.NonePhoto;
        EditorUtility.SetDirty(elbComp);
        ApplyPanelSprite(elbPanel);
        elbPanel.SetActive(false);

        // SettingsUI вешаем на ВСЕГДА АКТИВНЫЙ Canvas (иначе кнопка «Меню» не подпишется,
        // пока панель скрыта). Панели он включает/выключает сам.
        var settings = canvasGO.AddComponent<SettingsUI>();
        new W(settings)
            .Ref("_panel", settingsPanel)
            .Ref("_openButton", settingsBtn)
            .Ref("_closeButton", settingsClose)
            .Ref("_musicSlider", musicSlider)
            .Ref("_sfxSlider", sfxSlider)
            .Ref("_voiceSlider", voiceSlider)
            .Ref("_fullscreenButton", fullscreenBtn)
            .Ref("_fullscreenLabel", fullscreenLabel)
            .Ref("_leaderboardButton", leaderboardBtn)
            .Ref("_leaderboardPanel", lbPanel)
            .Ref("_leaderboardCloseButton", lbClose)
            .Ref("_endlessLbButton", endlessLbBtn)
            .Ref("_endlessLbPanel", elbPanel)
            .Ref("_endlessLbCloseButton", elbClose)
            .Apply();

        // ── Валютный HUD с иконками (Батч 13) ────────────────────────────────
        // CurrencyHudUI строит свой Canvas в рантайме; здесь лишь привязываем иконки Mini UI.
        var currencyHudGO = Child(root, "CurrencyHud");
        var currencyHud = currencyHudGO.AddComponent<CurrencyHudUI>();
        new W(currencyHud)
            .Ref("_gemIcon",   Spr("Assets/Mini UI/Icons/Blue Gem.png"))
            .Ref("_tokenIcon", Spr("Assets/Mini UI/Icons/Bronze Ticket.png"))
            .Ref("_keyIcon",   Spr("Assets/Mini UI/Icons/Golden Key.png"))
            .Apply();

        // ── Экран оформления (аватар + тема) + бейдж аватара на HUD (Батч 13) ──
        var custPanel = Panel("CustomizationPanel", ct, new Vector2(0.15f, 0.12f), new Vector2(0.85f, 0.88f), new Color(0.05f, 0.05f, 0.12f, 0.98f));
        var custClose = Btn("BtnCustomizationClose", custPanel.transform, "Закрыть");
        SetRect(custClose.GetComponent<RectTransform>(), new Vector2(0.35f, 0.035f), new Vector2(0.65f, 0.12f));
        ApplyPanelSprite(custPanel);
        custPanel.SetActive(false);

        // Бейдж аватара (левый верх HUD), тап открывает оформление; виден лишь после D4.
        var avatarBadgeGO = new GameObject("AvatarBadge", typeof(RectTransform), typeof(Image), typeof(Button));
        avatarBadgeGO.transform.SetParent(ct, false);
        SetRect((RectTransform)avatarBadgeGO.transform, new Vector2(0.012f, 0.875f), new Vector2(0.075f, 0.975f));
        var avatarImg = avatarBadgeGO.GetComponent<Image>();
        avatarImg.sprite = Spr("Assets/Mini UI/Avatars/Avatar 1.png");
        avatarImg.preserveAspect = true;
        var avatarBtn = avatarBadgeGO.GetComponent<Button>();
        avatarBtn.targetGraphic = avatarImg;
        avatarBadgeGO.AddComponent<ButtonClickSound>();

        // Спрайты: первые 8 аватаров + набор тем (DARK — дефолт, совпадает с базовой панелью).
        var avatarSprites = new Sprite[8];
        for (int i = 0; i < avatarSprites.Length; i++)
            avatarSprites[i] = Spr($"Assets/Mini UI/Avatars/Avatar {i + 1}.png");
        const string themeDir = "Assets/Mini UI/9 Splice Panels/Dark Theme RoundEdge Panels/Dark Theme RoundEdge ";
        var themeNames = new[] { "DARK", "BLUE", "GREEN", "PURPLE", "RED", "CYAN", "ORANGE", "PINK" };
        var themeSprites = new Sprite[themeNames.Length];
        for (int i = 0; i < themeNames.Length; i++)
            themeSprites[i] = Spr(themeDir + themeNames[i] + ".png");

        // На ВСЕГДА АКТИВНЫЙ Canvas (панель скрыта — иначе Awake не подпишет бейдж/не применит тему).
        var customization = canvasGO.AddComponent<CustomizationUI>();
        new W(customization)
            .Ref("_panel", custPanel)
            .Ref("_closeButton", custClose)
            .Ref("_avatarBadge", avatarImg)
            .Ref("_avatarBadgeButton", avatarBtn)
            .Arr("_avatarSprites", avatarSprites)
            .Arr("_themeSprites", themeSprites)
            .Apply();

        // ── Журнал гостей «Завсегдатаи» (Батч 6) ────────────────────────────
        // HUD-иконка «Журнал» (средняя в правом доке).
        var journalOpenBtn = IconBtn("BtnJournal", ct, "Assets/Mini UI/Icons/Book.png", "Журнал");
        SetRect(journalOpenBtn.GetComponent<RectTransform>(), new Vector2(0.905f, 0.76f), new Vector2(0.99f, 0.86f));
        Juice(journalOpenBtn, pulse: false);
        // Пункт 5: бейдж «новые гости» с числом непросмотренных записей + пульсация.
        var journalBadge = journalOpenBtn.gameObject.AddComponent<JournalBadge>();
        new W(journalBadge).Ref("_font", UiFont()).Apply();

        var journalPanel = Panel("GuestJournalPanel", ct, new Vector2(0.2f, 0.12f), new Vector2(0.8f, 0.88f), new Color(0.05f, 0.06f, 0.12f, 0.98f));
        Text("GuestJournalTitle", journalPanel.transform, "Журнал гостей · Завсегдатаи", 32, TextAlignmentOptions.Top, new Vector2(0.05f, 0.9f), new Vector2(0.95f, 0.99f));
        var journalProgress = Text("GuestJournalProgress", journalPanel.transform, "Знакомств: 0 / 0", 22, TextAlignmentOptions.Center, new Vector2(0.05f, 0.84f), new Vector2(0.95f, 0.9f));
        var journalContent  = MakeScrollView("GuestJournalScroll", journalPanel.transform, new Vector2(0.04f, 0.14f), new Vector2(0.96f, 0.83f));
        var journalCard     = MakeJournalCard(journalContent);
        var journalClose = Btn("BtnJournalClose", journalPanel.transform, "Закрыть");
        SetRect(journalClose.GetComponent<RectTransform>(), new Vector2(0.35f, 0.03f), new Vector2(0.65f, 0.12f));
        ApplyPanelSprite(journalPanel);
        journalPanel.SetActive(false);

        // GuestJournalUI вешаем на ВСЕГДА АКТИВНЫЙ Canvas (как SettingsUI), иначе его Awake
        // не выполнится (панель выключена) и кнопка «Журнал» не подпишется.
        var guestJournal = canvasGO.AddComponent<GuestJournalUI>();
        new W(guestJournal)
            .Ref("_panel", journalPanel)
            .Ref("_content", journalContent)
            .Ref("_cardTemplate", journalCard)
            .Ref("_progressText", journalProgress)
            .Ref("_btnOpen", journalOpenBtn)
            .Ref("_btnClose", journalClose)
            .Apply();

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
        dlg.sleepIcon      = sleepIcon.gameObject;
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
            .Ref("_journeyGate", journeyGate) // пункт 1
            .Ref("_dailyBonus", dailyBonus)   // Батч 2
            .Apply();

        // DayController
        new W(dayCtrl)
            .Ref("_stages", stages)
            .Ref("_customerController", custCtrl)
            .Ref("_craftingSystem", craft)
            .Ref("_dialogue", dlg)
            .Ref("_hintManager", hint)
            .Ref("_vfxController", vfx)
            .Ref("_dailyChallenge", dailyChallenge) // Батч 6
            .Arr("_customerPrefabs", stickmen)
            .Apply();

        // DailyChallenge (Батч 6): HUD-строка «Заказ дня»
        new W(dailyChallenge).Ref("_hudText", challengeHud).Apply();

        // ── Полировка панелей спрайтами Mini UI + ppuMultiplier=4 (пункты 4,5) ──
        ApplyPanelSprite(machinePanel);
        ApplyPanelSprite(hintPanel);
        ApplyPanelSprite(resultPanel);   // пункт 4: UI итогов дня
        ApplyPanelSprite(dialoguePanel);
        ApplyPanelSprite(dayIntroPanel);
        ApplyPanelSprite(messagePanel);

        // CustomerController — переиспользуем существующего ходячего гостя.
        // Анимацию/размер с него снимет и исходную модель удалит сам CustomerController (Awake).
        var existingGuest = FindSceneStickman();
        // Пункт 2: контроллер покоя для гостя, стоящего у стойки. Ходьбу оставляем
        // исходную (снимается с существующего гостя в CustomerController.Awake).
        var idleCtrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/PrefsAll/Hyper Casual Characters/Animator controller/idle.controller");
        // Пункт 1: эффекты-ауры над головой существ. Порядок: [0]искры,[1]дым,[2]пламя,[3]щит/вода,[4]портал.
        const string fxDir = "Assets/PrefsAll/FreeQuickEffectsVol1/Prefabs/";
        var auraPrefabs = new[]
        {
            AssetDatabase.LoadAssetAtPath<GameObject>(fxDir + "vfx_Sparks_01.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(fxDir + "vfx_Smoke_01.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(fxDir + "vfx_Flames_01.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(fxDir + "vfx_Shield_01.prefab"),
            AssetDatabase.LoadAssetAtPath<GameObject>(fxDir + "vfx_Portal_01.prefab"),
        };
        // Батч 1: эмоции гостя над головой. [0]грусть,[1]ок,[2]восторг (плейсхолдеры Mini UI).
        var emoteSprites = new[]
        {
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mini UI/UI Icons/Thumbsdown.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mini UI/UI Icons/Smiley.png"),
            AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Mini UI/UI Icons/ThumbsUp.png"),
        };
        new W(custCtrl)
            .Ref("_processVisitor", pv)
            .Ref("_visitorRoot", visitorRoot)
            .Ref("_satisfactionBarPrefab", sbComp)
            .Ref("_existingGuest", existingGuest)
            .Ref("_idleController", idleCtrl)   // покой, когда гость стоит
            .Arr("_creatureAuraPrefabs", auraPrefabs) // пункт 1: ауры существ
            .Arr("_emoteSprites", emoteSprites) // Батч 1: эмоции гостя
            .Apply();

        // MachineMinigame — UI вертикальных шкал
        new W(machine)
            .Ref("_panel", machinePanel)
            .Ref("_tempFill", tempFill)
            .Ref("_tempLabel", tempLabel)
            .Ref("_volumeFill", volFill)
            .Ref("_volumeLabel", volLabel)
            .Apply();

        // Главный герой (Female 1 Smooth Prefab) — не удаляем, добавляем idle (пункт 8)
        var hero = SetupHero();

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
            .Ref("_noMoneyPanel", noMoneyPanel)
            .Ref("_heroObject", hero)
            // Пункт 5: боковую верхне-правую кнопку-подсказку НЕ подключаем — она дублировала
            // иконку подсказки в правом доке. Подсказки остаются в панели подсказок, а
            // пульсирующая иконка «Подсказка» служит мягким призывом ими пользоваться.
            .Ref("_complimentButton", complimentBtn) // пункт 2: комплимент за монеты
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
            .Ref("_btnDouble", btnDouble)               // Батч 2: ×2 за рекламу
            .Ref("_doubleLabel", btnDoubleLabel)
            .Ref("_btnShop", btnShop)                   // Батч 3: магазин апгрейдов
            .Ref("_upgradeShop", upgradeShop)
            .Ref("_journeyProgressText", resJourneyProg) // Батч 6: трекер «Путь к 10 000»
            .Ref("_journeyForecastText", resJourneyFore)
            .Ref("_journeyFill", resJourneyFill)
            .Ref("_streakWarningText", resStreak)        // Батч 6: предупреждение о стрике
            .Ref("_btnSaveCombo", btnSaveCombo)          // Батч 6: сохранить комбо
            .Ref("_saveComboLabel", btnSaveComboLabel)
            .Apply();

        // AudioController
        // Банк звуков: создаём/наполняем ассет из пакета 50 клипов + фон-музыки.
        var soundBank = BuildSoundBank();

        var nightClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/night_ambience.mp3");
        new W(audio)
            .Ref("_musicSource", music)
            .Ref("_sfxSource", sfx)
            .Ref("_speechMixer", speech) // для превью громкости SFX (бубнёж)
            .Ref("_bank", soundBank)     // банк звуков (50 клипов)
            .Ref("_nightAmbience", nightClip) // ночная атмосфера сна (пункт 7)
            .Apply();

        // ── ВРЕМЕННО: кнопка сброса прогресса (для тестирования) ─────────────
        var resetBtn = Btn("BtnDebugReset", ct, "Сброс");
        SetRect(resetBtn.GetComponent<RectTransform>(), new Vector2(0.88f, 0.02f), new Vector2(0.99f, 0.08f));
        AddPersistentClick(resetBtn, gameMgr, "ResetProgressAndRestart");

        // ── Перф: облегчённые настройки качества под слабые устройства ───────
        Child(root, "PerformanceSetup").AddComponent<PerformanceSetup>();

        // ── ВСЕ кнопки/панели ВСЕЙ сцены → pixelsPerUnitMultiplier = 4 ──────
        //  Ищем по всей сцене (а не только по этому канвасу), чтобы одинаковую
        //  «толщину» рамок получили абсолютно все мини-кнопки и панели.
        foreach (var img in Object.FindObjectsOfType<Image>(true))
            if (img != null && img.type == Image.Type.Sliced)
                img.pixelsPerUnitMultiplier = 8f;

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

    // Создаёт/наполняет ассет SoundBank: все клипы пакета + фон-музыка + черновое
    // назначение событий по индексу. Пустые слоты заполняет, уже назначенные НЕ трогает.
    static SoundBank BuildSoundBank()
    {
        const string path = "Assets/SoundBank.asset";
        var bank = AssetDatabase.LoadAssetAtPath<SoundBank>(path);
        if (bank == null)
        {
            bank = ScriptableObject.CreateInstance<SoundBank>();
            AssetDatabase.CreateAsset(bank, path);
        }

        // Новый набор звуковых эффектов (средневеково-фэнтезийный) ПОЛНОСТЬЮ заменяет
        // прежний — старый пакет «Casual Game Sounds U6» больше не используется, ни один
        // старый звук в банке не остаётся. Новых клипов меньше, чем событий, поэтому
        // переиспользуем их по смыслу.
        AudioClip L(string file)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/sfx/" + file);
            if (c == null) Debug.LogWarning($"CoffeGameSetup: не найден звук Assets/Audio/sfx/{file}");
            return c;
        }
        var runeSeal = L("rune_seal.mp3");  // святая печать/руна — магия
        var crossbow = L("crossbow.mp3");   // натяжение тетивы
        var axe      = L("axe_slash.mp3");  // резкий удар
        var sword    = L("sword_draw.mp3"); // металлический выхват
        var horn     = L("horn.mp3");       // рог герольда
        var bell     = L("bell.mp3");       // колокол
        var fanfare  = L("fanfare.mp3");    // фанфары
        var sting    = L("sting.mp3");      // зловещий стинг

        bank.all = new[] { runeSeal, crossbow, axe, sword, horn, bell, fanfare, sting };

        // Перезаписываем ВСЕ события (без проверки на null) — старых звуков не остаётся.
        bank.click      = axe;       // клик — короткий резкий
        bank.uiOpen     = sword;     // открытие панели — выхват меча
        bank.uiClose    = crossbow;  // закрытие — натяжение
        bank.pour       = crossbow;  // налив ингредиента — натяжение
        bank.ding       = bell;      // подача напитка — колокол
        bank.perfect    = runeSeal;  // «Идеально» — магическая печать
        bank.star       = runeSeal;  // звезда/топпинг — магия
        bank.customerIn = horn;      // приход гостя — рог герольда
        bank.coin       = bell;      // монета — звон
        bank.combo      = fanfare;   // комбо — фанфары
        bank.bonus      = fanfare;   // бонус/награда — фанфары
        bank.correct    = runeSeal;  // верный заказ — магия
        bank.wrong      = axe;       // неверный заказ — резкий удар
        bank.dayClear   = fanfare;   // день завершён — фанфары
        bank.dayFail    = sting;     // рестарт дня — зловещий стинг

        // Фон-музыка не относится к звуковым эффектам — оставляем существующую.
        if (bank.music == null)
            bank.music = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/bg_music_celtic.mp3");

        EditorUtility.SetDirty(bank);
        AssetDatabase.SaveAssets();
        Debug.Log("CoffeGameSetup: SoundBank пересобран на новые средневековые звуки (8 клипов). " +
                  "Старые звуковые эффекты из банка полностью удалены.");
        return bank;
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
            it.displayName   = MagicIngredientName(child.name);   // RU
            it.displayNameEn = MagicIngredientNameEn(child.name); // EN
            RemoveWorldLabel(child.gameObject);               // убираем парящие подписи (пункт 2)
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

        int i = 0;
        foreach (Transform child in parent.transform)
        {
            EnsureCollider(child.gameObject);
            var it = child.GetComponent<IngredientItem>();
            if (it == null) it = child.gameObject.AddComponent<IngredientItem>();
            it.kind = IngredientItem.ItemKind.Topping;
            // Привязка enum к предмету ПО ИМЕНИ МОДЕЛИ (а не по порядку) — гость просит
            // конкретную еду, и она совпадает с конкретной моделью на полке.
            it.topping = ToppingByName(child.name);
            it.displayName = child.name;
            RemoveWorldLabel(child.gameObject);
            if (it.topping == Topping.None)
                Debug.LogWarning($"CoffeGameSetup: топпинг '{child.name}' не распознан — не будет кликабельным как топпинг.");
            i++;
        }
        Debug.Log($"CoffeGameSetup: топпингов (дети {parentName}): {i}");
    }

    // Топпинг по имени модели-предмета на полке (Food Pack). Единый источник привязки —
    // тот же ToppingUtil, что и рантайм IngredientItem (чтобы не расходились).
    static Topping ToppingByName(string objName) => ToppingUtil.FromObjectName(objName);

    // Убирает парящую подпись над объектом, если осталась от прошлой версии (пункт 2)
    static void RemoveWorldLabel(GameObject go)
    {
        var lbl = go.transform.Find("ItemLabel");
        if (lbl != null) Object.DestroyImmediate(lbl.gameObject);
    }

    // Магическое имя ингредиента по имени объекта (пункт 1)
    static string MagicIngredientName(string objName)
    {
        string n = objName.ToLower();
        if (n.Contains("goblet"))      return "Кубок забвения";
        if (n.Contains("inkwell"))     return "Чернильный отвар";
        if (n.Contains("drinkinghorn"))return "Рунный рог";
        if (n.Contains("jar_big") || n.Contains("jarbig")) return "Большой сосуд зорь";
        if (n.Contains("jar_full")|| n.Contains("jarfull"))return "Полная склянка лун";
        if (n.Contains("jar"))         return "Сосуд странствий";
        return objName; // запасной вариант — как назван объект
    }

    // Английское магическое имя ингредиента (перевод названий выше).
    static string MagicIngredientNameEn(string objName)
    {
        string n = objName.ToLower();
        if (n.Contains("goblet"))      return "Goblet of Oblivion";
        if (n.Contains("inkwell"))     return "Inkwell Brew";
        if (n.Contains("drinkinghorn"))return "Runic Horn";
        if (n.Contains("jar_big") || n.Contains("jarbig")) return "Great Vessel of Dawns";
        if (n.Contains("jar_full")|| n.Contains("jarfull"))return "Full Flask of Moons";
        if (n.Contains("jar"))         return "Vessel of Wanderings";
        return objName;
    }

    // Находит главного героя (Female 1 Smooth Prefab — ребёнок Main Camera),
    // ОТКРЕПЛЯЕТ от камеры (пункт 2) и вешает гуманоидный idle (пункт 1). НЕ удаляет.
    static GameObject SetupHero()
    {
        GameObject hero = GameObject.Find("Female 1 Smooth Prefab");

        // Запасной поиск: среди детей камеры (в т.ч. неактивных)
        if (hero == null)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                foreach (var tr in cam.GetComponentsInChildren<Transform>(true))
                {
                    if (tr == cam.transform) continue;
                    string n = tr.name.ToLower();
                    if (n.Contains("female") || n.Contains("smooth prefab"))
                    {
                        hero = tr.gameObject;
                        break;
                    }
                }
            }
        }

        if (hero == null)
        {
            Debug.LogWarning("CoffeGameSetup: главный герой не найден под Main Camera — " +
                             "назначь CoffeeCraftingSystem._heroObject вручную.");
            return null;
        }

        // Пункт 2: открепляем от камеры и СТАВИМ НА ПОЛ за стойкой, чтобы герой
        // не «летал» вместе с камерой (раньше он был ребёнком Main Camera и ездил
        // с ней во время обучения и на этапах игры).
        hero.transform.SetParent(null, true);
        PlaceHeroBehindCounter(hero);

        // Пункт 1: гуманоидная анимация Standing Idle с ретаргетом на риг героя
        bool animOk = SetupHeroAnimation(hero);
        var hi = hero.GetComponent<HeroIdle>();
        if (animOk)
        {
            if (hi != null) Object.DestroyImmediate(hi); // настоящий клип заменяет процедурный
        }
        else
        {
            if (hi == null) hero.AddComponent<HeroIdle>(); // запасной процедурный idle
            Debug.LogWarning("CoffeGameSetup: не удалось настроить гуманоидную анимацию героя — " +
                             "оставил процедурный HeroIdle. Проверь Rig=Humanoid у Standing Idle.fbx и Female 1.fbx.");
        }

        Debug.Log($"CoffeGameSetup: главный герой '{hero.name}' откреплён от камеры, idle={(animOk ? "humanoid" : "procedural")}.");
        return hero;
    }

    // Ставит героя на фиксированное место за стойкой и опускает на пол (пункт 2).
    // Приоритет позиции:
    //   1) пустышка-маркер "HeroPoint"/"HeroStand"/"HeroAnchor" — берём её позицию и поворот
    //      (создай такой объект там, где должна стоять хозяйка — это точная ручная настройка);
    //   2) иначе — точка кассира (PointCashier) по X/Z + рейкаст вниз до пола;
    //   3) герой разворачивается лицом к гостю (VisitorBasis).
    static void PlaceHeroBehindCounter(GameObject hero)
    {
        if (hero == null) return;

        var marker = FindMarker("HeroPoint", "HeroStand", "HeroAnchor");
        if (marker != null)
        {
            hero.transform.SetPositionAndRotation(marker.position, marker.rotation);
            Debug.Log("CoffeGameSetup: герой поставлен по маркеру " + marker.name + ".");
            return;
        }

        // База — точка кассира за стойкой (X/Z), иначе оставляем текущие X/Z.
        var cashier = FindMarker("PointCashier", "PointCashierForDialog");
        Vector3 pos = hero.transform.position;
        if (cashier != null) { pos.x = cashier.position.x; pos.z = cashier.position.z; }

        // Опускаем на пол рейкастом сверху вниз (чтобы герой не висел в воздухе).
        // Игнорируем собственные коллайдеры героя, чтобы не «приземлиться» на его макушку.
        Vector3 from = pos + Vector3.up * 50f;
        var hits = Physics.RaycastAll(from, Vector3.down, 200f);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (var h in hits)
        {
            if (h.collider != null && h.collider.transform.IsChildOf(hero.transform)) continue;
            pos.y = h.point.y;
            break;
        }
        hero.transform.position = pos;

        // Разворот лицом к гостю.
        var guest = FindMarker("VisitorBasis");
        if (guest != null)
        {
            Vector3 dir = guest.position - hero.transform.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.0001f)
                hero.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
        }

        Debug.Log("CoffeGameSetup: герой поставлен у стойки и опущен на пол. " +
                  "Для точной ручной настройки создай в сцене пустой объект 'HeroPoint' там, где должна стоять хозяйка — сборка подхватит его позицию.");
    }

    // Настраивает Female + Standing Idle как Humanoid, строит контроллер и вешает Animator.
    static bool SetupHeroAnimation(GameObject hero)
    {
        // 1. Female FBX → Humanoid + аватар из этой модели
        string femalePath = AssetDatabase.GUIDToAssetPath("32e26f88fac2c504fa382ed43968e1f9");
        var femaleImp = AssetImporter.GetAtPath(femalePath) as ModelImporter;
        if (femaleImp == null) return false;
        if (femaleImp.animationType != ModelImporterAnimationType.Human ||
            femaleImp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            femaleImp.animationType = ModelImporterAnimationType.Human;
            femaleImp.avatarSetup   = ModelImporterAvatarSetup.CreateFromThisModel;
            femaleImp.SaveAndReimport();
        }
        Avatar femaleAvatar = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(femalePath))
            if (a is Avatar av) femaleAvatar = av;

        // 2. Standing Idle FBX → Humanoid (аватар из этой модели)
        const string idlePath = "Assets/Standing Idle.fbx";
        var idleImp = AssetImporter.GetAtPath(idlePath) as ModelImporter;
        if (idleImp == null) return false;
        if (idleImp.animationType != ModelImporterAnimationType.Human ||
            idleImp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel)
        {
            idleImp.animationType = ModelImporterAnimationType.Human;
            idleImp.avatarSetup   = ModelImporterAvatarSetup.CreateFromThisModel;
            idleImp.SaveAndReimport();
        }
        AnimationClip idleClip = null;
        foreach (var a in AssetDatabase.LoadAllAssetsAtPath(idlePath))
            if (a is AnimationClip c && !c.name.StartsWith("__preview")) idleClip = c;

        if (femaleAvatar == null || idleClip == null) return false;

        // 3. Контроллер с idle-клипом
        const string ctrlPath = "Assets/HeroIdleController.controller";
        var ctrl = AnimatorController.CreateAnimatorControllerAtPathWithClip(ctrlPath, idleClip);
        if (ctrl == null) return false;

        // 4. Animator на герое: контроллер + аватар
        var anim = hero.GetComponent<Animator>();
        if (anim == null) anim = hero.AddComponent<Animator>();
        anim.runtimeAnimatorController = ctrl;
        anim.avatar = femaleAvatar;
        anim.applyRootMotion = false;
        return true;
    }

    // Единый нормальный шрифт для всех UI-текстов (добавлен в Assets пользователем).
    static TMP_FontAsset _uiFont;
    static TMP_FontAsset UiFont()
    {
        if (_uiFont == null)
            _uiFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ofont.ru_Nunito SDF.asset");
        if (_uiFont == null)
            Debug.LogWarning("CoffeGameSetup: шрифт 'Assets/ofont.ru_Nunito SDF.asset' не найден — тексты останутся на стандартном шрифте.");
        return _uiFont;
    }

    // Применяет красивый 9-slice спрайт Mini UI к панели.
    // Пункт 5: pixelsPerUnitMultiplier = 4 для всех UI-панелей.
    static Sprite _panelSprite;
    static void ApplyPanelSprite(GameObject panel)
    {
        if (_panelSprite == null)
            _panelSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Mini UI/9 Splice Panels/Dark Theme RoundEdge Panels/Dark Theme RoundEdge DARK.png");
        if (_panelSprite == null) return;
        var img = panel.GetComponent<Image>();
        if (img != null)
        {
            img.sprite = _panelSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.pixelsPerUnitMultiplier = 8f; // пункт 5
        }
        // Батч 13: помечаем панель темизируемой — сменит спрайт под выбранную тему (UiTheme).
        if (panel.GetComponent<ThemedPanel>() == null) panel.AddComponent<ThemedPanel>();
    }

    // Применяет спрайт-кнопку Mini UI (пункт 4) с pixelsPerUnitMultiplier = 4 (пункт 5).
    static Sprite _buttonSprite;
    static void ApplyButtonSprite(Image img)
    {
        if (img == null) return;
        if (_buttonSprite == null)
            _buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Mini UI/Buttons/Dark Theme Border Buttons/256Px Rectangle DarkBorder/Dark Long Btn DARK.png");
        if (_buttonSprite != null)
        {
            img.sprite = _buttonSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.pixelsPerUnitMultiplier = 8f; // пункт 5
        }
        else
        {
            img.color = new Color(0.2f, 0.2f, 0.28f, 0.95f); // запасной вид, если спрайт не найден
        }
    }

    // Иконка-картинка из ассета на Canvas
    // Загрузка спрайта по пути (Батч 13: иконки валют, аватары, темы).
    static Sprite Spr(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static Image IconImage(string name, Transform parent, string assetPath, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, aMin, aMax);
        var img = go.GetComponent<Image>();
        var sp = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sp != null) img.sprite = sp;
        img.preserveAspect = true;
        img.raycastTarget = false;
        return img;
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

    // Настраивает кружку (PlayerCup): реальная модель Coffee Cup C + CupController + якоря зон.
    static CupController SetupCup(string cupName, Transform ingA, Transform macA, Transform topA, Transform countA)
    {
        var cup = GameObject.Find(cupName);
        if (cup == null)
        {
            cup = new GameObject(cupName);
            Debug.LogWarning("CoffeGameSetup: PlayerCup не найден — создан новый объект-носитель кружки.");
        }
        cup.transform.SetParent(null, true);   // кружка живёт в мире, не под камерой
        cup.transform.localScale = Vector3.one; // масштаб задаём модели-визуалу ниже

        // Пункт 3: реальная кружка из ассетов вместо цилиндра.
        // Снимаем старый примитив-меш с самого PlayerCup и старый визуал.
        var oldMf = cup.GetComponent<MeshFilter>();   if (oldMf != null) Object.DestroyImmediate(oldMf);
        var oldMr = cup.GetComponent<MeshRenderer>(); if (oldMr != null) Object.DestroyImmediate(oldMr);
        var oldCol = cup.GetComponent<Collider>();    if (oldCol != null) Object.DestroyImmediate(oldCol);
        var oldVisual = cup.transform.Find("CupVisual");
        if (oldVisual != null) Object.DestroyImmediate(oldVisual.gameObject);

        float cupHeight = 0.16f; // целевая высота кружки в мире
        var cupPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/PrefsAll/Food Pack-Demo/Prefabs/Coffee Cup C.prefab");
        if (cupPrefab != null)
        {
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(cupPrefab);
            visual.name = "CupVisual";
            visual.transform.SetParent(cup.transform, false);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;

            // Авто-масштаб по габаритам, чтобы кружка была нужного размера независимо
            // от исходного масштаба модели.
            var rends = visual.GetComponentsInChildren<Renderer>();
            if (rends.Length > 0)
            {
                var b = rends[0].bounds;
                for (int i = 1; i < rends.Length; i++) b.Encapsulate(rends[i].bounds);
                float h = Mathf.Max(0.0001f, b.size.y);
                float k = cupHeight / h;
                visual.transform.localScale *= k;
                cupHeight = h * k;
            }
        }
        else
        {
            Debug.LogWarning("CoffeGameSetup: 'Coffee Cup C.prefab' не найден — оставил пустой PlayerCup.");
        }

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
        content.localPosition = new Vector3(0f, cupHeight * 0.6f, 0f); // у кромки кружки

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

    // Горизонтальная шкала-заполнение (Image: Filled, Horizontal). Батч 6.
    static Image HorizontalFill(string name, Transform parent, Vector2 aMin, Vector2 aMax, Color color)
    {
        var bg = new GameObject(name + "_BG", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(parent, false);
        SetRect((RectTransform)bg.transform, aMin, aMax);
        bg.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.5f);

        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(bg.transform, false);
        SetRect((RectTransform)go.transform, Vector2.zero, Vector2.one);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.sprite = WhiteSprite();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 0f;
        img.raycastTarget = false;
        return img;
    }

    // Заполняющий слой (Filled, Horizontal) на весь родитель — для наложенных шкал
    // «сейчас/станет» в магазине апгрейдов (Батч 11). Родитель служит фоном.
    static Image RowFill(string name, Transform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, Vector2.zero, Vector2.one);
        var img = go.GetComponent<Image>();
        img.color = color;
        img.sprite = WhiteSprite();
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
        img.fillAmount = 0f;
        img.raycastTarget = false;
        return img;
    }

    // Прокручиваемый список (ScrollRect + Viewport(Mask) + Content с VerticalLayout).
    // Возвращает Transform контейнера Content (куда класть карточки). Батч 6.
    static Transform MakeScrollView(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var root = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        root.transform.SetParent(parent, false);
        SetRect((RectTransform)root.transform, aMin, aMax);
        root.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

        var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
        viewport.transform.SetParent(root.transform, false);
        SetRect((RectTransform)viewport.transform, Vector2.zero, Vector2.one);
        viewport.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.01f);
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        var content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var crt = (RectTransform)content.transform;
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.offsetMin = Vector2.zero;
        crt.offsetMax = Vector2.zero;
        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = 8;
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth  = true;
        vlg.childControlHeight = true;
        vlg.childControlWidth  = true;
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        var sr = root.GetComponent<ScrollRect>();
        sr.viewport = (RectTransform)viewport.transform;
        sr.content  = crt;
        sr.horizontal = false;
        sr.vertical   = true;
        sr.movementType = ScrollRect.MovementType.Clamped;
        sr.scrollSensitivity = 20f;
        return content.transform;
    }

    // Шаблон-карточка гостя для журнала (JournalCard + поля). Батч 6.
    static JournalCard MakeJournalCard(Transform content)
    {
        var card = Panel("JournalCardTemplate", content, Vector2.zero, Vector2.one, new Color(1f, 1f, 1f, 0.05f));
        var le = card.AddComponent<LayoutElement>();
        le.preferredHeight = 92f;
        le.minHeight = 92f;

        var nameText   = Text("Name",   card.transform, "Гость",  24, TextAlignmentOptions.TopLeft,     new Vector2(0.03f, 0.55f), new Vector2(0.62f, 0.97f));
        var statusText = Text("Status", card.transform, "Статус", 18, TextAlignmentOptions.TopRight,    new Vector2(0.62f, 0.55f), new Vector2(0.97f, 0.97f));
        statusText.color = new Color(0.8f, 0.9f, 1f);
        var symFill    = HorizontalFill("SympathyFill", card.transform, new Vector2(0.03f, 0.30f), new Vector2(0.80f, 0.50f), new Color(0.3f, 0.85f, 0.4f));
        var symText    = Text("SympathyPct", card.transform, "50%", 18, TextAlignmentOptions.Right,     new Vector2(0.81f, 0.28f), new Vector2(0.97f, 0.52f));
        var visitsText = Text("Visits", card.transform, "Визитов: 0", 16, TextAlignmentOptions.BottomLeft, new Vector2(0.03f, 0.02f), new Vector2(0.55f, 0.28f));
        var starsText  = Text("Stars",  card.transform, "", 20, TextAlignmentOptions.BottomRight,   new Vector2(0.55f, 0.02f), new Vector2(0.97f, 0.28f));
        starsText.color = new Color(1f, 0.85f, 0.3f);

        var jc = card.AddComponent<JournalCard>();
        new W(jc)
            .Ref("_nameText", nameText)
            .Ref("_statusText", statusText)
            .Ref("_visitsText", visitsText)
            .Ref("_starsText", starsText)
            .Ref("_sympathyFill", symFill)
            .Ref("_sympathyText", symText)
            .Apply();
        return jc;
    }

    // Функциональный ползунок UnityEngine.UI.Slider (фон + заполнение + ручка), 0..1.
    static Slider MakeSlider(string name, Transform parent, Vector2 aMin, Vector2 aMax, float value)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Slider));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, aMin, aMax);
        var slider = go.GetComponent<Slider>();

        // Фон (тёмная дорожка)
        var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
        bg.transform.SetParent(go.transform, false);
        SetRect((RectTransform)bg.transform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));
        var bgImg = bg.GetComponent<Image>(); bgImg.color = new Color(0f, 0f, 0f, 0.5f); bgImg.sprite = WhiteSprite();

        // Зона заполнения → Заполнение (зелёное)
        var fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(go.transform, false);
        SetRect((RectTransform)fillArea.transform, new Vector2(0f, 0.3f), new Vector2(1f, 0.7f));
        var fill = new GameObject("Fill", typeof(RectTransform), typeof(Image));
        fill.transform.SetParent(fillArea.transform, false);
        SetRect((RectTransform)fill.transform, Vector2.zero, Vector2.one);
        var fillImg = fill.GetComponent<Image>(); fillImg.color = new Color(0.3f, 0.8f, 0.4f); fillImg.sprite = WhiteSprite();

        // Зона ручки → Ручка (белый кружок)
        var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
        handleArea.transform.SetParent(go.transform, false);
        SetRect((RectTransform)handleArea.transform, Vector2.zero, Vector2.one);
        var handle = new GameObject("Handle", typeof(RectTransform), typeof(Image));
        handle.transform.SetParent(handleArea.transform, false);
        var handleRt = (RectTransform)handle.transform;
        handleRt.sizeDelta = new Vector2(28, 0);
        var handleImg = handle.GetComponent<Image>(); handleImg.color = Color.white; handleImg.sprite = WhiteSprite();

        slider.fillRect      = (RectTransform)fill.transform;
        slider.handleRect    = handleRt;
        slider.targetGraphic = handleImg;
        slider.direction     = Slider.Direction.LeftToRight;
        slider.minValue = 0f; slider.maxValue = 1f;
        slider.value    = value;
        return slider;
    }

    // Legacy UnityEngine.UI.Text (нужен компоненту LeaderboardYG в простом режиме).
    static UnityEngine.UI.Text LegacyText(string name, Transform parent, Vector2 aMin, Vector2 aMax)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(UnityEngine.UI.Text));
        go.transform.SetParent(parent, false);
        SetRect((RectTransform)go.transform, aMin, aMax);
        var t = go.GetComponent<UnityEngine.UI.Text>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null) font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        t.font = font;
        t.color = Color.white;
        t.alignment = TextAnchor.UpperLeft;
        t.fontSize = 26;
        t.lineSpacing = 1.1f;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
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
        var uiFont = UiFont();
        if (uiFont != null) t.font = uiFont; // единый нормальный шрифт во всех UI
        // Пункт 4: текст всегда аккуратно вписывается в свою область и не липнет к краям.
        t.fontSize = size;
        t.fontSizeMax = size;
        t.fontSizeMin = Mathf.Max(10f, size * 0.5f);
        t.enableAutoSizing = true;
        t.alignment = align;
        t.color = Color.white;
        t.enableWordWrapping = true;
        t.margin = new Vector4(12, 6, 12, 6); // отступы от краёв
        t.raycastTarget = false; // Перф: надписи не нужны как цели рейкаста (клики ловят кнопки)
        SetRect(t.rectTransform, aMin, aMax);
        TryLocalize(t.gameObject, content); // авто-локализация статических подписей (ru→en)
        return t;
    }

    static Button Btn(string name, Transform parent, string label)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        ApplyButtonSprite(img); // пункты 4,5: спрайт-кнопка + ppuMultiplier
        ((RectTransform)go.transform).sizeDelta = new Vector2(150, 48);
        var txt = Text("Label", go.transform, label, 20, TextAlignmentOptions.Center);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        go.AddComponent<ButtonClickSound>(); // клик-звук всем кнопкам
        return btn;
    }

    // Кнопка «иконка сверху + мини-подпись снизу» (для HUD). Подпись локализуется через Text().
    static Button IconBtn(string name, Transform parent, string iconPath, string ru)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        ApplyButtonSprite(img);
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;

        var icon = IconImage("Icon", go.transform, iconPath, new Vector2(0.24f, 0.34f), new Vector2(0.76f, 0.95f));
        icon.color = Color.white; // цветные иконки — без тонировки
        Text("Caption", go.transform, ru, 15, TextAlignmentOptions.Center, new Vector2(0.02f, 0.04f), new Vector2(0.98f, 0.34f));
        go.AddComponent<ButtonClickSound>(); // клик-звук
        return btn;
    }

    // Навешивает лёгкую анимацию (сочность) на кнопку.
    static void Juice(Button b, bool pulse = true, bool shine = false, bool wobble = false)
    {
        if (b == null) return;
        b.gameObject.AddComponent<ButtonJuice>().Configure(pulse, shine, wobble);
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

    // ─── Сборка сцены сна (пункт 4) ──────────────────────────────────────────────
    // Вешает SleepSceneController на сцену со спящим ГГ (эффекты/текст/UI контроллер
    // строит сам в рантайме), назначает ночной звук и шрифт, добавляет обе сцены в
    // Build Settings (нужно для перехода MainScene ↔ Sleepy scene по имени).
    [MenuItem("Tools/CoffeGame/Build Sleep Scene")]
    public static void BuildSleepScene()
    {
        const string scenePath = "Assets/Scenes/Sleepy scene.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath) == null)
        {
            Debug.LogError($"CoffeGameSetup: не найдена сцена {scenePath}.");
            return;
        }

        string prevPath = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        var go = GameObject.Find("SleepController");
        if (go == null) go = new GameObject("SleepController");
        var ctrl = go.GetComponent<SleepSceneController>();
        if (ctrl == null) ctrl = go.AddComponent<SleepSceneController>();

        var nightClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/night_ambience.mp3");
        var font      = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/ofont.ru_Nunito SDF.asset");
        if (nightClip == null) Debug.LogWarning("CoffeGameSetup: не найден Assets/Audio/night_ambience.mp3 — сон будет без звука.");
        if (font == null)      Debug.LogWarning("CoffeGameSetup: не найден шрифт Nunito SDF — текст сна на стандартном шрифте.");

        var so = new SerializedObject(ctrl);
        if (nightClip != null) so.FindProperty("_nightClip").objectReferenceValue = nightClip;
        if (font != null)      so.FindProperty("_font").objectReferenceValue      = font;
        var mainProp = so.FindProperty("_mainSceneName");
        if (mainProp != null) mainProp.stringValue = "MainScene";
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        RegisterScenesInBuildSettings();

        Debug.Log("CoffeGameSetup: сцена сна собрана (SleepController + ночной звук + шрифт), сцены добавлены в Build Settings.");

        if (!string.IsNullOrEmpty(prevPath) && prevPath != scenePath)
            EditorSceneManager.OpenScene(prevPath, OpenSceneMode.Single);
    }

    // Регистрирует MainScene и сцену сна в Build Settings (для LoadScene по имени).
    static void RegisterScenesInBuildSettings()
    {
        string[] wanted =
        {
            "Assets/Scenes/MainScene.unity",
            "Assets/Scenes/Sleepy scene.unity",
        };
        var list = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool changed = false;
        foreach (var path in wanted)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                Debug.LogWarning($"CoffeGameSetup: сцена не найдена в Build Settings: {path}");
                continue;
            }
            if (!list.Exists(s => s.path == path))
            {
                list.Add(new EditorBuildSettingsScene(path, true));
                changed = true;
            }
        }
        if (changed) EditorBuildSettings.scenes = list.ToArray();
    }
}
#endif
