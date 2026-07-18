/// <summary>
/// Менеджер звука. Обязательно паузирует AudioListener при скрытии окна (требование YG2).
/// Управляет фоновой музыкой и эффектами.
/// Сцена: MainScene
/// Зависимости: YG2 (onFocusWindowGame)
/// SDK: YG2 (window visibility)
/// </summary>
using UnityEngine;
using YG;

public class AudioController : MonoBehaviour
{
    public static AudioController Instance { get; private set; }

    // ─── Инспектор ───────────────────────────────────────────────────────────

    [Header("Источники звука")]
    [SerializeField] private AudioSource _musicSource;     // Фоновая музыка
    [SerializeField] private AudioSource _sfxSource;       // Эффекты
    [SerializeField] private SpeechMixer _speechMixer;     // Бубнёж (реальный звук игры + превью громкости)

    [Header("Банк звуков (пакет 50 клипов + музыка)")]
    [SerializeField] private SoundBank _bank;

    [Header("Клипы (запасные; приоритет — банк)")]
    [SerializeField] private AudioClip _mainTheme;         // Основная музыка кофейни
    [SerializeField] private AudioClip _dayClearSound;     // Конец дня (успех)
    [SerializeField] private AudioClip _dayFailSound;      // Рестарт дня
    [SerializeField] private AudioClip _coinSound;         // Монета
    [SerializeField] private AudioClip _wrongOrderSound;   // Ошибка заказа
    [SerializeField] private AudioClip _correctOrderSound; // Правильный заказ
    [SerializeField] private AudioClip _customerInSound;   // Гость входит

    [Header("Ночная атмосфера (сон между днями, пункт 7)")]
    [SerializeField] private AudioClip _nightAmbience;     // «Шёпот призраков» на стадии сна
    private AudioSource _nightSource;                      // отдельный зацикленный источник

    [Header("Клипы — сочность (Батч 1; можно оставить пустыми — игра не упадёт)")]
    [SerializeField] private AudioClip _pourSound;         // Налив ингредиента
    [SerializeField] private AudioClip _serveDingSound;    // Подача напитка (динь)
    [SerializeField] private AudioClip _perfectSound;      // «Идеально!» / 3 звезды
    [SerializeField] private AudioClip _starSound;         // Появление звезды
    [SerializeField] private AudioClip _bonusSound;        // Ежедневный бонус / награда

    [Header("Настройки")]
    [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float _sfxVolume   = 0.8f;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private bool _isMuted = false;

    // ─── Жизненный цикл ──────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;

        // Отдельный зацикленный источник для ночной атмосферы (создаём в коде,
        // чтобы не требовать ручной привязки в билдере).
        _nightSource = gameObject.AddComponent<AudioSource>();
        _nightSource.loop        = true;
        _nightSource.playOnAwake = false;
    }

    private void Start()
    {
        // Обязательное требование YG2 — останавливаем звук при скрытии
        YG2.onFocusWindowGame += HandleWindowFocus;

        PlayMusic(_bank != null && _bank.music != null ? _bank.music : _mainTheme);
    }

    private void OnDestroy()
    {
        YG2.onFocusWindowGame -= HandleWindowFocus;
    }

    // ─── YG2: обработка видимости ────────────────────────────────────────────

    private void HandleWindowFocus(bool visible)
    {
        // Это обязательное требование Яндекс Игр (п. 1.3)
        AudioListener.pause  = !visible;
        AudioListener.volume = visible ? 1f : 0f;
    }

    // ─── Публичное API ───────────────────────────────────────────────────────

    public void SetMuted(bool muted)
    {
        _isMuted = muted;
        AudioListener.volume = muted ? 0f : 1f;
    }

