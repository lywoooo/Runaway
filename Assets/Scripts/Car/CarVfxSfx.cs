using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarVfxSfx : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private CarController car;               
    [SerializeField] private WheelPhysics[] wheels;          
    [SerializeField] private TrailRenderer[] skidTrails;      
    [SerializeField] private ParticleSystem[] skidParticles;  

    [Header("Audio Sources")]
    [SerializeField] private AudioSource engineSource; 
    [SerializeField] private AudioSource skidSource;   

    [Header("Engine Tuning")]
    [SerializeField] private float maxSpeedForAudio = 55f;
    [SerializeField] private float engineVolume = 0.75f;
    [SerializeField] private float minPitch = 0.9f;
    [SerializeField] private float maxPitch = 2.0f;
    [SerializeField] private float pitchSmooth = 8f;

    [Header("Skid Detection")]
    [SerializeField] private float skidMinSpeed = 6f;           
    [SerializeField] private float lateralSlipStart = 2.5f;     
    [SerializeField] private float lateralSlipFull = 6.0f;      
    [SerializeField] private float skidVolume = 0.7f;

    [Header("Skid Marks")]
    [SerializeField] private float trailAlphaAtFullSlip = 0.85f;

    [Header("Particles")]
    [SerializeField] private float particleMinSpeed = 7f;

    void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        if (!car) car = GetComponent<CarController>();
        if (wheels == null || wheels.Length == 0) wheels = GetComponentsInChildren<WheelPhysics>();

        if (engineSource)
        {
            engineSource.loop = true;
            engineSource.playOnAwake = true;
            engineSource.spatialBlend = 1f; 
            if (!engineSource.isPlaying) engineSource.Play();
        }

        if (skidSource)
        {
            skidSource.loop = true;
            skidSource.playOnAwake = true;
            skidSource.spatialBlend = 1f; 
            if (!skidSource.isPlaying) skidSource.Play();
            skidSource.volume = 0f;
        }

        if (skidTrails != null)
            foreach (var t in skidTrails) if (t) t.emitting = false;

        if (skidParticles != null)
            foreach (var p in skidParticles) if (p) { var em = p.emission; em.enabled = false; }
    }

    void Update()
    {
        if (!rb) return;

        float speed = rb.velocity.magnitude;
        float speed01 = Mathf.Clamp01(speed / Mathf.Max(1f, maxSpeedForAudio));

        if (engineSource)
        {
            float throttle = car ? Mathf.Abs(car.throttleInput) : 0f;
            float targetPitch = Mathf.Lerp(minPitch, maxPitch, Mathf.Clamp01(speed01 * 0.75f + throttle * 0.35f));
            engineSource.pitch = Mathf.Lerp(engineSource.pitch, targetPitch, Time.deltaTime * pitchSmooth);
            engineSource.volume = engineVolume;
        }

        float maxSlip01 = 0f;

        for (int i = 0; i < wheels.Length; i++)
        {
            var w = wheels[i];
            if (!w) continue;

            Vector3 contact = w.isGrounded ? w.contactPoint : w.transform.position;
            Vector3 v = rb.GetPointVelocity(contact);

            float lateral = Mathf.Abs(Vector3.Dot(v, transform.right));

            float slip01 = Mathf.InverseLerp(lateralSlipStart, lateralSlipFull, lateral);
            maxSlip01 = Mathf.Max(maxSlip01, slip01);

            bool shouldSkid = w.isGrounded && speed >= skidMinSpeed && slip01 > 0.01f;

            if (skidTrails != null && i < skidTrails.Length && skidTrails[i])
            {
                var tr = skidTrails[i];
                tr.emitting = shouldSkid;

                float a = Mathf.Clamp01(slip01) * trailAlphaAtFullSlip;
                SetTrailAlpha(tr, a);
            }

            if (skidParticles != null && i < skidParticles.Length && skidParticles[i])
            {
                var ps = skidParticles[i];
                var em = ps.emission;

                bool emit = shouldSkid && speed >= particleMinSpeed;
                em.enabled = emit;

                if (emit)
                {
                    if (!ps.isPlaying) ps.Play();
                }
                else
                {
                    if (ps.isPlaying) ps.Stop();
                }
            }
        }

        if (skidSource)
        {
            float speedGate = Mathf.InverseLerp(skidMinSpeed, skidMinSpeed * 2f, speed);
            float vol = skidVolume * maxSlip01 * speedGate;
            skidSource.volume = Mathf.Lerp(skidSource.volume, vol, Time.deltaTime * 12f);
            skidSource.pitch = Mathf.Lerp(0.9f, 1.15f, speed01);
        }
    }

    static void SetTrailAlpha(TrailRenderer tr, float a)
    {
        Color s = tr.startColor;
        Color e = tr.endColor;
        s.a = a;
        e.a = 0f;
        tr.startColor = s;
        tr.endColor = e;
    }
}
