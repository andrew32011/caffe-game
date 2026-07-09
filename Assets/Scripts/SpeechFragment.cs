using UnityEngine;
using System.Collections.Generic;
using System.Collections;

[System.Serializable]
public class SpeechFragment
{
    public string name = "replica";
    [Range(0f, 1f)] public float startTime = 0f; // 0.0 = ������ �����, 1.0 = �����
    [Range(0f, 1f)] public float endTime = 0.5f;
}

public class SpeechMixer : MonoBehaviour
{
    [Header("Source")]
    public AudioClip compressedMurmur; // ���� ���������� ����

    [Header("Fragments")]
    public List<SpeechFragment> fragments = new List<SpeechFragment>();

    [Header("Playback")]
    [Range(0.3f, 1f)] public float playbackSpeed = 0.66f; // ���������� � Unity

    private AudioSource audioSource;
    private float[] audioData;
    // Перф: кэш готовых клипов по фрагменту — создаём каждый максимум один раз (без GC на каждый показ реплики).
    private readonly Dictionary<SpeechFragment, AudioClip> _clipCache = new Dictionary<SpeechFragment, AudioClip>();

    void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.pitch = playbackSpeed;
        audioSource.spatialBlend = 0f;
        audioSource.playOnAwake = false;

        // �������� �����-������ ��� �������� �������
        if (compressedMurmur != null)
        {
            audioData = new float[compressedMurmur.samples * compressedMurmur.channels];
            compressedMurmur.GetData(audioData, 0);
        }
    }

    // ������������� �������� �� �����
    public void PlayFragment(string fragmentName)
    {
        SpeechFragment fragment = fragments.Find(f => f.name == fragmentName);
        if (fragment != null)
        {
            PlayFragment(fragment);
        }
    }

    // ������������� �������� �� �������
    public void PlayFragment(int index)
    {
        if (index >= 0 && index < fragments.Count)
        {
            PlayFragment(fragments[index]);
        }
    }

    // ������������� ��������� ��������
    public void PlayRandomFragment()
    {
        if (fragments.Count > 0)
        {
            int randomIndex = Random.Range(0, fragments.Count);
            PlayFragment(fragments[randomIndex]);
        }
    }

    // ������������� ������������������ ����������
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
            yield return new WaitForSeconds(0.1f); // ����� ����� ���������
        }
    }

    // �������� ����� ���������������
    private void PlayFragment(SpeechFragment fragment)
    {
        if (audioSource == null || audioData == null || compressedMurmur == null) return;

        // ������������ ������ ��� ������ � �����
        int startSample = (int)(fragment.startTime * compressedMurmur.samples);
        int endSample = (int)(fragment.endTime * compressedMurmur.samples);
        int length = endSample - startSample;

        if (length <= 0) return;

        // Перф: берём клип из кэша; создаём и наполняем только при первом обращении.
        if (!_clipCache.TryGetValue(fragment, out AudioClip fragmentClip) || fragmentClip == null)
        {
            fragmentClip = AudioClip.Create(
                "Fragment_" + fragment.name,
                length,
                compressedMurmur.channels,
                compressedMurmur.frequency,
                false
            );

            float[] fragmentData = new float[length * compressedMurmur.channels];
            System.Array.Copy(audioData, startSample * compressedMurmur.channels, fragmentData, 0, fragmentData.Length);
            fragmentClip.SetData(fragmentData, 0);

            _clipCache[fragment] = fragmentClip;
        }

        // Громкость бубнежа подчиняется ползунку «Звуки» (AudioController.SfxVolume),
        // т.к. это фактически единственный слышимый SFX в игре.
        audioSource.volume = AudioController.Instance != null
            ? Mathf.Clamp01(AudioController.Instance.SfxVolume) : 1f;

        // Воспроизведение
        audioSource.Stop();
        audioSource.clip = fragmentClip;
        audioSource.Play();
    }
}