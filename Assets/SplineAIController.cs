using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[RequireComponent(typeof(Rigidbody))]
public class SplineAIController : MonoBehaviour
{
    [Header("Cấu hình Spline")]
    public SplineContainer splineContainer;

    [Header("Thuộc tính AI")]
    public float targetSpeed = 15f;
    public float acceleration = 5f;
    private float currentSpeed = 0f;
    public bool loop = true;

    [Header("Điều khiển chuyển động")]
    public float rotationSpeed = 8f;
    public float snapThreshold = 1.5f;

    [Header("Hiệu ứng rẽ thân (Lean)")]
    public Transform bikeModel;
    public float leanAngle = 20f;
    public float leanSpeed = 5f;

    [Header("Hiệu ứng vặn cổ tay lái (Steering)")]
    public Transform frontFork;
    public float maxSteeringAngle = 35f;
    public float steeringSpeed = 8f;

    private Rigidbody rb;
    private float currentSplineT = 0f;

    private Quaternion initialBikeModelRot;
    private Quaternion initialFrontForkRot;

    [Header("Ground Stick")]
    public float groundCheckDistance = 2f;
    public float groundStickForce = 50f;
    public float groundOffset = 0f;
    public LayerMask groundLayer;

    [Header("Wheel Rotation")]
    public Transform frontWheel;
    public Transform rearWheel;
    public float wheelRadius = 0.35f;

    [Header("Direction")]
    public bool reverseDirection = false;
    [Tooltip("Tích nếu model bị quay ngược 180 độ")]
    public bool flipModel180 = false;

    void Start()
    {
        if (PlayerPrefs.HasKey("BotTargetSpeed"))
            targetSpeed = PlayerPrefs.GetFloat("BotTargetSpeed") / 3.6f; // km/h → m/s

        rb = GetComponent<Rigidbody>();

        if (bikeModel != null) initialBikeModelRot = bikeModel.localRotation;
        if (frontFork != null) initialFrontForkRot = frontFork.localRotation;

        if (splineContainer == null)
        {
            Debug.LogError("SplineAIController: Chưa gán SplineContainer!");
            enabled = false;
            return;
        }

        FindNearestPointOnSpline();
    }

    void FixedUpdate()
    {
        if (splineContainer == null) return;

        if (GameManager.Instance != null && !GameManager.Instance.raceStarted)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        if (currentSpeed < 1f) currentSpeed = 1f;

        float dynamicAccel = Mathf.Lerp(0.5f, acceleration, currentSpeed / targetSpeed);
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, dynamicAccel * Time.fixedDeltaTime);

