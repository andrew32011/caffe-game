/// <summary>
/// Вступление в первой сцене (SampleScene). Когда камера доезжает до последней точки
/// и фокусируется на книге — показываем полноэкранный UI с историей Миры. Снизу кнопка
/// «Продолжить»: сначала серая и неактивная, через 2 секунды загорается. По нажатию —
/// загрузка основной сцены игры.
/// Сцена: SampleScene
/// Зависимости: SmoothCameraWaypointController (вызывает Begin), Loc, TMPro
/// SDK: Нет
/// </summary>
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using YG;

public class IntroStoryUI : MonoBehaviour
{
    public static IntroStoryUI Instance { get; private set; }

    [Header("UI (заполняется сборкой Build Intro)")]
    [SerializeField] private CanvasGroup _group;
    [SerializeField] private TextMeshProUGUI _storyText;
    [SerializeField] private Button _continueButton;
    [SerializeField] private TextMeshProUGUI _continueLabel;
    [Tooltip("Кнопка «Пропустить» (верх-право): активна сразу, минует форс-ожидание.")]
    [SerializeField] private Button _skipButton;

    [Header("Настройки")]
    [Tooltip("Через сколько секунд кнопка «Продолжить» загорается (короткое — меньше трения).")]
    [SerializeField] private float _activateDelay = 0.5f;
    [Tooltip("Индекс основной сцены в Build Settings (обычно 1).")]
    [SerializeField] private int _mainSceneIndex = 1;

    private bool _started;

    private void Awake()
    {
        Instance = this;
        if (_group != null)
        {
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.gameObject.SetActive(false);
        }
    }

    /// <summary>Запускает показ истории. Вызывается камерой на финальной точке.
    /// При повторном входе (интро уже просмотрено) — сразу грузит игру без канваса.</summary>
    public void Begin()
    {
        if (_started) return;
        _started = true;
        StartCoroutine(BeginRoutine());
    }

    private IEnumerator BeginRoutine()
    {
        // Ждём инициализации SDK (с таймаутом), чтобы прочитать флаг «интро просмотрено».
        float wait = 0f;
        while (!YG2.isSDKEnabled && wait < 3f) { wait += Time.unscaledDeltaTime; yield return null; }

        bool introSeen = YG2.isSDKEnabled && YG2.saves.introSeen;
        if (introSeen)
        {
            // Повторный вход: канвас не показываем — сразу грузим основную сцену.
            Time.timeScale = 1f;
            SceneManager.LoadScene(_mainSceneIndex);
            yield break;
        }

        // Первый вход: помечаем флаг и показываем историю.
        if (YG2.isSDKEnabled) { YG2.saves.introSeen = true; YG2.SaveProgress(); }
        yield return StartCoroutine(Routine());
    }

    private IEnumerator Routine()
    {
        Analytics.Send(Analytics.IntroStart); // первый вход: показываем историю

        if (_storyText != null) _storyText.text = StoryText();
        SetButtonLit(false); // серая и неактивная

        if (_continueButton != null)
        {
            _continueButton.onClick.RemoveListener(OnContinue);
            _continueButton.onClick.AddListener(OnContinue);
        }

        // Кнопка «Пропустить» активна СРАЗУ — нетерпеливый игрок прыгает в игру,
        // не дожидаясь отсчёта «Продолжить» (снимаем трение первых секунд).
        if (_skipButton != null)
        {
            _skipButton.onClick.RemoveListener(OnSkip);
            _skipButton.onClick.AddListener(OnSkip);
            _skipButton.interactable = true;
            _skipButton.gameObject.SetActive(true);
        }

        if (_group != null)
        {
            _group.gameObject.SetActive(true);
            _group.blocksRaycasts = true;
            // Плавное появление (на unscaled-времени, чтобы не зависеть от Time.timeScale).
            float t = 0f;
            while (t < 1f) { t += Time.unscaledDeltaTime * 1.5f; _group.alpha = Mathf.Clamp01(t); yield return null; }
            _group.alpha = 1f;
        }

        // Обратный отсчёт прямо на кнопке — чтобы игрок видел, через сколько она активируется.
        float remaining = _activateDelay;
        while (remaining > 0f)
        {
            if (_continueLabel != null)
                _continueLabel.text = Loc.T($"Продолжить ({Mathf.CeilToInt(remaining)})",
                                            $"Continue ({Mathf.CeilToInt(remaining)})");
            remaining -= Time.unscaledDeltaTime;
            yield return null;
        }
        if (_continueLabel != null) _continueLabel.text = Loc.T("Продолжить", "Continue");
        SetButtonLit(true); // загорается после отсчёта
    }

    private void SetButtonLit(bool lit)
    {
        if (_continueButton != null)
        {
            _continueButton.interactable = lit;
            var img = _continueButton.targetGraphic as Image;
            if (img != null)
                img.color = lit ? Color.white : new Color(0.45f, 0.45f, 0.5f, 0.7f);
        }
        if (_continueLabel != null)
            _continueLabel.color = lit ? Color.white : new Color(0.8f, 0.8f, 0.85f, 0.5f);
    }

    private void OnContinue()
    {
        Analytics.Send(Analytics.IntroComplete);
        Proceed();
    }

    private void OnSkip()
    {
        Analytics.Send(Analytics.IntroSkip);
        Proceed();
    }

    private void Proceed()
    {
        if (_continueButton != null) _continueButton.interactable = false;
        if (_skipButton != null)     _skipButton.interactable = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene(_mainSceneIndex);
    }

    // Короткая, «ударная» подача (по правилу first-impression: не стена текста).
    // Полную легенду можно раскрыть позже в игре (первый «Сон»/меню истории).
    private string StoryText()
    {
        return Loc.T(
            "За Каем пришли из тумана в ночь перед свадьбой — и унесли за грань, во тьму.\n\n" +
            "У Миры остались лишь аромат кофе и обещание найти его.\n\n" +
            "Свари надежду — по чашке за раз.",

            "They took Kai into the fog the night before the wedding — beyond the veil, into the dark.\n\n" +
            "All Mira has left is the scent of coffee and a promise to find him.\n\n" +
            "Brew hope — one cup at a time.");
    }
}
