using System.Collections;
using UnityEngine;

/// <summary>
/// AudioManager singleton minimal:
/// - PlaySound(clip) -> 2D PlayOneShot on manager (UI / global)
/// - PlaySoundAt(clip, pos) -> positional 3D sound instantiated at position (auto-destroy)
/// - Optional volume and spatialBlend control.
/// Drop this on a persistent GameObject in the scene (e.g., "AudioManager").
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Global")]
    [Range(0f, 1f)] public float masterVolume = 1f;

    [Header("3D playback defaults")]
    public float defaultVolume3D = 1f;
    public float defaultSpatialBlend = 1f; // 1 = fully 3D
    public float defaultMinDistance = 0.5f;
    public float defaultMaxDistance = 50f;

    // internal 2D AudioSource for non-positional sounds
    private AudioSource sfxSource2D;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        sfxSource2D = gameObject.AddComponent<AudioSource>();
        sfxSource2D.playOnAwake = false;
        sfxSource2D.spatialBlend = 0f; // 2D
        sfxSource2D.rolloffMode = AudioRolloffMode.Logarithmic;
        sfxSource2D.volume = masterVolume;
    }

    private void OnValidate()
    {
        if (sfxSource2D != null) sfxSource2D.volume = masterVolume;
    }

    /// <summary>
    /// Play a non-positional sound (UI / global)
    /// </summary>
    public void PlaySound(AudioClip clip, float volume = 1f)
    {
        if (clip == null) { Debug.LogWarning("AudioManager.PlaySound: clip null"); return; }
        sfxSource2D.PlayOneShot(clip, Mathf.Clamp01(volume) * masterVolume);
    }

    /// <summary>
    /// Play a positional 3D sound at world position.
    /// Creates a temporary GameObject with AudioSource and destroys when finished.
    /// </summary>
    public void PlaySoundAt(AudioClip clip, Vector3 position, float volume = -1f, float spatialBlend = -1f,
                           float minDistance = -1f, float maxDistance = -1f)
    {
        if (clip == null) { Debug.LogWarning("AudioManager.PlaySoundAt: clip null"); return; }

        float vol = (volume < 0f) ? defaultVolume3D : volume;
        float sp = (spatialBlend < 0f) ? defaultSpatialBlend : spatialBlend;
        float minD = (minDistance < 0f) ? defaultMinDistance : minDistance;
        float maxD = (maxDistance < 0f) ? defaultMaxDistance : maxDistance;

        StartCoroutine(PlayAtCoroutine(clip, position, vol, sp, minD, maxD));
    }

    private IEnumerator PlayAtCoroutine(AudioClip clip, Vector3 pos, float vol, float sp, float minD, float maxD)
    {
        GameObject go = new GameObject($"SFX:{clip.name}");
        go.transform.position = pos;
        AudioSource src = go.AddComponent<AudioSource>();
        src.clip = clip;
        src.spatialBlend = Mathf.Clamp01(sp);
        src.minDistance = Mathf.Max(0.01f, minD);
        src.maxDistance = Mathf.Max(src.minDistance + 0.1f, maxD);
        src.rolloffMode = AudioRolloffMode.Logarithmic;
        src.playOnAwake = false;
        src.volume = Mathf.Clamp01(vol) * masterVolume;
        src.dopplerLevel = 0f;

        src.Play();
        yield return new WaitForSeconds(clip.length + 0.05f);
        Destroy(go);
    }

    /// <summary>
    /// Convenience wrappers matching WeaponShooter usage:
    /// </summary>
    public void PlayShot(AudioClip clip, Vector3 position)
    {
        PlaySoundAt(clip, position);
    }

    public void PlayEmpty(AudioClip clip, Vector3? position = null)
    {
        if (position.HasValue) PlaySoundAt(clip, position.Value);
        else PlaySound(clip);
    }
}
