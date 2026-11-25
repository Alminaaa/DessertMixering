using UnityEngine;

/// <summary>
/// Plays short 2D sound effects at full volume (no distance fade).
/// Created automatically the first time it is used. No setup needed.
///
/// Usage: SoundPlayer.Play(clip, volume, pitchVariation);
/// </summary>
public class SoundPlayer : MonoBehaviour
{
    private const int PoolSize = 12;

    private static SoundPlayer instance;
    private static bool isQuitting;

    private AudioSource[] sources;
    private int nextIndex;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        instance = null;
        isQuitting = false;
        Application.quitting -= OnQuitting;
        Application.quitting += OnQuitting;
    }

    private static void OnQuitting()
    {
        isQuitting = true;
    }

    /// <summary>
    /// Plays a sound once.
    /// pitchVariation 0.05 = pitch changes randomly by +-5%, so repeated sounds feel less robotic.
    /// </summary>
    public static void Play(AudioClip clip, float volume = 1f, float pitchVariation = 0f)
    {
        if (clip == null || isQuitting)
            return;

        if (instance == null)
            CreateInstance();

        instance.PlayInternal(clip, volume, pitchVariation);
    }

    private static void CreateInstance()
    {
        GameObject go = new GameObject("[SoundPlayer]");
        DontDestroyOnLoad(go);
        instance = go.AddComponent<SoundPlayer>();
    }

    private void Awake()
    {
        sources = new AudioSource[PoolSize];

        for (int i = 0; i < PoolSize; i++)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f; // 2D: same volume everywhere
            sources[i] = source;
        }
    }

    private void PlayInternal(AudioClip clip, float volume, float pitchVariation)
    {
        AudioSource source = sources[nextIndex];
        nextIndex = (nextIndex + 1) % PoolSize;

        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, volume);
    }
}