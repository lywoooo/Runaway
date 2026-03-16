using UnityEngine;

public class PoliceArrest : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;

    [Header("Detection (No Layers Needed)")]
    [SerializeField] private float detectRadius = 12f;
    [SerializeField] private float hardRadius = 6f;
    [SerializeField] private int maxPoliceToCheck = 32;

    [Header("Arrest Meter")]
    [SerializeField] private float arrestTimeNear = 6.0f;
    [SerializeField] private float arrestTimeHard = 3.0f;
    [SerializeField] private float decayTime = 5.0f;

    [Header("Boxed-In (Lose Faster When Trapped)")]
    [SerializeField] private float boxedSpeed = 2.0f;
    [SerializeField] private int boxedPoliceCount = 2;
    [SerializeField] private float boxedBoost = 2.5f;

    [Header("Contact Bonus")]
    [SerializeField] private float contactFillBonus = 0.25f;

    [Header("Tags")]
    [SerializeField] private string policeTag = "Police";

    private Collider[] hits;
    private float arrest01;
    private float contactBonusAcc;

    // NEW: cached reference so we don’t Find every time
    private GameOverManager gameOverManager;

    public float Arrest01 => arrest01;
    public bool IsArrested => arrest01 >= 1f;

    private void Awake()
    {
        if (!rb) rb = GetComponent<Rigidbody>();
        hits = new Collider[Mathf.Max(8, maxPoliceToCheck)];

        // NEW: find once
        gameOverManager = FindObjectOfType<GameOverManager>();
    }

    private void Update()
    {
        if (IsArrested) return;

        float dt = Time.deltaTime;

        int count = Physics.OverlapSphereNonAlloc(
            transform.position,
            detectRadius,
            hits,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        if (count <= 0)
        {
            Drain(dt);
            contactBonusAcc = 0f;
            return;
        }

        float nearest = float.PositiveInfinity;
        int policeFound = 0;

        for (int i = 0; i < count; i++)
        {
            Collider col = hits[i];
            if (!col) continue;

            // Root tag check so child colliders work
            if (!col.transform.root.CompareTag(policeTag)) continue;

            policeFound++;

            float d = Vector3.Distance(transform.position, col.transform.position);
            if (d < nearest) nearest = d;
        }

        if (policeFound == 0)
        {
            Drain(dt);
            contactBonusAcc = 0f;
            return;
        }

        float timeToLose = (nearest <= hardRadius) ? arrestTimeHard : arrestTimeNear;
        float fillRate = 1f / Mathf.Max(0.01f, timeToLose);

        float speed = rb ? rb.velocity.magnitude : 0f;

        bool boxed =
            speed <= boxedSpeed &&
            policeFound >= boxedPoliceCount &&
            nearest <= hardRadius;

        if (boxed)
            fillRate *= boxedBoost;

        if (contactBonusAcc > 0f)
        {
            fillRate += contactFillBonus;
            contactBonusAcc = 0f;
        }

        arrest01 = Mathf.Clamp01(arrest01 + fillRate * dt);

        if (arrest01 >= 1f)
            OnArrested();
    }

    private void Drain(float dt)
    {
        arrest01 = Mathf.Max(0f, arrest01 - dt / Mathf.Max(0.01f, decayTime));
    }

    private void OnArrested()
    {
        Debug.Log("ARRESTED — GAME OVER");

        if (gameOverManager != null)
        {
            gameOverManager.GameOver();
        }
        else
        {
            // fallback so you still see something if manager isn't in scene
            Time.timeScale = 0f;
        }
    }

    private void OnCollisionStay(Collision c)
    {
        if (c.collider && c.collider.transform.root.CompareTag(policeTag))
        {
            contactBonusAcc += Time.fixedDeltaTime;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, hardRadius);
    }
}
