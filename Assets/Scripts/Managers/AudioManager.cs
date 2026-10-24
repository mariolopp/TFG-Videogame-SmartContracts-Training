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

    // Bucles (escritura, recuentos...): cada uno con el componente que lo pidió
    private readonly List<AudioSource> loopSources = new List<AudioSource>();
    private readonly Dictionary<AudioSource, Behaviour> loopOwners = new Dictionary<AudioSource, Behaviour>();

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

    public void PlaySFX(string id) => PlaySFX(id, 1f);

    /// <summary>
    /// Igual que PlaySFX(id) pero multiplicando el pitch de la entrada. Permite que un mismo clip
    /// suene distinto según el contexto (p. ej. la estatua sube de tono con cada capa).
    /// </summary>
    public void PlaySFX(string id, float pitchMultiplier)
    {
        if (!TryGetSfx(id, out var entry)) return;

        AudioSource src = GetFreeSfxSource();
        src.clip = entry.clip;
        src.volume = entry.volume * masterSfxVolume;
        float pitch = entry.randomizePitch
            ? entry.pitch + Random.Range(-entry.pitchVariance, entry.pitchVariance)
            : entry.pitch;
        src.pitch = pitch * pitchMultiplier;
        src.Play();
    }

    /// <summary>
    /// Reproduce un SFX en bucle hasta llamar a StopLoopSFX con el AudioSource devuelto.
    /// Si owner se destruye o se desactiva (p. ej. cambio de escena a mitad de una animación)
    /// el bucle se para solo, porque sus corrutinas ya no llegarán a pararlo.
    /// </summary>
    public AudioSource PlayLoopSFX(string id, Behaviour owner)
    {
        if (!TryGetSfx(id, out var entry)) return null;

        AudioSource src = loopSources.Find(s => !loopOwners.ContainsKey(s));
        if (src == null)
        {
            src = gameObject.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            loopSources.Add(src);
        }
        src.clip = entry.clip;
        src.volume = entry.volume * masterSfxVolume;
        src.pitch = entry.pitch;
        src.Play();
        loopOwners[src] = owner;
        return src;
    }

    public void StopLoopSFX(AudioSource src)
    {
        if (src == null || !loopOwners.Remove(src)) return;
        src.Stop();
    }

    private void Update()
    {
        if (loopOwners.Count == 0) return;
        foreach (var src in loopSources)
        {
            if (loopOwners.TryGetValue(src, out var owner) && (owner == null || !owner.isActiveAndEnabled))
                StopLoopSFX(src);
        }
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

    private bool TryGetSfx(string id, out SoundLibrary.SoundEntry entry)
    {
        if (sfxLookup.TryGetValue(id, out entry) && entry.clip != null) return true;
        Debug.LogWarning($"AudioManager: SFX '{id}' no encontrado en la biblioteca.");
        return false;
    }

    private AudioSource GetFreeSfxSource()
    {
        foreach (var src in sfxPool)
            if (!src.isPlaying) return src;

        // Pool agotada: reutiliza el más antiguo (poco probable con sfxPoolSize=8)
        return sfxPool[0];
    }
}