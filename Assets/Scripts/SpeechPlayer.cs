using UnityEngine;

public class SpeechPlayer : MonoBehaviour
{
    [Header("Compressed Speech")]
    public AudioClip compressedSpeech; // Ускоренная в 2-3 раза запись

    [Header("Playback Settings")]
    [Range(0.1f, 1f)]
    public float playbackSpeed = 0.5f; // 0.5 = замедление в 2 раза

    private AudioSource audioSource;
    [SerializeField] private bool startspeech = false;

    void Start()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.clip = compressedSpeech;
        audioSource.pitch = playbackSpeed; // Ключевой параметр!
        audioSource.spatialBlend = 0f;     // 2D звук (экономия ресурсов)
        audioSource.pitch = 1f; // ВАЖНО: pitch = 1.0!
        //audioSource.Play();
    }
    private void Update()
    {
        if (startspeech)
        {
            PlaySpeech();
            startspeech = false;
        }
    }

    // Для запуска из других скриптов
    public void PlaySpeech()
    {
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        audioSource.pitch = playbackSpeed;
        audioSource.Play();
    }
}