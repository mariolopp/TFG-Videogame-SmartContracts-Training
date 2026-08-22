using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private SoundLibrary library;
    [SerializeField] private int sfxPoolSize = 8;

    [Header("Mezcla")]
    [Range(0f, 1f)] public float masterSfxVolume = 1f;
    [Range(0f, 1f)] public float masterMusicVolume = 0.6f;

    private AudioSource musicSource;
    private List<AudioSource> sfxPool;
    private Dictionary<string, SoundLibrary.SoundEntry> sfxLookup;
    private Dictionary<string, SoundLibrary.SoundEntry> musicLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        BuildLookups();
        BuildSfxPool();

        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
    }

    private void BuildLookups()
    {
        sfxLookup = new Dictionary<string, SoundLibrary.SoundEntry>();
        musicLookup = new Dictionary<string, SoundLibrary.SoundEntry>();

        if (library == null) return;
        foreach (var entry in library.sfx) sfxLookup[entry.id] = entry;
        foreach (var entry in library.music) musicLookup[entry.id] = entry;
    }

    private void BuildSfxPool()
    {
        sfxPool = new List<AudioSource>();
        for (int i = 0; i < sfxPoolSize; i++)
        {
            AudioSource src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            sfxPool.Add(src);
        }
    }

    // ---------- API pública ----------

    public void PlaySFX(string id)
    {
        if (!sfxLookup.TryGetValue(id, out var entry) || entry.clip == null)
        {
            Debug.LogWarning($"AudioManager: SFX '{id}' no encontrado en la biblioteca.");
            return;
        }

        AudioSource src = GetFreeSfxSource();
        src.clip = entry.clip;
        src.volume = entry.volume * masterSfxVolume;
        src.pitch = entry.randomizePitch
            ? entry.pitch + Random.Range(-entry.pitchVariance, entry.pitchVariance)
            : entry.pitch;
        src.Play();
    }

    public void PlayMusic(string id, bool restartIfSame = false)
    {
        if (!musicLookup.TryGetValue(id, out var entry) || entry.clip == null)
        {
            Debug.LogWarning($"AudioManager: música '{id}' no encontrada en la biblioteca.");
            return;
        }

        if (!restartIfSame && musicSource.clip == entry.clip && musicSource.isPlaying) return;

        musicSource.clip = entry.clip;
        musicSource.volume = entry.volume * masterMusicVolume;
        musicSource.pitch = entry.pitch;
        musicSource.Play();
    }

    public void StopMusic() => musicSource.Stop();

    public void SetSfxVolume(float v) { masterSfxVolume = Mathf.Clamp01(v); }
    public void SetMusicVolume(float v)
    {
        masterMusicVolume = Mathf.Clamp01(v);
        musicSource.volume = masterMusicVolume;
    }

    private AudioSource GetFreeSfxSource()
    {
        foreach (var src in sfxPool)
            if (!src.isPlaying) return src;

        // Pool agotada: reutiliza el más antiguo (poco probable con sfxPoolSize=8)
        return sfxPool[0];
    }
}