using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

/// <summary>
/// Điều khiển xe đạp chạy theo Spline kết hợp Physics (Rigidbody).
/// Xe sẽ tự động đi theo đường Spline, người chơi có thể dùng W để tăng tốc,
/// A/D để bẻ lái lệch khỏi đường spline.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class SplineBikeController : MonoBehaviour
{
    [Header("Spline Reference")]
    [Tooltip("Kéo thả GameObject có Spline Container vào đây")]
    public SplineContainer splineContainer;

    [Header("Movement")]
    public float maxSpeed = 20f;
    public float acceleration = 5f;
    private float currentSpeed = 0f;

    [Header("Spline Following")]
    [Tooltip("Khoảng cách nhìn trước trên spline để xoay hướng")]
    public float lookAheadDistance = 5f;
    [Tooltip("Tốc độ xoay hướng theo spline")]
    public float rotationSpeed = 5f;
    [Tooltip("Tốc độ kéo xe về lại đường spline khi bị lệch")]
    public float returnToSplineSpeed = 2f;
    [Tooltip("Khoảng cách tối đa cho phép lệch khỏi spline")]
    public float maxOffsetDistance = 3f;

    [Header("Steering (Tùy chọn - dùng bàn phím)")]
    [Tooltip("Bật để dùng A/D bẻ lái")]
    public bool allowManualSteering = true;
    public float steerStrength = 3f;

    [Header("Lean / Nghiêng xe")]
    public Transform bikeModel;
    public float leanAngle = 15f;
    public float leanSpeed = 5f;

    [Header("Loop")]
    [Tooltip("Lặp lại khi đến cuối spline")]
    public bool loop = true;

    private Rigidbody rb;
    private float currentSplineT = 0f; // Vị trí hiện tại trên spline (0 → 1)
    private float steerInput = 0f;
    private float lateralOffset = 0f;  // Độ lệch ngang hiện tại

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.angularDamping = 2f;

        if (splineContainer == null)
        {
            Debug.LogError("SplineBikeController: Chưa gắn Spline Container! Kéo thả Spline vào Inspector.");
            enabled = false;
            return;
        }

        // Tìm vị trí gần nhất trên spline so với vị trí hiện tại của xe
        FindNearestPointOnSpline();
    }

    void Update()
    {
        if (allowManualSteering)
        {
            // Đọc input bàn phím
            steerInput = 0f;
            if (Input.GetKey(KeyCode.A)) steerInput = -1f;
            if (Input.GetKey(KeyCode.D)) steerInput = 1f;
        }
    }

    void FixedUpdate()
    {
        if (splineContainer == null) return;

        // --- 1. Tăng/giảm tốc ---
        float moveInput = Input.GetKey(KeyCode.W) ? 1f : 0f;
        float targetSpeed = moveInput * maxSpeed;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

        // --- 2. Cập nhật vị trí trên spline (t) dựa theo tốc độ ---
        if (currentSpeed > 0.01f)
        {
            float splineLength = splineContainer.CalculateLength();
            float deltaT = (currentSpeed * Time.fixedDeltaTime) / splineLength;
            currentSplineT += deltaT;

            if (loop)
            {
                if (currentSplineT > 1f) currentSplineT -= 1f;
            }
            else
            {
                currentSplineT = Mathf.Clamp01(currentSplineT);
            }
        }

        // --- 3. Lấy vị trí & hướng trên spline ---
        float3 splinePosition;
        float3 splineTangent;
        float3 splineUp;
        splineContainer.Evaluate(currentSplineT, out splinePosition, out splineTangent, out splineUp);

        Vector3 targetPosition = (Vector3)splinePosition;
        Vector3 forwardDir = ((Vector3)splineTangent).normalized;

        // --- 4. Xử lý lệch ngang (steering) ---
        if (allowManualSteering && Mathf.Abs(steerInput) > 0.01f)
        {
            lateralOffset += steerInput * steerStrength * Time.fixedDeltaTime;
            lateralOffset = Mathf.Clamp(lateralOffset, -maxOffsetDistance, maxOffsetDistance);
        }
        else
        {
            // Tự động kéo về lại đường spline
            lateralOffset = Mathf.MoveTowards(lateralOffset, 0f, returnToSplineSpeed * Time.fixedDeltaTime);
        }

        // Tính hướng ngang (perpendicular) so với spline
        Vector3 right = Vector3.Cross(Vector3.up, forwardDir).normalized;
        Vector3 offsetTargetPos = targetPosition + right * lateralOffset;

        // --- 5. Di chuyển xe ---
        // Giữ Y hiện tại (để Rigidbody + gravity xử lý)
        offsetTargetPos.y = rb.position.y;

        // Lực kéo về vị trí trên spline
        Vector3 toTarget = offsetTargetPos - rb.position;
        Vector3 lateralCorrection = toTarget * returnToSplineSpeed;

        // Velocity = forward (theo spline) + lateral correction
        Vector3 forwardVelocity = -forwardDir * currentSpeed;
        // Nếu xe đạp hướng -Z thì dùng -forwardDir, nếu +Z thì dùng forwardDir
        // Thử đổi dấu nếu xe chạy ngược

        rb.linearVelocity = new Vector3(
            forwardVelocity.x + lateralCorrection.x,
            rb.linearVelocity.y,
            forwardVelocity.z + lateralCorrection.z
        );

        // --- 6. Xoay xe theo hướng đi ---
        if (forwardDir.sqrMagnitude > 0.001f)
        {
            // Xe đạp hướng -Z nên cần xoay ngược
            Quaternion targetRotation = Quaternion.LookRotation(-forwardDir, Vector3.up);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
        }

        // --- 7. Nghiêng xe (lean) ---
        if (bikeModel != null)
        {
            float targetLean = -steerInput * leanAngle;
            Quaternion leanRot = Quaternion.Euler(0f, 0f, targetLean);
            bikeModel.localRotation = Quaternion.Lerp(
                bikeModel.localRotation,
                leanRot,
                leanSpeed * Time.fixedDeltaTime
            );
        }
    }

    /// <summary>
    /// Tìm vị trí gần nhất trên spline so với vị trí hiện tại của xe.
    /// Gọi 1 lần khi Start.
    /// </summary>
    void FindNearestPointOnSpline()
    {
        float3 nearestPoint;
        float t;
        SplineUtility.GetNearestPoint(
            splineContainer.Spline,
            (float3)transform.position,
            out nearestPoint,
            out t
        );
        currentSplineT = t;
    }

    /// <summary>
    /// Vẽ đường debug trong Scene View
    /// </summary>
    void OnDrawGizmos()
    {
        if (splineContainer == null) return;

        // Vẽ vị trí hiện tại trên spline
        float3 pos, tan, up;
        splineContainer.Evaluate(currentSplineT, out pos, out tan, out up);

        Gizmos.color = Color.green;
        Gizmos.DrawSphere((Vector3)pos, 0.3f);

        // Vẽ hướng đi
        Gizmos.color = Color.blue;
        Gizmos.DrawRay((Vector3)pos, ((Vector3)tan).normalized * 3f);
    }
}