        // ═══════════════════════════════════════════
        // BƯỚC 1: Tìm điểm gần nhất trên Spline
        // ═══════════════════════════════════════════
        float3 nearestPointF3;
        float actualT;
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            // ✅ FIX: Chuyển về local space của SplineContainer trước khi tính
            splineContainer.transform.InverseTransformPoint(rb.position),
            out nearestPointF3,
            out actualT
        );
        currentSplineT = actualT;

        // ✅ FIX: Chuyển ngược về world space
        Vector3 nearestWorld = splineContainer.transform.TransformPoint((Vector3)nearestPointF3);

        // ═══════════════════════════════════════════
        // BƯỚC 2: Tính điểm look-ahead trên Spline
        // ═══════════════════════════════════════════
        float splineLength = splineContainer.CalculateLength();
        float lookAheadDistance = Mathf.Max(4f, currentSpeed * 0.4f);
        float deltaT = lookAheadDistance / splineLength;

        float targetT = actualT + (reverseDirection ? -deltaT : deltaT);
        if (loop)
        {
            if (targetT > 1f) targetT -= 1f;
            if (targetT < 0f) targetT += 1f;
        }
        else targetT = Mathf.Clamp01(targetT);

        // ✅ FIX: Evaluate trong local space rồi transform ra world
        splineContainer.Spline.Evaluate(targetT, out float3 localPos, out float3 localTan, out float3 localUp);
        Vector3 targetPosition = splineContainer.transform.TransformPoint((Vector3)localPos);
        Vector3 forwardDir = splineContainer.transform.TransformDirection((Vector3)localTan).normalized;

        forwardDir.y = 0;
        forwardDir.Normalize();

        // ✅ FIX CHÍNH: actualForwardDir là hướng XE ĐI (không bị ảnh hưởng flipModel180)
        // flipModel180 CHỈ ảnh hưởng visual, không ảnh hưởng hướng di chuyển
        Vector3 actualForwardDir = reverseDirection ? -forwardDir : forwardDir;

        // ═══════════════════════════════════════════
        // BƯỚC 3: Tính velocity bám Spline
        // ═══════════════════════════════════════════
        Vector3 rbFlat = new Vector3(rb.position.x, 0, rb.position.z);
        Vector3 nearestFlat = new Vector3(nearestWorld.x, 0, nearestWorld.z);
        Vector3 toNearest = nearestFlat - rbFlat;
        float distanceFromSpline = toNearest.magnitude;

        Vector3 finalVelocity;

        if (distanceFromSpline > snapThreshold)
        {
            // LẠC ĐƯỜNG: kéo thẳng về Spline
            float snapSpeed = Mathf.Clamp(distanceFromSpline * 6f, currentSpeed * 0.6f, currentSpeed * 1.2f);
            finalVelocity = toNearest.normalized * snapSpeed;

            // Debug màu đỏ khi lạc đường
            Debug.DrawLine(rb.position, nearestWorld, Color.red);
        }
        else
        {
            // BÁM ĐƯỜNG: blend hướng Spline + kéo nhẹ về tâm
            float blendWeight = Mathf.Clamp01(distanceFromSpline / snapThreshold);
            Vector3 combined = (actualForwardDir * 0.4f + toNearest.normalized * 0.6f).normalized;
            Vector3 blendedDir = Vector3.Lerp(actualForwardDir, combined, blendWeight).normalized;
            finalVelocity = blendedDir * currentSpeed;

            // Debug màu xanh khi bám đường tốt
            Debug.DrawLine(rb.position, nearestWorld, Color.green);
        }

        // Debug hướng xe đang đi
        Debug.DrawRay(rb.position, actualForwardDir * 3f, Color.blue);
        Debug.DrawRay(rb.position, finalVelocity.normalized * 3f, Color.yellow);

        // ═══════════════════════════════════════════
        // BƯỚC 4: Ground Stick & Rotation
        // ═══════════════════════════════════════════

        // ✅ FIX: visualForwardDir CHỈ dùng để xoay model, KHÔNG dùng để tính velocity
        Vector3 visualForwardDir = flipModel180 ? -actualForwardDir : actualForwardDir;
        Vector3 actualTransformForward = flipModel180 ? -transform.forward : transform.forward;

        Quaternion targetRotation = rb.rotation;
        if (visualForwardDir.sqrMagnitude > 0.001f)
            targetRotation = Quaternion.LookRotation(visualForwardDir, Vector3.up);

        float targetYVelocity = rb.linearVelocity.y;
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down,
            out hit, groundCheckDistance, groundLayer))
        {
            float yError = (hit.point.y + groundOffset) - rb.position.y;
            targetYVelocity = Mathf.Clamp(yError * groundStickForce, -15f, 15f);

            Vector3 forwardProjected = Vector3.ProjectOnPlane(visualForwardDir, hit.normal).normalized;
            if (forwardProjected.sqrMagnitude > 0.001f)
                targetRotation = Quaternion.LookRotation(forwardProjected, hit.normal);
        }

        rb.linearVelocity = new Vector3(finalVelocity.x, targetYVelocity, finalVelocity.z);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));

        // ═══════════════════════════════════════════
        // BƯỚC 5: Lean & Steering
        // ═══════════════════════════════════════════
        float lookAheadT = reverseDirection ? currentSplineT - 0.015f : currentSplineT + 0.015f;
        if (loop)
        {
            if (lookAheadT > 1f) lookAheadT -= 1f;
            if (lookAheadT < 0f) lookAheadT += 1f;
        }
        else lookAheadT = Mathf.Clamp01(lookAheadT);

        splineContainer.Spline.Evaluate(lookAheadT, out float3 fPos, out float3 fTan, out float3 fUp);
        Vector3 futureDir = splineContainer.transform.TransformDirection((Vector3)fTan).normalized;
        if (reverseDirection) futureDir = -futureDir;
        futureDir.y = 0;
        futureDir.Normalize();

        float curveAmount = Vector3.Cross(actualTransformForward, futureDir).y;

        if (bikeModel != null)
        {
            float targetLean = -Mathf.Clamp(curveAmount * 2f, -1f, 1f) * leanAngle;
            bikeModel.localRotation = Quaternion.Lerp(
                bikeModel.localRotation,
                initialBikeModelRot * Quaternion.Euler(0f, 0f, targetLean),
                leanSpeed * Time.fixedDeltaTime
            );
        }

        if (frontFork != null)
        {
            float angleDiff = Vector3.SignedAngle(actualTransformForward, futureDir, Vector3.up);
            float targetSteer = Mathf.Clamp(angleDiff * 1.5f, -maxSteeringAngle, maxSteeringAngle);
            frontFork.localRotation = Quaternion.Lerp(
                frontFork.localRotation,
                initialFrontForkRot * Quaternion.Euler(0f, targetSteer, 0f),
                steeringSpeed * Time.fixedDeltaTime
            );
        }

        float rotationAmount = (currentSpeed / (2f * Mathf.PI * wheelRadius)) * 360f * Time.fixedDeltaTime;
        if (frontWheel != null) frontWheel.Rotate(Vector3.right, rotationAmount, Space.Self);
        if (rearWheel != null) rearWheel.Rotate(Vector3.right, rotationAmount, Space.Self);
    }

    void FindNearestPointOnSpline()
    {
        float3 nearestPoint;
        float t;
        // ✅ FIX: Dùng local space
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            splineContainer.transform.InverseTransformPoint(transform.position),
            out nearestPoint,
            out t
        );
        currentSplineT = t;
    }
}