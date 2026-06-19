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
        // Адаптив под ПК и горизонтальный телефон, без деформации (требования 1.6/1.10).
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        var ct = canvasGO.transform;

        // ── Диалоговая панель (низ экрана) ──────────────────────────────────
        var dialoguePanel = Panel("DialoguePanel", ct, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.27f), new Color(0.05f, 0.05f, 0.1f, 0.85f));
        var speakerName   = Text("SpeakerName", dialoguePanel.transform, "Имя", 30, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.7f), new Vector2(0.6f, 0.98f));
        var dialogueText  = Text("DialogueText", dialoguePanel.transform, "Текст реплики...", 28, TextAlignmentOptions.TopLeft, new Vector2(0.02f, 0.08f), new Vector2(0.98f, 0.68f));
        dialogueText.enableAutoSizing = false; // печатная машинка: фикс. размер, без «прыжков»
        var continueHint  = Text("ContinueHint", dialoguePanel.transform, "▼ далее", 22, TextAlignmentOptions.BottomRight, new Vector2(0.6f, 0.0f), new Vector2(0.98f, 0.18f)).gameObject;

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

        // ── Ачивка («Отлично!»/«В точку!») сверху ───────────────────────────
        var achievement = Text("AchievementText", ct, "В точку!", 48, TextAlignmentOptions.Center, new Vector2(0.25f, 0.72f), new Vector2(0.75f, 0.79f));
        achievement.color = new Color(0.4f, 1f, 0.5f);
        achievement.gameObject.SetActive(false);

        // ── Кнопка «Подать» (низ по центру; пункт 10) ───────────────────────
        var serveBtn = Btn("BtnServe", ct, "Подать ☕");
        SetRect(serveBtn.GetComponent<RectTransform>(), new Vector2(0.4f, 0.04f), new Vector2(0.6f, 0.12f));
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
        var confirmBtn = Btn("BtnConfirm", ct, "Подтвердить ✓");
        SetRect(confirmBtn.GetComponent<RectTransform>(), new Vector2(0.4f, 0.05f), new Vector2(0.6f, 0.13f));
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
        var resEnd      = Text("DayEndText", resultPanel.transform, "Итоги дня...", 22, TextAlignmentOptions.Top, new Vector2(0.05f, 0.28f), new Vector2(0.95f, 0.48f));
        var btnContinue = Btn("BtnContinue", resultPanel.transform, "Продолжить");
        SetRect(btnContinue.GetComponent<RectTransform>(), new Vector2(0.08f, 0.03f), new Vector2(0.47f, 0.15f));
        // Батч 2: «Удвоить заработок — реклама» (rewarded)
        var btnDouble = Btn("BtnDoubleEarnings", resultPanel.transform, "Удвоить — реклама");
        SetRect(btnDouble.GetComponent<RectTransform>(), new Vector2(0.53f, 0.03f), new Vector2(0.92f, 0.15f));
        var btnDoubleLabel = btnDouble.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        btnDouble.gameObject.SetActive(false);
        // Батч 3: «Улучшить кофейню» — открывает магазин апгрейдов прямо с итогов дня.
        var btnShop = Btn("BtnUpgradeShop", resultPanel.transform, "Улучшить кофейню");
        SetRect(btnShop.GetComponent<RectTransform>(), new Vector2(0.25f, 0.165f), new Vector2(0.75f, 0.265f));

        // ── Магазин апгрейдов кофейни (Батч 3) ──────────────────────────────
        var shopPanel = Panel("UpgradeShopPanel", ct, new Vector2(0.2f, 0.14f), new Vector2(0.8f, 0.86f), new Color(0.05f, 0.05f, 0.12f, 0.97f));
        Text("UpgradeShopTitle", shopPanel.transform, "Кофейня · улучшения", 34, TextAlignmentOptions.Top, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f));
        var shopTitles = new TextMeshProUGUI[3];
        var shopInfos  = new TextMeshProUGUI[3];
        var shopBuys   = new Button[3];
        var shopBuyLbl = new TextMeshProUGUI[3];
        for (int i = 0; i < 3; i++)
        {
            float top = 0.84f - i * 0.235f;
            float bot = top - 0.205f;
            var row = Panel($"UpgradeRow{i}", shopPanel.transform, new Vector2(0.05f, bot), new Vector2(0.95f, top), new Color(1f, 1f, 1f, 0.04f));
            shopTitles[i] = Text($"UpgTitle{i}", row.transform, "—", 26, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.52f), new Vector2(0.63f, 0.96f));
            shopInfos[i]  = Text($"UpgInfo{i}",  row.transform, "—", 19, TextAlignmentOptions.TopLeft, new Vector2(0.03f, 0.04f), new Vector2(0.63f, 0.52f));
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

        // ── Ежедневный бонус (Батч 2) ───────────────────────────────────────
        var bonusPanel = Panel("DailyBonusPanel", ct, new Vector2(0.3f, 0.3f), new Vector2(0.7f, 0.7f), new Color(0.05f, 0.05f, 0.12f, 0.97f));
        var bonusGift  = IconImage("DailyBonusIcon", bonusPanel.transform, "Assets/Mini UI/Icons/Gift.png",
            new Vector2(0.4f, 0.62f), new Vector2(0.6f, 0.9f));
        var bonusTitle = Text("DailyBonusTitle", bonusPanel.transform, "Бонус за вход", 30, TextAlignmentOptions.Center, new Vector2(0.05f, 0.5f), new Vector2(0.95f, 0.62f));
        var bonusReward= Text("DailyBonusReward", bonusPanel.transform, "+50 монет", 40, TextAlignmentOptions.Center, new Vector2(0.05f, 0.38f), new Vector2(0.95f, 0.5f));
        var btnClaim   = Btn("BtnBonusClaim", bonusPanel.transform, "Забрать");
        SetRect(btnClaim.GetComponent<RectTransform>(), new Vector2(0.15f, 0.22f), new Vector2(0.85f, 0.34f));
        var btnClaimLabel = btnClaim.GetComponentInChildren<TMPro.TextMeshProUGUI>();
        var btnBonusDouble = Btn("BtnBonusDouble", bonusPanel.transform, "Удвоить — реклама");
        SetRect(btnBonusDouble.GetComponent<RectTransform>(), new Vector2(0.15f, 0.07f), new Vector2(0.85f, 0.19f));
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
        var settingsBtn = Btn("BtnSettings", ct, "Меню");
        SetRect(settingsBtn.GetComponent<RectTransform>(), new Vector2(0.915f, 0.9f), new Vector2(0.99f, 0.975f));

        var settingsPanel = Panel("SettingsPanel", ct, new Vector2(0.3f, 0.22f), new Vector2(0.7f, 0.82f), new Color(0.05f, 0.05f, 0.12f, 0.97f));
        Text("SettingsTitle", settingsPanel.transform, "Настройки", 34, TextAlignmentOptions.Top, new Vector2(0.05f, 0.88f), new Vector2(0.95f, 0.99f));
        Text("MusicLabel", settingsPanel.transform, "Музыка", 24, TextAlignmentOptions.Left, new Vector2(0.08f, 0.75f), new Vector2(0.5f, 0.83f));
        var musicSlider = MakeSlider("MusicSlider", settingsPanel.transform, new Vector2(0.08f, 0.66f), new Vector2(0.92f, 0.74f), 0.4f);
        Text("SfxLabel", settingsPanel.transform, "Звуки", 24, TextAlignmentOptions.Left, new Vector2(0.08f, 0.57f), new Vector2(0.5f, 0.65f));
        var sfxSlider = MakeSlider("SfxSlider", settingsPanel.transform, new Vector2(0.08f, 0.48f), new Vector2(0.92f, 0.56f), 0.8f);
        var fullscreenBtn = Btn("BtnFullscreen", settingsPanel.transform, "Полный экран: выкл");
        SetRect(fullscreenBtn.GetComponent<RectTransform>(), new Vector2(0.1f, 0.355f), new Vector2(0.9f, 0.45f));
        var fullscreenLabel = fullscreenBtn.GetComponentInChildren<TextMeshProUGUI>();
        var leaderboardBtn = Btn("BtnLeaderboard", settingsPanel.transform, "Таблица лидеров");
        SetRect(leaderboardBtn.GetComponent<RectTransform>(), new Vector2(0.1f, 0.235f), new Vector2(0.9f, 0.33f));
        var settingsClose = Btn("BtnSettingsClose", settingsPanel.transform, "Закрыть");
        SetRect(settingsClose.GetComponent<RectTransform>(), new Vector2(0.3f, 0.06f), new Vector2(0.7f, 0.16f));
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

        // SettingsUI вешаем на ВСЕГДА АКТИВНЫЙ Canvas (иначе кнопка «Меню» не подпишется,
        // пока панель скрыта). Панели он включает/выключает сам.
        var settings = canvasGO.AddComponent<SettingsUI>();
        new W(settings)
            .Ref("_panel", settingsPanel)
            .Ref("_openButton", settingsBtn)
            .Ref("_closeButton", settingsClose)
            .Ref("_musicSlider", musicSlider)
            .Ref("_sfxSlider", sfxSlider)
            .Ref("_fullscreenButton", fullscreenBtn)
            .Ref("_fullscreenLabel", fullscreenLabel)
            .Ref("_leaderboardButton", leaderboardBtn)
            .Ref("_leaderboardPanel", lbPanel)
            .Ref("_leaderboardCloseButton", lbClose)
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
            .Arr("_customerPrefabs", stickmen)
            .Apply();

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
            .Ref("_adHintButton", adHintBtn)
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
            it.displayName = MagicIngredientName(child.name); // имя из объекта (пункт 1)
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
            it.displayName = child.name;   // имя из объекта (пункт 1)
            RemoveWorldLabel(child.gameObject);
            i++;
        }
        Debug.Log($"CoffeGameSetup: топпингов (дети {parentName}): {i}");
    }

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
            img.pixelsPerUnitMultiplier = 4f; // пункт 5
        }
    }

    // Применяет спрайт-кнопку Mini UI (пункт 4) с pixelsPerUnitMultiplier = 4 (пункт 5).
    static Sprite _buttonSprite;
    static void ApplyButtonSprite(Image img)
    {
        if (img == null) return;
        if (_buttonSprite == null)
            _buttonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(
                "Assets/Mini UI/Buttons/Dark Theme Border Buttons/192Px Round DarkBorder/Small Round Button DARK.png");
        if (_buttonSprite != null)
        {
            img.sprite = _buttonSprite;
            img.type = Image.Type.Sliced;
            img.color = Color.white;
            img.pixelsPerUnitMultiplier = 4f; // пункт 5
        }
        else
        {
            img.color = new Color(0.2f, 0.2f, 0.28f, 0.95f); // запасной вид, если спрайт не найден
        }
    }

    // Иконка-картинка из ассета на Canvas
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
        SetRect(t.rectTransform, aMin, aMax);
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
