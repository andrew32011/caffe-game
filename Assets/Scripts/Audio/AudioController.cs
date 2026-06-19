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

    [Header("Клипы")]
    [SerializeField] private AudioClip _mainTheme;         // Основная музыка кофейни
    [SerializeField] private AudioClip _dayClearSound;     // Конец дня (успех)
    [SerializeField] private AudioClip _dayFailSound;      // Рестарт дня
    [SerializeField] private AudioClip _coinSound;         // Монета
    [SerializeField] private AudioClip _wrongOrderSound;   // Ошибка заказа
    [SerializeField] private AudioClip _correctOrderSound; // Правильный заказ
    [SerializeField] private AudioClip _customerInSound;   // Гость входит

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
    }

    private void Start()
    {
        // Обязательное требование YG2 — останавливаем звук при скрытии
        YG2.onFocusWindowGame += HandleWindowFocus;

        PlayMusic(_mainTheme);
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

    /// <summary>Применяет сохранённую громкость без записи в сейв (вызывает GameManager после загрузки).</summary>
    public void ApplySavedVolumes(float music, float sfx)
    {
        _musicVolume = Mathf.Clamp01(music);
        _sfxVolume   = Mathf.Clamp01(sfx);
        if (_musicSource != null) _musicSource.volume = _musicVolume;
    }

    // ─── Игровые события ─────────────────────────────────────────────────────

    public void PlayDayClear()      => PlaySFX(_dayClearSound);
    public void PlayDayFail()       => PlaySFX(_dayFailSound);
    public void PlayCoin()          => PlaySFX(_coinSound);
    public void PlayWrongOrder()    => PlaySFX(_wrongOrderSound);
    public void PlayCorrectOrder()  => PlaySFX(_correctOrderSound);
    public void PlayCustomerIn()    => PlaySFX(_customerInSound);

    // Сочность (Батч 1)
    public void PlayPour()          => PlaySFX(_pourSound);
    public void PlayServeDing()     => PlaySFX(_serveDingSound);
    public void PlayPerfect()       => PlaySFX(_perfectSound);
    public void PlayStar()          => PlaySFX(_starSound);
    public void PlayBonus()         => PlaySFX(_bonusSound);
}
