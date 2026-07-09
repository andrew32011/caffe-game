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
        _audioController?.ApplySavedVolumes(_saveData.musicVolume, _saveData.sfxVolume); // Батч 4
        YG2.GameReadyAPI();                  // 1.19.2: игрок может начинать
        yield return new WaitForSeconds(0.2f); // Ждём инициализации всех систем

        if (_forceTutorial || !_saveData.tutorialDone)
        {
            yield return StartCoroutine(RunTutorial());

            // Затемнение между обучением и первым днём (пункт 5)
            yield return StartCoroutine(Transition());
        }

        // Батч 2: ежедневный бонус за вход (если сегодня ещё не получали).
        if (_dailyBonus != null)
            yield return StartCoroutine(_dailyBonus.RunIfDue());

        yield return StartCoroutine(RunGameDays());
    }

    // ─── Туториал ────────────────────────────────────────────────────────────

    private IEnumerator RunTutorial()
    {
        _currentPhase = GamePhase.Tutorial;

        if (_tutorialController != null)
        {
            // Туториал сам управляет замком: на объяснении — выкл, на практике — вкл
            yield return StartCoroutine(_tutorialController.RunTutorial());
        }

        GameInput.Locked = true; // после обучения управление выключаем (пункт 5)
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
#if InterstitialAdv_yg
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

            // Пункт 1: перед финальным днём проверяем «цель путешествия» (10000 монет).
            // Не хватило — даём выбор: начать заново с 1-го дня (копить) или купить монеты.
            if (day >= 40 && _journeyGate != null && _saveData.totalCoins < CoinsUI.JourneyGoal)
            {
                GameInput.Locked = true;
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
                    AudioController.Instance?.PlayDayFail();
                    _dialogue.ShowMessage(
                        Loc.T("День не засчитан. Начинаем заново...",
                              "The day doesn't count. Starting over..."), 2f);
                    yield return new WaitForSeconds(2.5f);
                }
            }

            YG2.GameplayStop();

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

            // ─── Вставная сцена (визуальный эффект) ─────────────────────────
            if (dayData.hasVignette && _vfxController != null)
            {
                _currentPhase = GamePhase.StoryVignette;
                yield return StartCoroutine(_vfxController.PlayVignette(
                    dayData.vignetteEffect,
                    dayData.GetVignetteText(),
                    _dialogue));
            }

            // ─── «Сон» в конце каждого дня (пункт 8): тусклый свет + эффекты + текст ─
            if (day < 40 && _vfxController != null)
            {
                _currentPhase = GamePhase.StoryVignette;
                yield return StartCoroutine(PlayDreamVignette(day));
            }

            // Батч 6: тизер «Особого гостя» накануне (пик вовлечения → мотив вернуться завтра).
            if (day < 40 && DayController.IsSpecialDay(day + 1) && _dialogue != null)
            {
                _dialogue.ShowMessage(
                    Loc.T("Завтра придёт кто-то особенный…", "Someone special is coming tomorrow…"), 2.5f);
                yield return new WaitForSeconds(2f);
            }

            // Реклама теперь показывается на каждом переходе — внутри Transition() (пункт 4).

            // ─── Переход к следующему дню ────────────────────────────────────
            _saveData.currentDay++;
            SaveGame();
            SubmitLeaderboard(); // Батч 4: обновляем место в таблице по монетам

            if (_saveData.currentDay > 40)
                break;

            // Затемнение между днями (пункт 5)
            yield return StartCoroutine(Transition());
        }

        yield return StartCoroutine(ShowGameComplete());
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

        yield return null;
    }

    // ─── Монеты ───────────────────────────────────────────────────────────────

    public void AddCoins(int amount)
    {
        _saveData.totalCoins += amount;
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

    /// <summary>Записывает громкость в сейв и сохраняет (вызывает AudioController при изменении).</summary>
    public void SetVolumes(float music, float sfx)
    {
        _saveData.musicVolume = Mathf.Clamp01(music);
        _saveData.sfxVolume   = Mathf.Clamp01(sfx);
        SaveGame();
    }

    // ─── Батч 4: лидерборд по монетам ──────────────────────────────────────────

    /// <summary>Техническое имя таблицы лидеров в консоли Яндекс Игр (счёт = всего монет).</summary>
    public const string LeaderboardName = "coins";

    /// <summary>Отправляет текущий баланс монет в таблицу лидеров (если модуль установлен).</summary>
    public void SubmitLeaderboard()
    {
#if Leaderboards_yg
        if (YG2.isSDKEnabled) YG2.SetLeaderboard(LeaderboardName, _saveData.totalCoins);
#endif
    }

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

    /// <summary>Записывает визит гостя и лучшую оценку (1..3⭐). Сохраняет.</summary>
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
        if (YG2.isSDKEnabled) YG2.SaveProgress();
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
