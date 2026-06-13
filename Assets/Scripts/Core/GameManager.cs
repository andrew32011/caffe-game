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
            _currentPhase = GamePhase.DayResult;
            _saveData.totalCoins += _dayController.CoinsEarnedToday;

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
