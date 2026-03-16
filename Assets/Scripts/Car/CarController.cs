using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rb;
    [SerializeField] private WheelPhysics[] wheels;
    public bool isAIControlled = false;

    [Header("Acceleration")]
    [SerializeField] private float acceleration = 12000f;   
    [SerializeField] private float brakeForce = 8000f;      
    [SerializeField] private float maxSpeed = 55f;         

    [Header("Steering")]
    [SerializeField] private float maxSteeringAngle = 35f; 
    [SerializeField] private float steerAtMaxSpeed = 20f;  
    private float trackWidth;
    private float wheelBase;

    [Header("Stability")]
    [SerializeField] private float downforce = 70f;         
    [SerializeField] private float rollStabilize = 12000f; 
    [SerializeField] private float yawDamping = 2.0f;      

    [Header("Anti-Roll")]
    [SerializeField] private float frontAntiRoll = 22000f;
    [SerializeField] private float rearAntiRoll = 20000f;

    [HideInInspector] public float throttleInput;
    [HideInInspector] public float steerInput;
    [HideInInspector] public bool brakeInput;

    private WheelPhysics FL, FR, BL, BR;

    void Start()
    {
        if (!rb) rb = GetComponent<Rigidbody>();

        // Major anti-lean lever
        rb.centerOfMass = new Vector3(0f, -0.035f, 0f);

        CalculateDimensions();
    }

    void CalculateDimensions()
    {
        WheelPhysics frontMost = null, rearMost = null;
        WheelPhysics leftMost = null, rightMost = null;

        foreach (var w in wheels)
        {
            if (!w) continue;

            Vector3 lp = transform.InverseTransformPoint(w.transform.position);

            if (frontMost == null || lp.z > transform.InverseTransformPoint(frontMost.transform.position).z) frontMost = w;
            if (rearMost == null || lp.z < transform.InverseTransformPoint(rearMost.transform.position).z) rearMost = w;
            if (leftMost == null || lp.x < transform.InverseTransformPoint(leftMost.transform.position).x) leftMost = w;
            if (rightMost == null || lp.x > transform.InverseTransformPoint(rightMost.transform.position).x) rightMost = w;

            if (w.isFrontWheel && w.isLeftWheel) FL = w;
            if (w.isFrontWheel && !w.isLeftWheel) FR = w;
            if (!w.isFrontWheel && w.isLeftWheel) BL = w;
            if (!w.isFrontWheel && !w.isLeftWheel) BR = w;
        }

        if (frontMost && rearMost && leftMost && rightMost)
        {
            wheelBase = Mathf.Abs(transform.InverseTransformPoint(frontMost.transform.position).z -
                                  transform.InverseTransformPoint(rearMost.transform.position).z);

            trackWidth = Mathf.Abs(transform.InverseTransformPoint(leftMost.transform.position).x -
                                   transform.InverseTransformPoint(rightMost.transform.position).x);
        }
        else
        {
            wheelBase = 2f;
            trackWidth = 1.2f;
        }
    }

    void FixedUpdate()
    {
        if (!rb) return;

        float speed = rb.velocity.magnitude;

        if (speed > maxSpeed)
            rb.velocity = rb.velocity.normalized * maxSpeed;

        float speed01 = Mathf.Clamp01(speed / Mathf.Max(0.1f, maxSpeed));
        float currentMaxSteer = Mathf.Lerp(maxSteeringAngle, steerAtMaxSpeed, speed01);

        float steerAngleLeft = 0f;
        float steerAngleRight = 0f;

        if (Mathf.Abs(steerInput) > 0.001f)
        {
            float steerAngle = Mathf.Abs(steerInput) * currentMaxSteer;
            float steerRad = steerAngle * Mathf.Deg2Rad;

            float tan = Mathf.Tan(steerRad);
            tan = Mathf.Clamp(tan, 0.0001f, 9999f);

            float radius = wheelBase / tan;

            float inner = Mathf.Atan(wheelBase / (radius - trackWidth * 0.5f)) * Mathf.Rad2Deg;
            float outer = Mathf.Atan(wheelBase / (radius + trackWidth * 0.5f)) * Mathf.Rad2Deg;

            if (steerInput > 0f) // right
            {
                steerAngleLeft = outer;
                steerAngleRight = inner;
            }
            else // left
            {
                steerAngleLeft = -inner;
                steerAngleRight = -outer;
            }
        }

        float forwardSpeed = Vector3.Dot(transform.forward, rb.velocity);

        foreach (var wheel in wheels)
        {
            if (!wheel) continue;

            if (wheel.isFrontWheel)
            {
                float angle = wheel.isLeftWheel ? steerAngleLeft : steerAngleRight;
                wheel.UpdateSteering(angle);
            }

            if (brakeInput)
            {
                wheel.Brake(1f, brakeForce);
            }
            else
            {
                float reverseLimit = -maxSpeed * 0.5f;

                if (Mathf.Abs(throttleInput) > 0.001f &&
                    (throttleInput > 0f ? forwardSpeed < maxSpeed : forwardSpeed > reverseLimit))
                {
                    wheel.AccelForce(throttleInput, acceleration);
                }
                else if (!isAIControlled)
                {
                    wheel.CoastDrag();
                }
            }
        }

        AntiRoll();

        rb.AddForce(-transform.up * downforce * speed, ForceMode.Force);

        Vector3 torqueAxis = Vector3.Cross(transform.up, Vector3.up);
        rb.AddTorque(torqueAxis * rollStabilize, ForceMode.Force);

        Vector3 ang = rb.angularVelocity;
        ang.y = ang.y / (1f + yawDamping * Time.fixedDeltaTime);
        rb.angularVelocity = ang;
    }

    void AntiRoll()
    {
        AxleAntiRoll(FL, FR, frontAntiRoll);
        AxleAntiRoll(BL, BR, rearAntiRoll);
    }

    void AxleAntiRoll(WheelPhysics left, WheelPhysics right, float strength)
    {
        if (!left || !right) return;

        bool leftGround = left.isGrounded;
        bool rightGround = right.isGrounded;
        if (!leftGround && !rightGround) return;

        float travelL = leftGround ? left.compressionDist : 0f;
        float travelR = rightGround ? right.compressionDist : 0f;

        float diff = travelL - travelR; // + if left more compressed
        float antiRollForce = diff * strength;


        float maxForcePerSide = rb.mass * Physics.gravity.magnitude * 0.75f;
        antiRollForce = Mathf.Clamp(antiRollForce, -maxForcePerSide, maxForcePerSide);

        Vector3 up = transform.up;

        if (leftGround)
            rb.AddForceAtPosition(up * antiRollForce, left.contactPoint);

        if (rightGround)
            rb.AddForceAtPosition(-up * antiRollForce, right.contactPoint);
    }

    public void SetInputs(float throttle, float steer, bool brake)
    {
        throttleInput = Mathf.Clamp(throttle, -1f, 1f);
        steerInput = Mathf.Clamp(steer, -1f, 1f);
        brakeInput = brake;
    }

}
