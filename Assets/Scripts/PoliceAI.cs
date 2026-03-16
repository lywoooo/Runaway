using UnityEngine;
using UnityEngine.AI;

public class PoliceAI : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private CarController car;
    [SerializeField] private Rigidbody rb;
    [SerializeField] private Transform target;

    [Header("Pathing")]
    [SerializeField] private float repathInterval = 0.25f;
    [SerializeField] private float waypointReach = 3.0f;

    [Header("Lane Bias")]
    [SerializeField] private float centerOffset = 0.6f;    // 0.4–0.9 for 10-unit roads
    [SerializeField] private float offsetSmoothing = 10f;
    [SerializeField] private float navSnapRadius = 2.0f;   // snap aim point back onto road navmesh
    private float offsetCmd;

    [Header("Steering")]
    [SerializeField] private float steerSensitivity = 1.2f;
    [SerializeField] private float steerSmoothing = 10f;
    [SerializeField] private float maxSteerAtHighSpeed = 0.65f; // let it steer more at speed
    [SerializeField] private float highSpeedStart = 14f;
    [SerializeField] private float highSpeedFull = 30f;

    [Header("Lookahead (tight roads)")]
    [SerializeField] private float lookAheadMin = 2.0f;
    [SerializeField] private float lookAheadMax = 6.5f;

    [Header("Turn Speed Control (fix overshoot)")]
    [Tooltip("Desired speed when going straight-ish.")]
    [SerializeField] private float straightSpeed = 26f;

    [Tooltip("Desired speed for very sharp turns.")]
    [SerializeField] private float cornerSpeed = 10f;

    [Tooltip("At what angle we start slowing for turns.")]
    [SerializeField] private float slowAngle = 18f;

    [Tooltip("Angle considered a 'full corner'.")]
    [SerializeField] private float fullCornerAngle = 70f;

    [Tooltip("How close to the aim point we treat as 'approaching a turn'.")]
    [SerializeField] private float turnApproachDistance = 16f;

    [Tooltip("Extra braking when too fast for a turn.")]
    [SerializeField] private float brakeAggression = 1.0f; // 0.8–1.5

    [Header("Stuck Recovery")]
    [SerializeField] private float stuckSpeed = 1.0f;
    [SerializeField] private float stuckTime = 0.8f;
    [SerializeField] private float reverseTime = 0.9f;      // longer reverse
    [SerializeField] private float reverseThrottle = -0.85f; // stronger reverse
    [SerializeField] private float unstickSteer = 0.85f;     // stronger wiggle
    [SerializeField] private float wallProbeDistance = 1.8f; // raycast to detect wall
    [SerializeField] private LayerMask wallMask = ~0;        // set to Buildings if you have a layer

    private float stuckTimer;
    private float reverseTimer;

    [Header("Debug")]
    [SerializeField] private bool debug = false;

    private NavMeshPath path;
    private int cornerIndex = 1;
    private float repathTimer;
    private float steerCmd;

    void Awake()
    {
        if (!car) car = GetComponentInParent<CarController>();
        if (!rb) rb = GetComponentInParent<Rigidbody>();
        if (path == null) path = new NavMeshPath();

        if (car) car.isAIControlled = true;
    }

    void OnEnable()
    {
        if (path == null) path = new NavMeshPath();
        repathTimer = 0f;
    }

    void Update()
    {
        if (!car || !rb || !target) return;

        repathTimer -= Time.deltaTime;
        if (repathTimer <= 0f)
        {
            repathTimer = repathInterval;
            Repath();
        }
    }

    void FixedUpdate()
    {
        if (!car || !rb || !target) return;
        Drive();
    }

    void Repath()
    {
        if (path == null) path = new NavMeshPath();

        if (!NavMesh.SamplePosition(transform.position, out NavMeshHit startHit, 10f, NavMesh.AllAreas))
            return;

        if (!NavMesh.SamplePosition(target.position, out NavMeshHit endHit, 25f, NavMesh.AllAreas))
            return;

        bool ok = NavMesh.CalculatePath(startHit.position, endHit.position, NavMesh.AllAreas, path);
        if (!ok || path.corners == null || path.corners.Length < 2)
            return;

        cornerIndex = 1;
    }

    void Drive()
    {
        float speed = rb.velocity.magnitude;

        // --- reverse recovery overrides everything ---
        if (reverseTimer > 0f)
        {
            reverseTimer -= Time.fixedDeltaTime;

            // If we are touching a wall, steer away from it
            float steerAway = Mathf.Sign(Mathf.Sin(Time.time * 11f)); // fallback wiggle
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, transform.right, out var hitR, wallProbeDistance, wallMask))
                steerAway = -1f; // wall on right => steer left
            else if (Physics.Raycast(transform.position + Vector3.up * 0.5f, -transform.right, out var hitL, wallProbeDistance, wallMask))
                steerAway = 1f; // wall on left => steer right

            car.SetInputs(reverseThrottle, steerAway * unstickSteer, false);
            return;
        }

        if (path == null || path.corners == null || path.corners.Length < 2)
        {
            car.SetInputs(0.55f, 0f, false);
            return;
        }

        // Advance corners
        while (cornerIndex < path.corners.Length &&
               Vector3.Distance(transform.position, path.corners[cornerIndex]) < waypointReach)
        {
            cornerIndex++;
        }

        if (cornerIndex >= path.corners.Length)
        {
            car.SetInputs(0f, 0f, true);
            return;
        }

        // Lookahead
        float lookAhead = Mathf.Lerp(lookAheadMin, lookAheadMax, Mathf.InverseLerp(0f, 30f, speed));
        Vector3 wp = GetLookaheadPoint(lookAhead);

        // Raw turn direction (before offset)
        Vector3 rawTo = wp - transform.position; rawTo.y = 0f;
        if (rawTo.sqrMagnitude < 0.25f)
        {
            car.SetInputs(0.6f, 0f, false);
            return;
        }

        Vector3 rawDir = rawTo.normalized;
        float rawSigned = Vector3.SignedAngle(transform.forward, rawDir, Vector3.up);

        // Lane bias offset
        float desiredOffset = -Mathf.Sign(rawSigned) * centerOffset;
        float ot = 1f - Mathf.Exp(-offsetSmoothing * Time.fixedDeltaTime);
        offsetCmd = Mathf.Lerp(offsetCmd, desiredOffset, ot);

        wp += transform.right * offsetCmd;

        // Snap back onto navmesh
        if (NavMesh.SamplePosition(wp, out var snapped, navSnapRadius, NavMesh.AllAreas))
            wp = snapped.position;

        // Recompute after offset
        Vector3 to = wp - transform.position; to.y = 0f;
        if (to.sqrMagnitude < 0.25f)
        {
            car.SetInputs(0.6f, 0f, false);
            return;
        }

        Vector3 dir = to.normalized;
        float signed = Vector3.SignedAngle(transform.forward, dir, Vector3.up);
        float angleAbs = Mathf.Abs(signed);

        // Steering demand
        float desiredSteer = Mathf.Clamp(signed / 45f, -1f, 1f) * steerSensitivity;

        // Cap steer at high speed (but less harsh than before)
        float speedT = Mathf.InverseLerp(highSpeedStart, highSpeedFull, speed);
        float steerCap = Mathf.Lerp(1f, maxSteerAtHighSpeed, speedT);
        desiredSteer *= steerCap;

        float st = 1f - Mathf.Exp(-steerSmoothing * Time.fixedDeltaTime);
        steerCmd = Mathf.Lerp(steerCmd, desiredSteer, st);
        steerCmd = Mathf.Clamp(steerCmd, -1f, 1f);

        // -----------------------------
        // TURN SPEED CONTROL (main fix)
        // -----------------------------
        float distToAim = Vector3.Distance(transform.position, wp);

        // How "corner-y" is this? 0 = straight, 1 = full corner
        float cornerT = Mathf.InverseLerp(slowAngle, fullCornerAngle, angleAbs);

        // Only slow if we're actually approaching the turn (prevents random braking on straights)
        float approachT = Mathf.InverseLerp(turnApproachDistance, 0f, distToAim);

        // Desired speed based on turn severity + closeness
        float desiredSpeed = Mathf.Lerp(straightSpeed, cornerSpeed, cornerT * approachT);

        // Convert desiredSpeed into throttle/brake (simple controller)
        float throttle = 1f;
        bool brake = false;

        float speedError = desiredSpeed - speed;

        if (speedError >= 0f)
        {
            // below desired speed => throttle, but scale down if corner is sharp
            float throttleScale = Mathf.Lerp(1f, 0.35f, cornerT);
            throttle = Mathf.Clamp01((speedError / 8f)) * throttleScale + 0.15f;
            brake = false;
        }
        else
        {
            // above desired speed => brake more aggressively for sharper turns
            float over = Mathf.Clamp01((-speedError) / 10f);
            float brakeStrength = over * Mathf.Lerp(0.5f, 1.0f, cornerT) * brakeAggression;

            brake = brakeStrength > 0.2f;
            throttle = Mathf.Lerp(0.25f, 0f, brakeStrength);
        }

        car.SetInputs(throttle, steerCmd, brake);

        if (debug && Time.frameCount % 30 == 0)
            Debug.Log($"[PoliceAI] spd={speed:F1} ang={angleAbs:F0} dist={distToAim:F1} desSpd={desiredSpeed:F1} thr={throttle:F2} brk={brake}");

        // -----------------------------
        // STUCK DETECTION / RECOVERY
        // -----------------------------
        bool tryingToMove = !brake && throttle > 0.35f;

        if (tryingToMove && speed < stuckSpeed)
            stuckTimer += Time.fixedDeltaTime;
        else
            stuckTimer = Mathf.Max(0f, stuckTimer - Time.fixedDeltaTime * 0.7f);

        if (stuckTimer >= stuckTime)
        {
            stuckTimer = 0f;
            reverseTimer = reverseTime;
            repathTimer = 0f; // force immediate repath after reversing starts
        }
    }

    Vector3 GetLookaheadPoint(float dist)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
            return target ? target.position : transform.position + transform.forward * dist;

        Vector3 prev = transform.position; prev.y = 0f;
        float remain = dist;

        for (int i = cornerIndex; i < path.corners.Length; i++)
        {
            Vector3 c = path.corners[i]; c.y = 0f;
            float seg = Vector3.Distance(prev, c);

            if (seg >= remain)
            {
                Vector3 p = Vector3.Lerp(prev, c, remain / Mathf.Max(seg, 0.0001f));
                p.y = transform.position.y;
                return p;
            }

            remain -= seg;
            prev = c;
        }

        Vector3 last = path.corners[path.corners.Length - 1];
        last.y = transform.position.y;
        return last;
    }

    public void SetTarget(Transform t)
    {
        target = t;
        repathTimer = 0f;
    }
}
