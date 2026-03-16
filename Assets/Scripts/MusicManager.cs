using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioClip musicClip;

    [Header("Settings")]
    [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;
    [SerializeField] private bool playOnStart = true;
    [SerializeField] private bool dontDestroyOnLoad = true;

    void Awake()
    {
        // Singleton (prevents duplicates when changing scenes)
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        if (!musicSource) musicSource = GetComponent<AudioSource>();
        if (!musicSource) musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.spatialBlend = 0f; // 2D music
        musicSource.volume = volume;

        if (musicClip) musicSource.clip = musicClip;
    }

    void Start()
    {
        if (playOnStart) Play();
    }

    void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void Play()
    {
        if (!musicSource) return;
        if (!musicSource.clip) return;
        if (!musicSource.isPlaying) musicSource.Play();
    }

    public void Stop()
    {
        if (!musicSource) return;
        if (musicSource.isPlaying) musicSource.Stop();
    }

    public void SetVolume(float v)
    {
        volume = Mathf.Clamp01(v);
        if (musicSource) musicSource.volume = volume;
    }

    public void SetClip(AudioClip clip, bool restart = true)
    {
        musicClip = clip;
        if (!musicSource) return;

        musicSource.clip = musicClip;
        if (restart) { musicSource.Stop(); Play(); }
    }
}
