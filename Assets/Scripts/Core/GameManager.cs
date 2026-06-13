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

    [Header("Свет для «сна» (приглушается между днями; можно не задавать — найдётся сам)")]
    [SerializeField] private Light _sceneLight;

    [Header("Сохранение")]
    [SerializeField] private int _startDay = 1; // 0 — показывать туториал
    [Tooltip("Всегда показывать обучение при старте (игнорируя сохранение). Удобно при разработке.")]
    [SerializeField] private bool _forceTutorial = true;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private GamePhase _currentPhase = GamePhase.Tutorial;
    private GameSaveData _saveData  = new GameSaveData();

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

        // Подписываемся на события видимости (требование YG2 — пауза звука)
        YG2.onFocusWindowGame += OnWindowFocusChanged;

        // Этапами управляет DayController — отключаем автозапуск этапа 0,
        // иначе гость пойдёт к стойке до начала дня
        if (_stages != null)
            _stages.autoStartStageZero = false;
    }

    private void Start()
    {
        LoadGame();
        YG2.GameReadyAPI(); // Сообщаем Яндексу что игра готова

        StartCoroutine(StartGameFlow());
    }

    private void OnDestroy()
    {
        YG2.onFocusWindowGame -= OnWindowFocusChanged;
    }

    // ─── Основной поток игры ──────────────────────────────────────────────────

    private IEnumerator StartGameFlow()
    {
        GameInput.Locked = true;             // на старте управление выключено
        yield return new WaitForSeconds(0.3f); // Ждём инициализации всех систем

        if (_forceTutorial || !_saveData.tutorialDone)
        {
            yield return StartCoroutine(RunTutorial());

            // Затемнение между обучением и первым днём (пункт 5)
            yield return StartCoroutine(Transition());
        }

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

        // 1) Уводим экран в чёрный и крупной надписью говорим, что это сон (пункт 3)
        if (_vfxController != null)
            yield return StartCoroutine(_vfxController.FadeScreen(true, 0.6f));
        _dialogue?.ShowMessage(Loc.T("Это сон. Всего лишь сон…", "It's a dream. Only a dream…"), 2.2f);
        yield return new WaitForSeconds(2.6f);

        // 2) Чуть приоткрываем тьму и играем кошмар: эффект + текст
        if (_vfxController != null)
            yield return StartCoroutine(_vfxController.FadeScreen(false, 0.5f)); // станет видно сцену в темноте

        var effects = new[]
        {
            VignetteEffectType.CameraShake, VignetteEffectType.RedPulse,
            VignetteEffectType.VisionLoss,  VignetteEffectType.DarknessFlash
        };
        var effect = effects[day % effects.Length];
        string text = Loc.IsRu ? DreamLines[day % DreamLines.Length][0]
                               : DreamLines[day % DreamLines.Length][1];

        yield return StartCoroutine(_vfxController.PlayVignette(effect, text, _dialogue));

        // Возвращаем свет (просыпаемся)
        if (_sceneLight != null)
            yield return StartCoroutine(FadeLight(_sceneLight, _sceneLight.intensity, origIntensity, 0.6f));

        CoffeeCraftingSystem.Instance?.SetHeroVisible(false);
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

    /// <summary>Затемнить экран → пауза → высветлить. Управление выключено на время.</summary>
    private IEnumerator Transition()
    {
        GameInput.Locked = true;
        if (_vfxController != null)
        {
            yield return StartCoroutine(_vfxController.FadeScreen(true, 0.6f));
            yield return new WaitForSeconds(0.3f);
            yield return StartCoroutine(_vfxController.FadeScreen(false, 0.6f));
        }
        else
        {
            yield return new WaitForSeconds(0.6f);
        }
    }

    // ─── Цикл дней ───────────────────────────────────────────────────────────

    private IEnumerator RunGameDays()
    {
        while (_saveData.currentDay <= 20)
        {
            int day = _saveData.currentDay;
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
            UiEffects.Instance?.DayEndBanner(Loc.T($"День {day} завершён", $"Day {day} complete"));

            if (_dayResultUI != null)
            {
                _dayResultUI.Show(day, _dayController.CoinsEarnedToday, dayData.GetDayEndText());
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
            if (day < 20 && _vfxController != null)
            {
                _currentPhase = GamePhase.StoryVignette;
                yield return StartCoroutine(PlayDreamVignette(day));
            }

            // ─── Межуровневая реклама (каждые 3 дня) ────────────────────────
            if (day % 3 == 0)
            {
#if InterstitialAdv_yg
                YG2.InterstitialAdvShow();
                yield return new WaitUntil(() => !YG2.nowInterAdv);
#endif
            }

            // ─── Переход к следующему дню ────────────────────────────────────
            _saveData.currentDay++;
            SaveGame();

            if (_saveData.currentDay > 20)
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

        if (_dialogue != null)
        {
            _dialogue.ShowMessage(
                Loc.T("«Междумирье» снова живёт. Кай рядом. Границы запечатаны навсегда.\n\nСПАСИБО ЗА ИГРУ!",
                      "The Inbetween lives again. Kai is by your side. The borders are sealed forever.\n\nTHANK YOU FOR PLAYING!"),
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

    // ─── Сохранение / Загрузка ───────────────────────────────────────────────

    private void SaveGame()
    {
        string json = JsonUtility.ToJson(_saveData);
        PlayerPrefs.SetString("GameSave", json);
        PlayerPrefs.Save();
    }

    private void LoadGame()
    {
        string json = PlayerPrefs.GetString("GameSave", "");
        if (!string.IsNullOrEmpty(json))
        {
            try { _saveData = JsonUtility.FromJson<GameSaveData>(json); }
            catch { _saveData = new GameSaveData(); }
        }

        // Если установлен startDay в инспекторе — используем его для отладки
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
        PlayerPrefs.DeleteKey("GameSave");
        _saveData = new GameSaveData();
        Debug.Log("GameManager: прогресс сброшен.");
    }

    // ─── YG2: видимость окна ─────────────────────────────────────────────────

    private void OnWindowFocusChanged(bool visible)
    {
        if (_audioController != null)
            _audioController.SetMuted(!visible);
    }
}
