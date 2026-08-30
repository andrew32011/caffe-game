/// <summary>
/// Главный менеджер игры. Singleton. Управляет глобальным состоянием:
/// цикл из 20 дней, сохранения, YG2. Фазами внутри дня управляет DayController
/// через существующую машину этапов Stages.
/// Сцена: MainScene (единственный экземпляр)
/// Зависимости: StoryDatabase, DayController, DialogueDisplayer, Stages, VisualEffectsController
/// SDK: YG2 (GameplayStart/Stop, GameReady, фокус окна)
/// </summary>
using System.Collections;
using UnityEngine;
using YG;

public class GameManager : MonoBehaviour
{
    // ─── Singleton ───────────────────────────────────────────────────────────

    public static GameManager Instance { get; private set; }

    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("База данных сюжета")]
    [SerializeField] private StoryDatabase _storyDatabase;

    [Header("Ссылки на системы")]
    [SerializeField] private DayController       _dayController;
    [SerializeField] private DialogueDisplayer   _dialogue;
    [SerializeField] private TutorialController  _tutorialController;
    [SerializeField] private VisualEffectsController _vfxController;
    [SerializeField] private DayResultUI         _dayResultUI;
    [SerializeField] private AudioController     _audioController;
    [SerializeField] private HintManager         _hintManager;

    [Header("Машина этапов (существующая, объект StagesScripts)")]
    [SerializeField] private Stages _stages;

    [Header("Гейт путешествия (пункт 1): не хватило денег — начать заново / купить монеты")]
    [SerializeField] private JourneyGateUI _journeyGate;

    [Header("Ежедневный бонус (Батч 2)")]
    [SerializeField] private DailyBonusUI _dailyBonus;

    [Header("Свет для «сна» (приглушается между днями; можно не задавать — найдётся сам)")]
    [SerializeField] private Light _sceneLight;

    [Header("Сохранение")]
    [SerializeField] private int _startDay = 1; // 0 — показывать туториал
    [Tooltip("Всегда показывать обучение при старте (игнорируя сохранение). ТОЛЬКО для разработки — в релизе false.")]
    [SerializeField] private bool _forceTutorial = false;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private GamePhase _currentPhase = GamePhase.Tutorial;
    // Прогресс хранится в облачном сейве Яндекс Игр (YG2.saves). Поля — в SavesYG.Game.cs.
    private SavesYG _saveData => YG2.saves;

    // Разовые на СЕССИЮ. Статики переживают SceneManager.LoadScene (домен не перезагружается),
    // а сцена сна перезагружает MainScene каждый день — поэтому обучение, ежедневный бонус и
    // GameReadyAPI выполняем только на ПЕРВОМ входе в сессию, а не на каждом дне.
    private static bool _sessionReadyCalled;
    private static bool _sessionInitDone;

    // Имя сцены сна (полная смена сцены между днями).
    private const string SleepSceneName = "Sleepy scene";
    private static bool SleepSceneAvailable()
        => Application.CanStreamedLevelBeLoaded(SleepSceneName);

    // ─── Публичные свойства ───────────────────────────────────────────────────

    public GamePhase    CurrentPhase  => _currentPhase;
    public int          CurrentDay    => _saveData.currentDay;
    public int          TotalCoins    => _saveData.totalCoins;
    public StoryDatabase StoryDB      => _storyDatabase;
    public DialogueDisplayer Dialogue => _dialogue;
    public Stages       StageFlow     => _stages;

    // ─── Awake / Start ────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Звук останавливается при сворачивании окна (требование 1.3) и при показе
        // любой рекламы (требование 4.7).
        YG2.onFocusWindowGame += OnWindowFocusChanged;
        YG2.onOpenAnyAdv      += OnAdOpened;
        YG2.onCloseAnyAdv     += OnAdClosed;

        // Батч 12-B: покупки кристаллов (сосуществует с обработчиками no_ads/coins_pack,
        // каждый игнорирует чужие товары).
        YG2.onPurchaseSuccess += OnGemPurchase;

        // Этапами управляет DayController — отключаем автозапуск этапа 0,
        // иначе гость пойдёт к стойке до начала дня
        if (_stages != null)
            _stages.autoStartStageZero = false;
    }

    private void Start()
    {
        StartCoroutine(StartGameFlow());
    }

    private void OnDestroy()
    {
        YG2.onFocusWindowGame -= OnWindowFocusChanged;
        YG2.onOpenAnyAdv      -= OnAdOpened;
        YG2.onCloseAnyAdv     -= OnAdClosed;
        YG2.onPurchaseSuccess -= OnGemPurchase;
    }

    // Пауза звука и геймплея на время полноэкранной рекламы (требование 4.7).
    private void OnAdOpened()
    {
        _audioController?.SetMuted(true);
        Time.timeScale = 0f;
    }

    private void OnAdClosed()
    {
        Time.timeScale = 1f;
        _audioController?.SetMuted(false);
    }

    // ─── Основной поток игры ──────────────────────────────────────────────────

    private IEnumerator StartGameFlow()
    {
        GameInput.Locked = true;             // на старте управление выключено

        // Требование 1.9/2.6: ждём инициализацию SDK и загрузку сейвов (облако/локально),
        // только потом читаем прогресс. Обновление страницы не теряет данные.
        yield return new WaitUntil(() => YG2.isSDKEnabled);
        LoadGame();
        CurrencyHudUI.Ensure(); // Батч 12-B: HUD кристаллов/жетонов/ключей (по разблокировкам)
        CustomizationUI.Instance?.ApplySaved(); // Батч 13: применить сохранённые аватар/тему
        CustomizationUI.Instance?.RefreshBadgeVisibility();
        TryGrantOfflineIncome(); // Батч 12-E: «кофейня работала, пока тебя не было» (крючок возврата)
        _audioController?.ApplySavedVolumes(_saveData.musicVolume, _saveData.sfxVolume, _saveData.voiceVolume); // Батч 4 + голоса
        if (!_sessionReadyCalled)            // 1.19.2: игрок может начинать (один раз за сессию)
        {
            YG2.GameReadyAPI();
            _sessionReadyCalled = true;
        }
        yield return new WaitForSeconds(0.2f); // Ждём инициализации всех систем

        // Обучение и ежедневный бонус — только на ПЕРВОМ входе в сессию. При возврате из
        // сцены сна MainScene перезагружается, но повторять их не нужно — сразу к дню.
        if (!_sessionInitDone)
        {
            Analytics.Send(Analytics.SessionStart); // метрика: старт игровой сессии

            if (_forceTutorial || !_saveData.tutorialDone)
            {
                yield return StartCoroutine(RunTutorial());

                // Затемнение между обучением и первым днём (пункт 5)
                yield return StartCoroutine(Transition());
            }

            // Батч 2: ежедневный бонус за вход (если сегодня ещё не получали).
            if (_dailyBonus != null)
                yield return StartCoroutine(_dailyBonus.RunIfDue());

            _sessionInitDone = true;
        }

        yield return StartCoroutine(RunGameDays());
    }

    // ─── Туториал ────────────────────────────────────────────────────────────

    private IEnumerator RunTutorial()
    {
        _currentPhase = GamePhase.Tutorial;
        Analytics.Send(Analytics.TutorialStart); // метрика: начало обучения (ранний отток)

        if (_tutorialController != null)
        {
            // Туториал сам управляет замком: на объяснении — выкл, на практике — вкл
            yield return StartCoroutine(_tutorialController.RunTutorial());
        }

        GameInput.Locked = true; // после обучения управление выключаем (пункт 5)
        Analytics.Send(Analytics.TutorialDone); // метрика: обучение пройдено
        _saveData.tutorialDone = true;
        _saveData.currentDay   = 1;
        SaveGame();
    }

    // ─── «Сон» между днями (пункт 8) ───────────────────────────────────────

    private static readonly string[][] DreamLines =
    {
        new[] { "Это всего лишь сон... всего лишь сон.", "It's only a dream... only a dream." },
        new[] { "Кай зовёт меня сквозь туман: «Найди меня...»", "Kai calls through the fog: \"Find me...\"" },
        new[] { "Три круга горят в темноте. Они смотрят.", "Three circles burn in the dark. They are watching." },
        new[] { "На столе — письмо, пахнущее пеплом. «Я ещё жив.»", "A letter on the table, smelling of ash. \"I am still alive.\"" },
        new[] { "Стены дышат. Я просыпаюсь — но не уверена, что проснулась.", "The walls breathe. I wake — but I'm not sure I'm awake." },
    };

    private IEnumerator PlayDreamVignette(int day)
    {
        GameInput.Locked = true;

        // ГГ виден во сне (пункт 2), как сновидец
        CoffeeCraftingSystem.Instance?.SetHeroVisible(true);

        // Приглушаем свет почти до тьмы (пункт 3 — должно быть ясно, что это сон)
        if (_sceneLight == null) _sceneLight = FindObjectOfType<Light>();
        float origIntensity = _sceneLight != null ? _sceneLight.intensity : 0f;
        if (_sceneLight != null)
            yield return StartCoroutine(FadeLight(_sceneLight, origIntensity, origIntensity * 0.1f, 0.5f));

        // 1) Показываем титр-заставку «Сон» с иконкой на потемневшей сцене —
        //    аналогично «ДЕНЬ N» в начале дня (пункт 1). Сначала титр, потом эффект.
        //    (Титр НЕ под чёрным оверлеем — иначе его не было бы видно; затемнение
        //    дальше сделает сам PlayVignette.)
        // Пункт 7: ночная атмосфера («шёпот призраков») — включается вместе с началом
        // диалога сна и выключается на пробуждении, ДО показа рекламы на переходе.
        _audioController?.PlayNight();

        if (_dialogue != null)
            yield return StartCoroutine(_dialogue.ShowTitleCardRoutine(Loc.T("СОН", "DREAM"), true, 2.2f));

        // 2) Кошмар: эффект + текст (PlayVignette сам затемняет вход и открывает сцену).
        // Пункт 4: разнообразим сны — берём эффект из перетасованного «мешка» без
        // повторов подряд (красный RedPulse не используем — сон мрачный, не «алый»).
        var effect = NextDreamEffect();
        string text = Loc.IsRu ? DreamLines[day % DreamLines.Length][0]
                               : DreamLines[day % DreamLines.Length][1];

        yield return StartCoroutine(_vfxController.PlayVignette(effect, text, _dialogue));

        // Возвращаем свет (просыпаемся)
        if (_sceneLight != null)
            yield return StartCoroutine(FadeLight(_sceneLight, _sceneLight.intensity, origIntensity, 0.6f));

        CoffeeCraftingSystem.Instance?.SetHeroVisible(false);

        // Проснулись — ночной звук выключаем (до рекламы на переходе).
        _audioController?.StopNight();
    }

    // Пул эффектов сна (без красного) + «мешок» для выдачи без повторов подряд.
    private static readonly VignetteEffectType[] DreamEffects =
    {
        VignetteEffectType.CameraShake,
        VignetteEffectType.VisionLoss,
        VignetteEffectType.DarknessFlash,
        VignetteEffectType.BrightRestore,
        VignetteEffectType.WhiteFlash,
        VignetteEffectType.SlowVeil
    };
    private readonly System.Collections.Generic.List<VignetteEffectType> _dreamBag
        = new System.Collections.Generic.List<VignetteEffectType>();
    private VignetteEffectType _lastDreamEffect = VignetteEffectType.None;

    private VignetteEffectType NextDreamEffect()
    {
        if (_dreamBag.Count == 0)
        {
            _dreamBag.AddRange(DreamEffects);
            // перетасовка
            for (int i = _dreamBag.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                var tmp = _dreamBag[i]; _dreamBag[i] = _dreamBag[j]; _dreamBag[j] = tmp;
            }
            // избегаем повтора того же эффекта на стыке мешков
            if (_dreamBag[0] == _lastDreamEffect && _dreamBag.Count > 1)
            {
                _dreamBag[0] = _dreamBag[1];
                _dreamBag[1] = _lastDreamEffect;
            }
        }
        _lastDreamEffect = _dreamBag[0];
        _dreamBag.RemoveAt(0);
        return _lastDreamEffect;
    }

    private IEnumerator FadeLight(Light light, float from, float to, float dur)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, dur);
            if (light != null) light.intensity = Mathf.Lerp(from, to, t);
            yield return null;
        }
        if (light != null) light.intensity = to;
    }

    /// <summary>Затемнить экран → реклама → высветлить. Управление выключено на время.
    /// Пункт 4: вызывается на КАЖДОМ переходе — между днями и обучение→день 1.</summary>
    private IEnumerator Transition()
    {
        GameInput.Locked = true;
        if (_vfxController != null)
            yield return StartCoroutine(_vfxController.FadeScreen(true, 0.6f));
        else
            yield return new WaitForSeconds(0.3f);

        // Всплывающая (межстраничная) реклама на переходе.
        yield return StartCoroutine(ShowInterstitial());

        if (_vfxController != null)
            yield return StartCoroutine(_vfxController.FadeScreen(false, 0.6f));
        else
            yield return new WaitForSeconds(0.3f);
    }

    /// <summary>Показ межстраничной рекламы и ожидание её закрытия (если модуль установлен).</summary>
    private IEnumerator ShowInterstitial()
    {
        // Куплено отключение рекламы (YG2 Payments, навсегда) — не показываем.
        if (_saveData != null && _saveData.adsDisabled) yield break;
#if InterstitialAdv_yg
        Analytics.Send(Analytics.AdInterstitial); // метрика: показ межстраничной (отток после рекламы)
        YG2.InterstitialAdvShow();
        yield return new WaitUntil(() => !YG2.nowInterAdv);
#else
        yield return null;
#endif
    }

    // ─── Цикл дней ───────────────────────────────────────────────────────────

    private IEnumerator RunGameDays()
    {
        while (_saveData.currentDay <= 40)
        {
            int day = _saveData.currentDay;

            // После всплывающей рекламы на переходе к дню (в т.ч. из сцены сна) — короткое
            // ненавязчивое напоминание, что рекламу можно убрать за донат и поддержать
            // автора. Не показываем на самом первом дне и если реклама уже отключена.
            // Батч 13: на дне 3 (кристаллы уже открыты) вместо напоминания — таймовый оффер
            // «Стартовый набор» с отсчётом (один раз за сессию); в остальные дни — напоминание.
            if (day > 1 && !_saveData.adsDisabled)
            {
                if (day == 3 && ProgressionManager.IsUnlocked(ProgressionManager.Feature.Gems))
                    TimedOfferUI.Ensure().ShowOffer();
                else
                    AdRemovalPrompt.Instance?.ShowAfterAd();
            }

            // Пункт 1: перед финальным днём проверяем «цель путешествия» (10000 монет).
            // Не хватило — даём выбор: начать заново с 1-го дня (копить) или купить монеты.
            if (day >= 40 && _journeyGate != null && _saveData.totalCoins < CoinsUI.JourneyGoal)
            {
                GameInput.Locked = true;
                Analytics.Send(Analytics.JourneyGate); // метрика: дошёл до гейта цели, но не накопил
                yield return StartCoroutine(_journeyGate.Run(CoinsUI.JourneyGoal));
                if (_journeyGate.RestartChosen)
                {
                    _saveData.currentDay = 1;
                    SaveGame();
                    yield return StartCoroutine(Transition());
                    continue; // начинаем сначала, монеты сохранены
                }
                // иначе монеты докуплены до цели — продолжаем в финал
            }

            DayData dayData = _storyDatabase?.GetDay(day);

            if (dayData == null)
            {
                Debug.LogWarning($"GameManager: данные для дня {day} не найдены.");
                _saveData.currentDay++;
                continue;
            }

            // ─── Запускаем день ─────────────────────────────────────────────
            _currentPhase = GamePhase.Day;
            YG2.GameplayStart();
            Analytics.DayStarted(day, endless: false); // метрика: разбивка оттока по дню сюжета

            bool dayCompleted = false;
            while (!dayCompleted)
            {
                GameInput.Locked = false; // на время рабочего дня управление включено
                yield return StartCoroutine(_dayController.RunDay(dayData));
                GameInput.Locked = true;  // после дня — выключаем (пункт 5)
                dayCompleted = _dayController.DaySuccess;

                if (!dayCompleted)
                {
                    // Рестарт дня — пауза, потом снова
                    Analytics.DayFail(day); // метрика: провал дня (частая причина фрустрации/оттока)
                    AudioController.Instance?.PlayDayFail();
                    _dialogue.ShowMessage(
                        Loc.T("День не засчитан. Начинаем заново...",
                              "The day doesn't count. Starting over..."), 2f);
                    yield return new WaitForSeconds(2.5f);
                }
            }

            YG2.GameplayStop();
            Analytics.DayCompleted(day, _dayController.CoinsEarnedToday); // метрика: день пройден
            Achievements.CheckAll(); // Батч 12-D: майлстоуны → кристаллы

            // ─── Экран результатов дня ──────────────────────────────────────
            // Деньги меняются живьём в процессе дня (списание себестоимости +
            // оплата клиента), поэтому здесь totalCoins НЕ трогаем — иначе двойной учёт.
            _currentPhase = GamePhase.DayResult;

            // 2D-баннер конца дня (пункт 5)
            AudioController.Instance?.PlayDayClear();
            UiEffects.Instance?.DayEndBanner(Loc.T($"День {day} завершён", $"Day {day} complete"));

            if (_dayResultUI != null)
            {
                _dayResultUI.Show(day, _dayController.CoinsEarnedToday, dayData.GetDayEndText(), _dayController.CurrentComboCount);
                yield return new WaitUntil(() => !_dayResultUI.IsShowing);
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            // Промпты вовлечения: после дня 1 — ярлык на рабочий стол, после дня 3 — оценка игры.
            yield return StartCoroutine(MaybeShowEngagementPrompt(day));

            // Ночь: видения/эффекты — фоновая музыка замолкает (возобновится на новом дне).
            _audioController?.PauseMusic();

            // ─── Вставная сцена (визуальный эффект) ─────────────────────────
            if (dayData.hasVignette && _vfxController != null)
            {
                _currentPhase = GamePhase.StoryVignette;
                yield return StartCoroutine(_vfxController.PlayVignette(
                    dayData.vignetteEffect,
                    dayData.GetVignetteText(),
                    _dialogue));
            }

            // Батч 6: тизер «Особого гостя» накануне (перед сном, ещё в MainScene).
            if (day < 40 && DayController.IsSpecialDay(day + 1) && _dialogue != null)
            {
                _dialogue.ShowMessage(
                    Loc.T("Завтра придёт кто-то особенный…", "Someone special is coming tomorrow…"), 2.5f);
                yield return new WaitForSeconds(2f);
            }

            // Батч 12-F: тизер разблокировки завтра (Зейгарник — причина вернуться).
            string nextUnlock = ProgressionManager.NextUnlockName(day + 1);
            if (day < 40 && nextUnlock != null && _dialogue != null)
            {
                _dialogue.ShowMessage(
                    Loc.T($"Завтра откроется: {nextUnlock}!", $"Unlocks tomorrow: {nextUnlock}!"), 2.5f);
                yield return new WaitForSeconds(2f);
            }

            // ─── Переход к следующему дню ────────────────────────────────────
            _saveData.currentDay++;
            SaveGame();
            SubmitLeaderboard(); // Батч 4: обновляем место в таблице по монетам

            if (_saveData.currentDay > 40)
                break;

            // ─── «Сон» между днями теперь в ОТДЕЛЬНОЙ сцене (Sleepy scene) ───
            // Порядок (пункт 4): итоги+магазин здесь → сцена сна (эффекты+текст+ночной
            // звук) → реклама (в сцене сна) → возврат в MainScene на новый день.
            // MainScene перезагрузится и продолжит игру с сохранённого currentDay.
            if (day < 40 && SleepSceneAvailable())
            {
                _saveData.sleepFromDay = day;   // для текста/эффекта сна
                SaveGame();
                if (_vfxController != null)
                    yield return StartCoroutine(_vfxController.FadeScreen(true, 0.6f));
                UnityEngine.SceneManagement.SceneManager.LoadScene(SleepSceneName);
                yield break; // управление уходит в сцену сна
            }

            // Запасной путь, если сцена сна не добавлена в Build Settings: старый оверлей
            // сна прямо в MainScene + реклама на переходе (порядок: сон → реклама → музыка).
            if (day < 40 && _vfxController != null)
            {
                _currentPhase = GamePhase.StoryVignette;
                yield return StartCoroutine(PlayDreamVignette(day));
            }
            yield return StartCoroutine(Transition());
            _audioController?.ResumeMusic();
        }

        // ─── Сюжет пройден ──────────────────────────────────────────────────────
        // При первом завершении показываем финал и включаем Бесконечный режим.
        // При возврате (перезагрузка / сцена сна) endlessMode уже true — сразу к игре.
        if (!_saveData.endlessMode)
        {
            yield return StartCoroutine(ShowGameComplete()); // финал (ждёт клик игрока)

            _saveData.endlessMode = true;
            _saveData.endlessDay  = 1;
            SaveGame();
            Analytics.Send(Analytics.EndlessStart); // метрика: игрок дошёл до финала и включил endless

            if (_dialogue != null)
            {
                _dialogue.ShowMessage(
                    Loc.T("Кофейня «Междумирье» остаётся открытой. Дальше — Бесконечный режим: держись как можно дольше и ставь рекорды!",
                          "The Inbetween stays open. From here on it's Endless mode — last as long as you can and set records!"),
                    0f); // ждёт клик
                yield return new WaitForSeconds(1f); // дать сообщению появиться
                yield return StartCoroutine(WaitForClick());
                yield return new WaitForSeconds(0.8f); // дать сообщению угаснуть
            }
        }

        yield return StartCoroutine(RunEndlessDays());
    }

    // ─── Бесконечный режим (после дня 40) ──────────────────────────────────────

    /// <summary>Цикл бесконечных дней: процедурные гости, максимальная сложность,
    /// рекорд по «самому дальнему дню». Переиспользует поток DayController и переходы.</summary>
    private IEnumerator RunEndlessDays()
    {
        while (true)
        {
            int eDay = _saveData.endlessDay;
            if (eDay < 1) { eDay = 1; _saveData.endlessDay = 1; }

            // После рекламы предыдущего перехода — ненавязчивое «убрать рекламу».
            if (eDay > 1 && !_saveData.adsDisabled)
                AdRemovalPrompt.Instance?.ShowAfterAd();

            DayData dayData = EndlessMode.BuildDay(eDay);

            _currentPhase = GamePhase.Day;
            YG2.GameplayStart();
            Analytics.DayStarted(eDay, endless: true); // метрика: как далеко заходят в endless

            bool dayCompleted = false;
            while (!dayCompleted)
            {
                GameInput.Locked = false;
                yield return StartCoroutine(_dayController.RunDay(dayData));
                GameInput.Locked = true;
                dayCompleted = _dayController.DaySuccess;

                if (!dayCompleted)
                {
                    AudioController.Instance?.PlayDayFail();
                    _dialogue.ShowMessage(
                        Loc.T("День не засчитан. Начинаем заново...",
                              "The day doesn't count. Starting over..."), 2f);
                    yield return new WaitForSeconds(2.5f);
                }
            }

            YG2.GameplayStop();

            // Рекорд бесконечного режима (самый дальний достигнутый день).
            if (eDay > _saveData.endlessBestDay) _saveData.endlessBestDay = eDay;
            Achievements.CheckAll(); // Батч 12-D: майлстоуны endless → кристаллы

            // ─── Экран результатов (деньги уже начислены в процессе дня) ─────────
            _currentPhase = GamePhase.DayResult;
            AudioController.Instance?.PlayDayClear();
            UiEffects.Instance?.DayEndBanner(
                Loc.T($"Бесконечный день {eDay}", $"Endless day {eDay}"));

            if (_dayResultUI != null)
            {
                _dayResultUI.Show(EndlessMode.DisplayDayNumber(eDay), _dayController.CoinsEarnedToday,
                                  Loc.T("Ещё один день в «Междумирье». Кофейня не спит.",
                                        "Another day at the Inbetween. The café never sleeps."),
                                  _dayController.CurrentComboCount,
                                  Loc.T($"БЕСКОНЕЧНЫЙ ДЕНЬ {eDay}", $"ENDLESS DAY {eDay}"), // Батч 11: плашка вместо «День 41…»
                                  _saveData.endlessBestDay);
                yield return new WaitUntil(() => !_dayResultUI.IsShowing);
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            _audioController?.PauseMusic();

            // Следующий бесконечный день.
            _saveData.endlessDay = eDay + 1;
            SaveGame();
            SubmitLeaderboard();          // монеты-лидерборд (растёт с игрой)
            SubmitEndlessLeaderboard();   // рекорд бесконечных дней

            // Переход к следующему дню: сцена сна (если в Build Settings) ИЛИ реклама-затемнение.
            if (SleepSceneAvailable())
            {
                _saveData.sleepFromDay = EndlessMode.DisplayDayNumber(eDay); // для текста/эффекта сна
                SaveGame();
                if (_vfxController != null)
                    yield return StartCoroutine(_vfxController.FadeScreen(true, 0.6f));
                UnityEngine.SceneManagement.SceneManager.LoadScene(SleepSceneName);
                yield break; // управление уходит в сцену сна, после неё MainScene продолжит endless
            }

            yield return StartCoroutine(Transition());
            _audioController?.ResumeMusic();
        }
    }

    /// <summary>Ждёт клика/тапа игрока (для подтверждения экранных сообщений).</summary>
    private IEnumerator WaitForClick()
    {
        bool clicked = false;
        while (!clicked)
        {
            if (Input.GetMouseButtonDown(0)) clicked = true;
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) clicked = true;
            yield return null;
        }
    }

    /// <summary>Промпты вовлечения после дня (каждый — один раз, локализация «плашками»):
    /// день 1 → ярлык на рабочий стол; день 3 → оценка игры. Промпт строится в рантайме
    /// (EngagementPrompt), сборка сцены билдером не нужна.</summary>
    private IEnumerator MaybeShowEngagementPrompt(int day)
    {
        // День 1 — предложить добавить ярлык (только если платформа реально умеет ярлык).
        if (day == 1 && !_saveData.shortcutAsked)
        {
            _saveData.shortcutAsked = true;
            SaveGame();
            if (YaShortcut.Available())
            {
                var p = EngagementPrompt.Ensure();
                Analytics.Prompt("shortcut", accepted: false);
                p.Show("Добавить игру на рабочий стол, чтобы вернуться?", "Добавить", "Не сейчас", () =>
                {
                    Analytics.Prompt("shortcut", accepted: true);
                    YaShortcut.Prompt();
                });
                yield return new WaitUntil(() => !p.IsShowing);
            }
        }

#if Review_yg
        // День 3 — предложить оценить игру (только с установленным модулем Review,
        // иначе кнопка «Оценить» была бы мёртвой — так её просто нет).
        if (day == 3 && !_saveData.reviewAsked)
        {
            _saveData.reviewAsked = true;
            SaveGame();
            var p = EngagementPrompt.Ensure();
            Analytics.Prompt("review", accepted: false);
            p.Show("Нравится игра? Оцени её — это очень поможет!", "Оценить", "Позже", () =>
            {
                Analytics.Prompt("review", accepted: true);
                YG2.ReviewShow();
            });
            yield return new WaitUntil(() => !p.IsShowing);
        }
#endif
        yield break;
    }

    // ─── Финал игры ───────────────────────────────────────────────────────────

    private IEnumerator ShowGameComplete()
    {
        _currentPhase = GamePhase.GameComplete;

        // Светлый финал: Кай спасён, кофейня живёт, дом снова полон. Монеты не трогаем.
        UiEffects.Instance?.Celebrate(3);
        AudioController.Instance?.PlayDayClear();

        if (_dialogue != null)
        {
            _dialogue.ShowMessage(
                Loc.T("Граница запечатана. Кай дома — и каждое утро снова варит кофе рядом с тобой.\n\n«Междумирье» больше не край миров, а просто тёплое место, куда хочется вернуться.\n\nСПАСИБО ЗА ИГРУ!",
                      "The border is sealed. Kai is home — and every morning he brews coffee by your side again.\n\nThe Inbetween is no longer the edge of worlds, just a warm place worth coming back to.\n\nTHANK YOU FOR PLAYING!"),
                0f // 0 = не автоматически, ждёт клика
            );
        }

        // Ждём, пока игрок подтвердит финал кликом (иначе анонс Бесконечного режима
        // проскочил бы поверх концовки).
        yield return new WaitForSeconds(1f); // дать финалу появиться
        yield return StartCoroutine(WaitForClick());
        yield return new WaitForSeconds(0.8f); // дать финальному тексту угаснуть
    }

    // ─── Монеты ───────────────────────────────────────────────────────────────

    public void AddCoins(int amount)
    {
        _saveData.totalCoins += amount;
    }

    // ─── Батч 12-B: тройная экономика (кристаллы, жетоны, ключи) ────────────────
    // ⚠️ Товары кристаллов завести в консоли Яндекс Игр с этими ID.
    public const string GemsSmallId   = "gems_small";   // consumable
    public const string GemsMediumId  = "gems_medium";  // consumable
    public const string GemsLargeId   = "gems_large";   // consumable
    public const string StarterPackId = "starter_pack"; // non-consumable, разовый

    public int Gems   => _saveData.gems;
    public int Tokens => _saveData.tokens;
    public int Keys   => _saveData.keys;

    public void AddGems(int n)    { _saveData.gems   = Mathf.Max(0, _saveData.gems   + n); SaveGame(); CurrencyHudUI.Instance?.Refresh(); }
    public bool SpendGems(int n)  { if (_saveData.gems   < n) return false; _saveData.gems   -= n; SaveGame(); CurrencyHudUI.Instance?.Refresh(); return true; }
    public void AddTokens(int n)  { _saveData.tokens = Mathf.Max(0, _saveData.tokens + n); SaveGame(); CurrencyHudUI.Instance?.Refresh(); }
    public bool SpendTokens(int n){ if (_saveData.tokens < n) return false; _saveData.tokens -= n; SaveGame(); CurrencyHudUI.Instance?.Refresh(); return true; }
    public void AddKeys(int n)    { _saveData.keys   = Mathf.Max(0, _saveData.keys   + n); SaveGame(); CurrencyHudUI.Instance?.Refresh(); }
    public bool SpendKeys(int n)  { if (_saveData.keys   < n) return false; _saveData.keys   -= n; SaveGame(); CurrencyHudUI.Instance?.Refresh(); return true; }

    // ─── Батч 13: кастомизация (аватар/тема) ────────────────────────────────────
    public int AvatarId => _saveData.avatarId;
    public int ThemeId  => _saveData.themeId;
    public void SetAvatar(int id) { _saveData.avatarId = Mathf.Max(0, id); SaveGame(); }
    public void SetTheme(int id)  { _saveData.themeId  = Mathf.Max(0, id); SaveGame(); }

    /// <summary>Запустить покупку кристаллов (UI-кнопки магазина зовут это).</summary>
    public void BuyGems(string productId)
    {
        Analytics.Send("purchase_start", "product", productId);
        YG2.BuyPayments(productId);
    }

    // Начисление кристаллов по факту покупки. Чужие товары (no_ads/coins_pack) игнорируем —
    // их обрабатывают AdRemovalPrompt / JourneyGateUI.
    private void OnGemPurchase(string id)
    {
        int grantGems = 0;
        bool consumable = true;
        switch (id)
        {
            case GemsSmallId:   grantGems = 50;  break;
            case GemsMediumId:  grantGems = 170; break;
            case GemsLargeId:   grantGems = 500; break;
            case StarterPackId: grantGems = 100; AddCoins(2000); _saveData.adsDisabled = true; consumable = false; break;
            default: return; // не наш товар
        }

        _saveData.gems += grantGems;
        if (consumable) YG2.ConsumePurchaseByID(id); // расходник — иначе не купить повторно
        SaveGame();
        CurrencyHudUI.Instance?.Refresh();
        Analytics.Bought(id);
        AudioController.Instance?.PlayCoin();
        RewardPopupUI.Ensure().Show(
            Loc.T("Спасибо за покупку!", "Thank you!"),
            Loc.T($"+{grantGems} кристаллов", $"+{grantGems} gems"),
            new Color(0.35f, 0.7f, 0.95f), 3.5f);
    }

    // ─── Батч 6: перенос комбо на следующий день (rewarded «Сохранить комбо») ──

    private int _carriedCombo = 0; // серия, перенесённая с прошлого дня (runtime; сгорает при перезагрузке между днями)

    /// <summary>Комбо, перенесённое с прошлого дня (0 — нет). Читается DayController в начале дня.</summary>
    public int CarriedCombo => _carriedCombo;

    /// <summary>Задать перенос комбо (rewarded на экране результата) или сбросить (0).</summary>
    public void SetCarriedCombo(int c) => _carriedCombo = Mathf.Max(0, c);

    // ─── Батч 2: продолжение посреди дня ───────────────────────────────────────

    /// <summary>С какого гостя продолжать текущий день (0 — с начала).</summary>
    public int ResumeCustomerIndex => _saveData.currentCustomerIndex;

    /// <summary>Сохранить прогресс внутри дня (после каждого гостя).</summary>
    public void SetCustomerIndex(int index)
    {
        _saveData.currentCustomerIndex = index;
        SaveGame();
    }

    // ─── Батч 2: ежедневный бонус (доступ к сейву для DailyBonusUI) ─────────────

    public string DailyBonusLastDate
    {
        get => _saveData.dailyBonusLastDate;
        set => _saveData.dailyBonusLastDate = value;
    }
    public int DailyBonusStreak
    {
        get => _saveData.dailyBonusStreak;
        set => _saveData.dailyBonusStreak = value;
    }

    // ─── Батч 3: апгрейды кофейни ──────────────────────────────────────────────

    public const int UpgradeMaxLevel = 3;

    /// <summary>Текущий уровень апгрейда (0..UpgradeMaxLevel).</summary>
    public int GetUpgradeLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.Beans:   return _saveData.upgBeans;
            case UpgradeType.Machine: return _saveData.upgMachine;
            default:                  return _saveData.upgLoyalty;
        }
    }

    /// <summary>Цена следующего уровня апгрейда; -1, если уже максимум.</summary>
    public int GetUpgradeCost(UpgradeType type)
    {
        int level = GetUpgradeLevel(type);
        if (level >= UpgradeMaxLevel) return -1;
        // База за тип × (уровень+1): первое улучшение дешевле, следующие дороже.
        int baseCost = type == UpgradeType.Beans   ? 300
                     : type == UpgradeType.Machine ? 250
                     :                                200;
        return baseCost * (level + 1);
    }

    /// <summary>Покупает следующий уровень апгрейда, если хватает монет. Сохраняет сразу.</summary>
    public bool TryBuyUpgrade(UpgradeType type)
    {
        int cost = GetUpgradeCost(type);
        if (cost < 0 || _saveData.totalCoins < cost) return false;

        _saveData.totalCoins -= cost;
        switch (type)
        {
            case UpgradeType.Beans:   _saveData.upgBeans++;   break;
            case UpgradeType.Machine: _saveData.upgMachine++; break;
            default:                  _saveData.upgLoyalty++; break;
        }
        SaveGame();
        return true;
    }

    // Множители эффектов апгрейдов (читают DayController / CoffeeCraftingSystem).
    public float PriceMultiplier => 1f + GetUpgradeLevel(UpgradeType.Beans)   * 0.12f; // +12%/ур к оплате
    public float ToleranceBonus  =>       GetUpgradeLevel(UpgradeType.Machine) * 0.04f; // шире допуск
    public float MoodBonus       =>       GetUpgradeLevel(UpgradeType.Loyalty) * 0.06f; // лучше чаевые

    // ─── Батч 4: настройки громкости (хранятся в облачном сейве) ────────────────

    public float SavedMusicVolume => _saveData.musicVolume;
    public float SavedSfxVolume   => _saveData.sfxVolume;
    public float SavedVoiceVolume => _saveData.voiceVolume;

    /// <summary>Записывает громкость в сейв и сохраняет (вызывает AudioController при изменении).</summary>
    public void SetVolumes(float music, float sfx, float voice)
    {
        _saveData.musicVolume = Mathf.Clamp01(music);
        _saveData.sfxVolume   = Mathf.Clamp01(sfx);
        _saveData.voiceVolume = Mathf.Clamp01(voice);
        SaveGame();
    }

    // ─── Батч 4: лидерборд по монетам ──────────────────────────────────────────

    /// <summary>Техническое имя таблицы лидеров в консоли Яндекс Игр (счёт = всего монет).</summary>
    public const string LeaderboardName = "coins";

    /// <summary>Таблица лидеров Бесконечного режима (счёт = самый дальний достигнутый день).
    /// Создать одноимённую таблицу в консоли Яндекс Игр (иначе отправка молча игнорируется).</summary>
    public const string EndlessLeaderboardName = "endless";

    /// <summary>Отправляет текущий баланс монет в таблицу лидеров (если модуль установлен).</summary>
    public void SubmitLeaderboard()
    {
#if Leaderboards_yg
        if (YG2.isSDKEnabled) YG2.SetLeaderboard(LeaderboardName, _saveData.totalCoins);
#endif
    }

    /// <summary>Отправляет рекорд Бесконечного режима (самый дальний день) в лидерборд.</summary>
    public void SubmitEndlessLeaderboard()
    {
#if Leaderboards_yg
        if (YG2.isSDKEnabled && _saveData.endlessBestDay > 0)
            YG2.SetLeaderboard(EndlessLeaderboardName, _saveData.endlessBestDay);
#endif
    }

    // ─── Бесконечный режим: доступ к состоянию (для UI/лидерборда) ──────────────
    public bool EndlessActive  => _saveData.endlessMode;
    public int  EndlessDay     => _saveData.endlessDay;
    public int  EndlessBestDay => _saveData.endlessBestDay;

    // ─── Память удовлетворённости по клиентам (пункт 4.3) ────────────────────

    public float GetClientSatisfaction(CharacterType type)
    {
        int key = (int)type;
        int i = _saveData.clientKeys.IndexOf(key);
        return i >= 0 ? _saveData.clientSats[i] : 0.5f; // новый клиент — нейтральные 50%
    }

    public void SetClientSatisfaction(CharacterType type, float value)
    {
        int key = (int)type;
        value = Mathf.Clamp01(value);
        int i = _saveData.clientKeys.IndexOf(key);
        if (i >= 0) _saveData.clientSats[i] = value;
        else { _saveData.clientKeys.Add(key); _saveData.clientSats.Add(value); }
        SaveGame();
    }

    // ─── Батч 6: журнал гостей («Завсегдатаи») ────────────────────────────────

    /// <summary>Записывает визит гостя и лучшую оценку (1..3). Сохраняет.</summary>
    public void RecordVisit(CharacterType type, int stars)
    {
        int key = (int)type;
        int i = _saveData.journalKeys.IndexOf(key);
        if (i < 0)
        {
            _saveData.journalKeys.Add(key);
            _saveData.journalVisits.Add(1);
            _saveData.journalBestStars.Add(Mathf.Clamp(stars, 1, 3));
        }
        else
        {
            _saveData.journalVisits[i]++;
            if (stars > _saveData.journalBestStars[i])
                _saveData.journalBestStars[i] = Mathf.Clamp(stars, 1, 3);
        }
        SaveGame();
    }

    /// <summary>Список встреченных типов гостей (ключи журнала).</summary>
    public System.Collections.Generic.List<int> JournalKeys => _saveData.journalKeys;

    public int GetVisits(CharacterType type)
    {
        int i = _saveData.journalKeys.IndexOf((int)type);
        return i >= 0 ? _saveData.journalVisits[i] : 0;
    }

    public int GetBestStars(CharacterType type)
    {
        int i = _saveData.journalKeys.IndexOf((int)type);
        return i >= 0 ? _saveData.journalBestStars[i] : 0;
    }

    /// <summary>Батч 6: помечает обучающую подсказку показанной. true — если показывается ВПЕРВЫЕ.</summary>
    public bool MarkTipShown(string id)
    {
        if (_saveData.shownTips.Contains(id)) return false;
        _saveData.shownTips.Add(id);
        SaveGame();
        return true;
    }

    // ─── Сохранение / Загрузка ───────────────────────────────────────────────

    /// <summary>Сохраняет прогресс в облако/локально (Яндекс). Вызывается после действий
    /// игрока: после каждого гостя, дня, обучения, покупки (требования 1.9, 1.13.3).</summary>
    public void SaveGame()
    {
        // Батч 12-E: отмечаем «последнюю активность» — база для оффлайн-дохода при следующем входе.
        if (YG2.isSDKEnabled)
        {
            _saveData.lastSeenUnix = NowUnix();
            YG2.SaveProgress();
        }
    }

    private static long NowUnix() => System.DateTimeOffset.UtcNow.ToUnixTimeSeconds();

    // ─── Батч 12-E: оффлайн-доход («кофейня работала, пока тебя не было») ────────
    private void TryGrantOfflineIncome()
    {
        long last = _saveData.lastSeenUnix;
        long now  = NowUnix();
        _saveData.lastSeenUnix = now; // отметка на будущее

        // Не начисляем на первом запуске/во время обучения (нет базы времени).
        if (last <= 0 || _saveData.currentDay < 1 || !_saveData.tutorialDone) return;

        long elapsed = now - last;
        if (elapsed < 600) return; // меньше 10 минут — пропускаем

        long cappedSec  = System.Math.Min(elapsed, 8L * 3600L); // кап 8 часов
        int  ratePerHour = 60 + _saveData.currentDay * 20;
        int  income = (int)(cappedSec / 3600.0 * ratePerHour);
        if (income < 20) return;

        _saveData.totalCoins += income;
        SaveGame();
        Analytics.Send("offline_income", "coins", income.ToString());
        RewardPopupUI.Ensure().Show(
            Loc.T("С возвращением!", "Welcome back!"),
            Loc.T($"Пока тебя не было, кофейня заработала +{income} монет.",
                  $"While you were away, the café earned +{income} coins."),
            new Color(0.95f, 0.8f, 0.3f), 4f);
    }

    private void LoadGame()
    {
        // Данные уже загружены плагином YG2 в YG2.saves (облако/локально).
        // Здесь — только отладочный override стартового дня из инспектора.
        if (_startDay > 1 && _saveData.currentDay < _startDay)
        {
            _saveData.currentDay   = _startDay;
            _saveData.tutorialDone = true;
        }
    }

    // ─── DEBUG: сбросить прогресс ─────────────────────────────────────────────

    [ContextMenu("DEBUG: Reset Save")]
    public void DebugResetSave()
    {
        YG2.SetDefaultSaves();
        if (YG2.isSDKEnabled) YG2.SaveProgress();
        Debug.Log("GameManager: прогресс сброшен.");
    }

    /// <summary>ВРЕМЕННО (для тестирования): сбрасывает прогресс и перезапускает игру
    /// с первой сцены. Привязано к временной кнопке «Сброс» в билдере сцены.</summary>
    public void ResetProgressAndRestart()
    {
        YG2.SetDefaultSaves();
        if (YG2.isSDKEnabled) YG2.SaveProgress();
        Time.timeScale = 1f;
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }

    // ─── YG2: видимость окна ─────────────────────────────────────────────────

    private void OnWindowFocusChanged(bool visible)
    {
        if (_audioController != null)
            _audioController.SetMuted(!visible);
    }
}
