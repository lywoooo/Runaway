using System;
using System.Collections.Generic;
using UnityEngine;

public class WheelPhysics : MonoBehaviour
{
    private Rigidbody rb;

    public bool isFrontWheel;
    public bool isLeftWheel;

    public bool isGrounded { get; private set; }
    public float compression { get; private set; }
    public float compressionDist { get; private set; }

    public float normalForce { get; private set; }
    public Vector3 contactPoint { get; private set; }
    public Vector3 groundNormal { get; private set; }

    private RaycastHit lastHit;
    private float lastSpringLen;
    private LayerMask drivable;


    [Header("References")]
    [SerializeField] private Transform wheelMesh;

    [Header("Suspension")]
    [SerializeField] private float springStrength = 55000f;
    [SerializeField] private float dampenStrength = 6500f;

    [SerializeField] private float restLen = 0.5f;
    [SerializeField] private float springTravel = 0.2f;
    [SerializeField] private float wheelRadius = 0.3f;

    [Header("Tire")]
    [SerializeField] private float cornerStiffness = 16000f;
    [SerializeField] private float maxLateralMu = 2.4f;
    [SerializeField] private float maxLongitudinalMu = 2.0f;

    [Header("Rolling Resistance")]
    [SerializeField] private float coastDragStiffness = 550f;

    void Start()
    {
        rb = GetComponentInParent<Rigidbody>();
        lastSpringLen = restLen;

        drivable = LayerMask.GetMask("Drivable", "Floor");
    }

    void FixedUpdate()
    {
        float maxLen = restLen + springTravel;

        RaycastHit hit;
        bool rayHit = Physics.Raycast(transform.position, -transform.up, out hit, maxLen + wheelRadius, drivable);

        float visualHitDist = restLen + wheelRadius;

        if (rayHit)
        {
            isGrounded = true;
            lastHit = hit;

            contactPoint = hit.point;
            groundNormal = hit.normal;

            Suspension(hit);
            LateralGrip(hit);

            visualHitDist = hit.distance;
        }
        else
        {
            isGrounded = false;
            compression = 0f;
            compressionDist = 0f;
            normalForce = 0f;
        }

        WheelVisual(visualHitDist);
    }

    void Suspension(RaycastHit hit)
    {
        Vector3 contact = hit.point;

        float springLen = Mathf.Max(0f, hit.distance - wheelRadius);

        compressionDist = Mathf.Clamp(restLen - springLen, 0f, springTravel);

        compression = (springTravel <= 0.0001f) ? 0f : Mathf.Clamp01(compressionDist / springTravel);


        float springVel = (springLen - lastSpringLen) / Time.fixedDeltaTime;
        lastSpringLen = springLen;

        float springForce = springStrength * compressionDist; 
        float damperForce = dampenStrength * springVel;       

        float net = springForce - damperForce;
        net = Mathf.Max(0f, net);

        const float bumpStart = 0.80f;          
        const float bumpStrength = 90000f;      

        if (compression > bumpStart)
        {
            float t = (compression - bumpStart) / (1f - bumpStart); 
            float bumpForce = bumpStrength * t * t;   
            net += bumpForce;
        }

        normalForce = net;
        rb.AddForceAtPosition(transform.up * net, contact);
    }

    void LateralGrip(RaycastHit hit)
    {
        if (normalForce <= 0.01f) return;

        Vector3 contact = hit.point;
        Vector3 n = hit.normal;

        Vector3 v = rb.GetPointVelocity(contact);

        Vector3 wheelFwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
        Vector3 wheelRight = Vector3.ProjectOnPlane(transform.right, n).normalized;

        float forwardVel = Vector3.Dot(v, wheelFwd);
        float lateralVel = Vector3.Dot(v, wheelRight);

        float desiredLatForce = (-lateralVel) * cornerStiffness;

        float slipRatio = Mathf.Abs(lateralVel) / (Mathf.Abs(forwardVel) + 4f);
        float slideT = Mathf.InverseLerp(0.15f, 0.40f, slipRatio);
        float gripMult = Mathf.Lerp(1f, 0.85f, slideT);

        desiredLatForce *= gripMult;

        float maxLatForce = maxLateralMu * normalForce;
        float latForce = Mathf.Clamp(desiredLatForce, -maxLatForce, maxLatForce);

        rb.AddForceAtPosition(wheelRight * latForce, contact);
    }

    public void UpdateSteering(float angleDeg)
    {
        if (!isFrontWheel) return;

        float currentAngle = transform.localEulerAngles.y;
        if (currentAngle > 180f) currentAngle -= 360f;

        float newAngle = Mathf.MoveTowards(currentAngle, angleDeg, 250f * Time.fixedDeltaTime);
        transform.localRotation = Quaternion.Euler(0f, newAngle, 0f);
    }

    public void AccelForce(float accelInput, float accelForce)
    {
        if (!isGrounded) return;
        if (normalForce <= 0.01f) return;

        Vector3 n = lastHit.normal;
        Vector3 contact = lastHit.point;

        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;

        float drive = accelForce * Mathf.Clamp(accelInput, -1f, 1f);
        Vector3 rawDrive = fwd * drive;

        float maxLongForce = maxLongitudinalMu * normalForce;
        Vector3 driveForce = Vector3.ClampMagnitude(rawDrive, maxLongForce);

        rb.AddForceAtPosition(driveForce, contact);
    }

    public void Brake(float brakeInput, float brakeStrength)
    {
        if (!isGrounded) return;
        if (normalForce <= 0.01f) return;

        Vector3 n = lastHit.normal;
        Vector3 contact = lastHit.point;

        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
        float forwardVel = Vector3.Dot(rb.GetPointVelocity(contact), fwd);

        float desired = (-forwardVel) * brakeStrength * Mathf.Clamp01(brakeInput);

        float maxLongForce = maxLongitudinalMu * normalForce;
        float clamped = Mathf.Clamp(desired, -maxLongForce, maxLongForce);

        rb.AddForceAtPosition(fwd * clamped, contact);
    }

    public void CoastDrag()
    {
        if (!isGrounded) return;
        if (normalForce <= 0.01f) return;

        Vector3 n = lastHit.normal;
        Vector3 contact = lastHit.point;

        Vector3 fwd = Vector3.ProjectOnPlane(transform.forward, n).normalized;
        float forwardVel = Vector3.Dot(rb.GetPointVelocity(contact), fwd);

        float desired = (-forwardVel) * coastDragStiffness;

        float maxLongForce = maxLongitudinalMu * normalForce;
        float clamped = Mathf.Clamp(desired, -maxLongForce, maxLongForce);

        rb.AddForceAtPosition(fwd * clamped, contact);
    }

    void WheelVisual(float hitDist)
    {
        if (!wheelMesh) return;

        float springLen = Mathf.Max(0f, hitDist - wheelRadius);

        float minLen = restLen - springTravel;
        float maxLen = restLen + springTravel;

        springLen = Mathf.Clamp(springLen, minLen, maxLen);

        Vector3 localPos = wheelMesh.localPosition;
        localPos.y = -springLen;
        wheelMesh.localPosition = localPos;
    }
}
