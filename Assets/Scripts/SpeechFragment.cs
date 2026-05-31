using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class SpeechFragment
{
    public string name = "replica";
    [Range(0f, 1f)] public float startTime = 0f; // 0.0 = начало трека, 1.0 = конец
    [Range(0f, 1f)] public float endTime = 0.5f;
}

public class SpeechMixer : MonoBehaviour
{
    [Header("Source")]
    public AudioClip compressedMurmur; // Один ускоренный трек

    [Header("Fragments")]
    public List<SpeechFragment> fragments = new List<SpeechFragment>();

    [Header("Playback")]
    [Range(0.3f, 1f)] public float playbackSpeed = 0.66f; // Замедление в Unity

    private AudioSource audioSource;
    private float[] audioData;

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.pitch = playbackSpeed;
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        // Кэшируем аудио-данные для быстрого доступа
        if (compressedMurmur != null)
        {
            audioData = new float[compressedMurmur.samples * compressedMurmur.channels];
            compressedMurmur.GetData(audioData, 0);
        }
    }

    // Воспроизвести фрагмент по имени
    public void PlayFragment(string fragmentName)
    {
        SpeechFragment fragment = fragments.Find(f => f.name == fragmentName);
        if (fragment != null)
        {
            PlayFragment(fragment);
        }
    }

    // Воспроизвести фрагмент по индексу
    public void PlayFragment(int index)
    {
        if (index >= 0 && index < fragments.Count)
        {
            PlayFragment(fragments[index]);
        }
    }

    // Воспроизвести случайный фрагмент
    public void PlayRandomFragment()
    {
        if (fragments.Count > 0)
        {
            int randomIndex = Random.Range(0, fragments.Count);
            PlayFragment(fragments[randomIndex]);
        }
    }

    // Воспроизвести последовательность фрагментов
    public void PlaySequence(params string[] fragmentNames)
    {
        StartCoroutine(PlaySequenceRoutine(fragmentNames));
    }

    private IEnumerator PlaySequenceRoutine(string[] fragmentNames)
    {
        foreach (string name in fragmentNames)
        {
            PlayFragment(name);
            while (audioSource.isPlaying)
            {
                yield return null;
            }
            yield return new WaitForSeconds(0.1f); // Пауза между репликами
        }
    }

    // Основной метод воспроизведения
    private void PlayFragment(SpeechFragment fragment)
    {
        if (audioSource == null || audioData == null || compressedMurmur == null) return;

        // Рассчитываем сэмплы для начала и конца
        int startSample = (int)(fragment.startTime * compressedMurmur.samples);
        int endSample = (int)(fragment.endTime * compressedMurmur.samples);
        int length = endSample - startSample;

        if (length <= 0) return;

        // Создаём временный клип для фрагмента
        AudioClip fragmentClip = AudioClip.Create(
            "Fragment_" + fragment.name,
            length,
            compressedMurmur.channels,
            compressedMurmur.frequency,
            false
        );

        float[] fragmentData = new float[length * compressedMurmur.channels];
        System.Array.Copy(audioData, startSample * compressedMurmur.channels, fragmentData, 0, fragmentData.Length);
        fragmentClip.SetData(fragmentData, 0);

        // Воспроизводим
        audioSource.Stop();
        audioSource.clip = fragmentClip;
        audioSource.Play();
    }
}