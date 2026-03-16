using UnityEngine;

public class PoliceSiren : MonoBehaviour
{
    [Header("Audio")]
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip wailLoop;

    [Header("3D Settings")]
    [Range(0f, 1f)] [SerializeField] private float volume = 0.85f;
    [SerializeField] private float minDistance = 8f;
    [SerializeField] private float maxDistance = 60f;

    [Header("Behavior")]
    [SerializeField] private bool sirenOnAtStart = true;
    [SerializeField] private bool autoSwapModes = true;
    [SerializeField] private float swapEverySeconds = 6f;

    private float timer;
    private bool usingWail = true;

    void Awake()
    {
        if (!source) source = GetComponent<AudioSource>();
        if (!source) source = gameObject.AddComponent<AudioSource>();

        source.loop = true;
        source.playOnAwake = false;
        source.spatialBlend = 1f; // 3D
        source.dopplerLevel = 0.5f;
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
        source.volume = volume;
    }

    void Start()
    {
        if (sirenOnAtStart) TurnOn();
        else TurnOff();
    }

    void Update()
    {
        if (!autoSwapModes) return;
        if (!source || !source.isPlaying) return;

        timer += Time.deltaTime;
        if (timer >= swapEverySeconds)
        {
            timer = 0f;
        }
    }

    public void TurnOn()
    {
        if (!source) return;
        if (!wailLoop) return;

        usingWail = wailLoop != null;
        source.clip = wailLoop;
        if (!source.isPlaying) source.Play();
    }

    public void TurnOff()
    {
        if (!source) return;
        if (source.isPlaying) source.Stop();
    }

}
