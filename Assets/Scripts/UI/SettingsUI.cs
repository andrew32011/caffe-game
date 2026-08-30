/// <summary>
/// Батч 4: меню настроек + пауза. Кнопка-шестерёнка открывает панель и ставит игру
/// на паузу (Time.timeScale = 0). Внутри: громкость музыки и звуков (ползунки,
/// сохраняются в облаке через AudioController → GameManager), переключатель полного
/// экрана (YG2 Fullscreen) и кнопка таблицы лидеров (счёт = монеты).
/// Сцена: MainScene (UI на Canvas)
/// Зависимости: AudioController, GameManager, Loc, UnityEngine.UI, TMPro
/// SDK: YG2 (Fullscreen, Leaderboards — опциональные модули)
/// </summary>
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using YG;

public class SettingsUI : MonoBehaviour
{
    [Header("Открытие/закрытие (шестерёнка = пауза)")]
    [SerializeField] private GameObject _panel;
    [SerializeField] private Button _openButton;
    [SerializeField] private Button _closeButton;

    [Header("Громкость")]
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;
    [SerializeField] private Slider _voiceSlider; // «Голоса» — бубнёж героев, отдельно от эффектов

    [Header("Полный экран")]
    [SerializeField] private Button _fullscreenButton;
    [SerializeField] private TextMeshProUGUI _fullscreenLabel;

    [Header("Таблица лидеров")]
    [SerializeField] private Button _leaderboardButton;
    [SerializeField] private GameObject _leaderboardPanel;
    [SerializeField] private Button _leaderboardCloseButton;

    [Header("Таблица лидеров: Бесконечный режим (Батч 11)")]
    [Tooltip("Кнопка показывается только когда бесконечный режим уже открыт (пройден сюжет).")]
    [SerializeField] private Button _endlessLbButton;
    [SerializeField] private GameObject _endlessLbPanel;
    [SerializeField] private Button _endlessLbCloseButton;

    private bool _ready;
    private bool _prevInputLocked;

    private void Awake()
    {
        if (_openButton  != null) _openButton.onClick.AddListener(Open);
        if (_closeButton != null) _closeButton.onClick.AddListener(Close);
        if (_fullscreenButton != null) _fullscreenButton.onClick.AddListener(ToggleFullscreen);
        if (_leaderboardButton != null) _leaderboardButton.onClick.AddListener(OpenLeaderboard);
        if (_leaderboardCloseButton != null) _leaderboardCloseButton.onClick.AddListener(CloseLeaderboard);
        if (_endlessLbButton != null) _endlessLbButton.onClick.AddListener(OpenEndlessLeaderboard);
        if (_endlessLbCloseButton != null) _endlessLbCloseButton.onClick.AddListener(CloseEndlessLeaderboard);

        if (_musicSlider != null) _musicSlider.onValueChanged.AddListener(OnMusicChanged);
        if (_sfxSlider   != null) _sfxSlider.onValueChanged.AddListener(OnSfxChanged);
        if (_voiceSlider != null) _voiceSlider.onValueChanged.AddListener(OnVoiceChanged);

        if (_panel != null) _panel.SetActive(false);
        if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false);
        if (_endlessLbPanel != null) _endlessLbPanel.SetActive(false);
    }

    public void Open()
    {
        // Синхронизируем ползунки с текущей громкостью БЕЗ записи в сейв.
        _ready = false;
        var audio = AudioController.Instance;
        if (audio != null)
        {
            if (_musicSlider != null) _musicSlider.value = audio.MusicVolume;
            if (_sfxSlider   != null) _sfxSlider.value   = audio.SfxVolume;
            if (_voiceSlider != null) _voiceSlider.value = audio.VoiceVolume;
        }
        _ready = true;

        UpdateFullscreenLabel();

        // Кнопку рекордов бесконечного режима показываем только когда режим уже открыт.
        if (_endlessLbButton != null)
            _endlessLbButton.gameObject.SetActive(GameManager.Instance != null && GameManager.Instance.EndlessActive);

        if (_panel != null) _panel.SetActive(true);

        // Пауза: морозим время (корутины дня) и блокируем клики по предметам.
        Time.timeScale = 0f;
        _prevInputLocked = GameInput.Locked;
        GameInput.Locked = true;
    }

    public void Close()
    {
        if (_panel != null) _panel.SetActive(false);
        if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false);
        Time.timeScale = 1f; // снять паузу
        GameInput.Locked = _prevInputLocked; // вернуть прежнее состояние ввода
    }

    private void OnMusicChanged(float v)
    {
        if (!_ready) return; // не пишем сейв при программной синхронизации
        AudioController.Instance?.SetMusicVolume(v);
    }

    private void OnSfxChanged(float v)
    {
        if (!_ready) return;
        AudioController.Instance?.SetSfxVolume(v);
        AudioController.Instance?.PlaySfxPreview(); // дать услышать уровень звука сразу
    }

    private void OnVoiceChanged(float v)
    {
        if (!_ready) return;
        AudioController.Instance?.SetVoiceVolume(v);
        AudioController.Instance?.PlayVoicePreview(); // услышать уровень «бубнёжа» сразу
    }

    // ─── Полный экран ─────────────────────────────────────────────────────────

    private void ToggleFullscreen()
    {
#if Fullscreen_yg
        YG2.SetFullscreen(!YG2.isFullscreen);
#endif
        UpdateFullscreenLabel();
    }

    private void UpdateFullscreenLabel()
    {
        if (_fullscreenLabel == null) return;
        bool on = false;
#if Fullscreen_yg
        on = YG2.isFullscreen;
#endif
        _fullscreenLabel.text = on ? Loc.T("Полный экран: вкл", "Fullscreen: on")
                                   : Loc.T("Полный экран: выкл", "Fullscreen: off");
    }

    // ─── Таблица лидеров ──────────────────────────────────────────────────────

    private void OpenLeaderboard()
    {
        GameManager.Instance?.SubmitLeaderboard(); // обновляем свой счёт перед показом
        if (_leaderboardPanel != null) _leaderboardPanel.SetActive(true); // LeaderboardYG сам загрузит данные (OnEnable)
    }

    private void CloseLeaderboard()
    {
        if (_leaderboardPanel != null) _leaderboardPanel.SetActive(false);
    }

    // ─── Таблица лидеров: Бесконечный режим (Батч 11) ─────────────────────────

    private void OpenEndlessLeaderboard()
    {
        GameManager.Instance?.SubmitEndlessLeaderboard(); // обновляем свой рекорд перед показом
        if (_endlessLbPanel != null) _endlessLbPanel.SetActive(true); // LeaderboardYG сам загрузит (OnEnable)
    }

    private void CloseEndlessLeaderboard()
    {
        if (_endlessLbPanel != null) _endlessLbPanel.SetActive(false);
    }
}
