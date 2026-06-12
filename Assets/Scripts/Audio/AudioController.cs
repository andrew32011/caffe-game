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

    [Header("Настройки")]
    [Range(0f, 1f)] [SerializeField] private float _musicVolume = 0.4f;
    [Range(0f, 1f)] [SerializeField] private float _sfxVolume   = 0.8f;

    // ─── Состояние ───────────────────────────────────────────────────────────

    private bool _isMuted = false;

    // ─── Жизненный цикл ──────────────────────────────────────────────────────

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

    // ─── Игровые события ─────────────────────────────────────────────────────

    public void PlayDayClear()      => PlaySFX(_dayClearSound);
    public void PlayDayFail()       => PlaySFX(_dayFailSound);
    public void PlayCoin()          => PlaySFX(_coinSound);
    public void PlayWrongOrder()    => PlaySFX(_wrongOrderSound);
    public void PlayCorrectOrder()  => PlaySFX(_correctOrderSound);
    public void PlayCustomerIn()    => PlaySFX(_customerInSound);
}