    public void PlayMusic(AudioClip clip)
    {
        if (_musicSource == null || clip == null) return;
        _musicSource.clip   = clip;
        _musicSource.volume = _musicVolume;
        _musicSource.loop   = true;
        _musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {
        if (_sfxSource == null || clip == null) return;
        _sfxSource.PlayOneShot(clip, _sfxVolume);
    }

    // ─── Батч 4: настройки громкости ─────────────────────────────────────────

    public float MusicVolume => _musicVolume;
    public float SfxVolume   => _sfxVolume;

    /// <summary>Меняет громкость музыки вживую и сохраняет (через GameManager → облако).</summary>
    public void SetMusicVolume(float v)
    {
        _musicVolume = Mathf.Clamp01(v);
        if (_musicSource != null) _musicSource.volume = _musicVolume;
        GameManager.Instance?.SetVolumes(_musicVolume, _sfxVolume);
    }

    /// <summary>Меняет громкость эффектов (применится к следующим звукам) и сохраняет.</summary>
    public void SetSfxVolume(float v)
    {
        _sfxVolume = Mathf.Clamp01(v);
        GameManager.Instance?.SetVolumes(_musicVolume, _sfxVolume);
    }

    private float _lastPreviewTime;
    /// <summary>Проигрывает короткий бубнёж на текущей громкости эффектов — чтобы игрок
    /// СЛЫШАЛ уровень при перетаскивании ползунка (в меню на паузе иначе звука нет). С троттлингом.</summary>
    public void PlaySfxPreview()
    {
        if (Time.unscaledTime - _lastPreviewTime < 0.12f) return;
        _lastPreviewTime = Time.unscaledTime;
        if (_speechMixer != null) _speechMixer.PlayRandomFragment();
    }

    /// <summary>Применяет сохранённую громкость без записи в сейв (вызывает GameManager после загрузки).</summary>
    public void ApplySavedVolumes(float music, float sfx)
    {
        _musicVolume = Mathf.Clamp01(music);
        _sfxVolume   = Mathf.Clamp01(sfx);
        if (_musicSource != null) _musicSource.volume = _musicVolume;
    }

    // ─── Музыка: пауза ночью / на видениях ────────────────────────────────────

    public void PauseMusic()  { if (_musicSource != null && _musicSource.isPlaying) _musicSource.Pause(); }
    public void ResumeMusic() { if (_musicSource != null && _musicSource.clip != null && !_isMuted) _musicSource.UnPause(); }

    // ─── Ночная атмосфера (пункт 7): звучит на «сне», глохнет до рекламы ───────

    /// <summary>Включает зацикленный ночной эмбиент (если клип задан). Громкость — по SFX.</summary>
    public void PlayNight()
    {
        if (_nightSource == null || _nightAmbience == null) return;
        _nightSource.clip   = _nightAmbience;
        _nightSource.volume = Mathf.Clamp01(_sfxVolume * 0.9f);
        _nightSource.Play();
    }

    /// <summary>Останавливает ночной эмбиент (на пробуждении, перед показом рекламы).</summary>
    public void StopNight()
    {
        if (_nightSource != null && _nightSource.isPlaying) _nightSource.Stop();
    }

    // ─── Игровые события (приоритет — клип из банка, иначе запасной) ───────────

    private AudioClip B(AudioClip bankClip, AudioClip fallback) => bankClip != null ? bankClip : fallback;

    public void PlayDayClear()      => PlaySFX(B(_bank?.dayClear, _dayClearSound));
    public void PlayDayFail()       => PlaySFX(B(_bank?.dayFail, _dayFailSound));
    public void PlayCoin()          => PlaySFX(B(_bank?.coin, _coinSound));
    public void PlayWrongOrder()    => PlaySFX(B(_bank?.wrong, _wrongOrderSound));
    public void PlayCorrectOrder()  => PlaySFX(B(_bank?.correct, _correctOrderSound));
    public void PlayCustomerIn()    => PlaySFX(B(_bank?.customerIn, _customerInSound));

    public void PlayPour()          => PlaySFX(B(_bank?.pour, _pourSound));
    public void PlayServeDing()     => PlaySFX(B(_bank?.ding, _serveDingSound));
    public void PlayPerfect()       => PlaySFX(B(_bank?.perfect, _perfectSound));
    public void PlayStar()          => PlaySFX(B(_bank?.star, _starSound));
    public void PlayBonus()         => PlaySFX(B(_bank?.bonus, _bonusSound));

    // Новые (только из банка)
    public void PlayClick()         => PlaySFX(_bank?.click);
    public void PlayUiOpen()        => PlaySFX(_bank?.uiOpen);
    public void PlayUiClose()       => PlaySFX(_bank?.uiClose);
    public void PlayCombo()         => PlaySFX(_bank?.combo);
}
