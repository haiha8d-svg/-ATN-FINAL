//using System.Net.Sockets;
//using System.Net;
//using System.Text;
//using System;
//using UnityEngine;
//using UnityEngine.InputSystem;
//public class BikeController : MonoBehaviour
//{

//    UdpClient udpClient;
//    int port = 5005;
//    string lastMessage = "";

//    [System.Serializable]
//    public class SensorData
//    {
//        public float yaw;
//        public float speed;
//    }

//    [Header("Movement")]
//    public float acceleration = 5f;
//    public float maxSpeed = 20f;
//    private float currentSpeed = 0f;
//    [Header("Debug Keyboard Control")]
//    public bool useKeyboardControl = true;
    
//    private float moveInput = 0f;   // W
    
//    [Header("Steering")]
//    public float steerStrength = 5f;
//    public float turnSpeed = 50f;
//    public float leanAngle = 15f;
//    public float leanSpeed = 5f;

//    [Header("Upright Stabilization")]
//    public float uprightStrength = 5f;

//    [Header("References")]
//    public Transform bikeModel;

//    private Rigidbody rb;
//    public float steerResponsiveness = 4f;   // càng lớn càng bắt lái nhanh
//    public float steerReturnSpeed = 6f;      // tốc độ trả lái về giữa

//    private float rawSteerInput = 0f;        // input A/D tức thời
//    private float smoothSteerInput = 0f;

//    void Start()
//    {
//    if (!useKeyboardControl)
//    {
//        udpClient = new UdpClient(port);
//        udpClient.BeginReceive(new AsyncCallback(ReceiveData), null);
//    }

//    rb = GetComponent<Rigidbody>();
//    rb.constraints = RigidbodyConstraints.FreezeRotationX;
//    rb.centerOfMass = new Vector3(0, -0.5f, 0);
//    rb.angularDamping = 2f;

//    rb.MoveRotation(transform.rotation);
//    }
//    void Update()
//    {
//        if (useKeyboardControl)
//        {
//            ReadKeyboardInput();
//        }

//        if (Mathf.Abs(rawSteerInput) > 0.01f)
//        {
//            smoothSteerInput = Mathf.MoveTowards(
//                smoothSteerInput,
//                rawSteerInput,
//                steerResponsiveness * Time.deltaTime
//            );
//        }
//        else
//        {
//            smoothSteerInput = Mathf.MoveTowards(
//                smoothSteerInput,
//                0f,
//                steerReturnSpeed * Time.deltaTime
//            );
//        }
//    }

//    void ReadKeyboardInput()
//    {
//        var kb = Keyboard.current;

//        if (kb == null)
//        {
//            moveInput = 0f;
//            rawSteerInput = 0f;
//            return;
//        }

//        moveInput = kb.wKey.isPressed ? 1f : 0f;

//        if (kb.aKey.isPressed && !kb.dKey.isPressed)
//            rawSteerInput = -1f;
//        else if (kb.dKey.isPressed && !kb.aKey.isPressed)
//            rawSteerInput = 1f;
//        else
//            rawSteerInput = 0f;
//    }
//    void FixedUpdate()
//    {
//        // Chờ đếm ngược 3-2-1
//        if (GameManager.Instance != null && !GameManager.Instance.raceStarted)
//        {
//            rb.linearVelocity = Vector3.zero;
//            return;
//        }

//        float targetSpeed = moveInput * maxSpeed;
//        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.fixedDeltaTime);

//        Vector3 forwardMove = -transform.forward * currentSpeed;
//        rb.linearVelocity = new Vector3(forwardMove.x, rb.linearVelocity.y, forwardMove.z);

//        if (Mathf.Abs(smoothSteerInput) > 0.01f && currentSpeed > 0.1f)
//        {
//            float turnAmount = smoothSteerInput * turnSpeed * Time.fixedDeltaTime;
//            rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, turnAmount, 0f));
//        }

//        if (bikeModel != null)
//        {
//            float targetLean = -smoothSteerInput * leanAngle;
//            Quaternion targetRot = Quaternion.Euler(0f, 0f, targetLean);
//            bikeModel.localRotation = Quaternion.Lerp(
//                bikeModel.localRotation,
//                targetRot,
//                leanSpeed * Time.fixedDeltaTime
//            );
//        }
//    }
//    void ReceiveData(IAsyncResult result)
//    {
//        if (useKeyboardControl) return;
//        IPEndPoint source = new IPEndPoint(IPAddress.Any, port);
//        byte[] data = udpClient.EndReceive(result, ref source);
//        lastMessage = Encoding.UTF8.GetString(data);
//        udpClient.BeginReceive(new AsyncCallback(ReceiveData), null);
//    }

////    void Movement()
////    {
////        //rb.velocity = Vector3.Lerp(rb.velocity, -maxSpeed * transform.foward, Time.fixedDeltaTime * acceleration);
////        rb.linearVelocity = Vector3.Lerp(speed, -maxSpeed * transform.foward);
////        Debug.Log("Speed: " + speed); 
////        //Debug.Log("Speed: " + speed); 
////}

////    void Rotation()
////    {
////        transform.Rotate(0, steerInput * moveInput * steerStrength * Time.fixedDeltaTime, 0, Space.World);
////    }
    

//    // void FixedUpdate()
//    // {
//    // if (!string.IsNullOrEmpty(lastMessage))
//    // {
//    //     try
//    //     {
//    //         SensorData sensor = JsonUtility.FromJson<SensorData>(lastMessage);
//    //         //Movement();
//    //         //Rotation();
//    //         }
//    //     catch
//    //     {
//    //         Debug.LogWarning("Failed UDP message: " + lastMessage);
//    //     }
//    // }

//    //     lastMessage = "";

//    //     //HandleSpeedControl();
//    //     //HandleMovement();
//    //     //HandleSteering();
//    //     //KeepUpright();
//    //     //HandleLeaning();
//    // }

//    void OnApplicationQuit()
//{
//    if (udpClient != null)
//    {
//        udpClient.Close();
//        udpClient = null;
//    }
//}



//    //void HandleSpeedControl()
//    //{
//    //    if (Input.GetKey(KeyCode.W))
//    //        currentSpeed += acceleration * Time.fixedDeltaTime;

//    //    if (Input.GetKey(KeyCode.S))
//    //        currentSpeed -= deceleration * Time.fixedDeltaTime;

//    //    currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
//    //}

//    //void HandleMovement()
//    //{
//    //    if (Input.GetKey(KeyCode.Space))
//    //        rb.AddForce(-transform.forward * currentSpeed, ForceMode.Acceleration);
//    //}

//    //void HandleSteering()
//    //{
//    //    float turn = Input.GetAxis("Horizontal");
//    //    rb.AddTorque(Vector3.up * turn * turnSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
//    //}

//    //void KeepUpright()
//    //{
//    //    Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
//    //    Vector3 torque = new Vector3(targetRotation.x, targetRotation.y, targetRotation.z) * uprightStrength;
//    //    rb.AddTorque(torque);
//    //}

//    //void HandleLeaning()
//    //{
//    //    float turn = Input.GetAxis("Horizontal");
//    //    float targetLean = turn * leanAngle * (currentSpeed / maxSpeed);

//    //    Quaternion targetRotation = Quaternion.Euler(
//    //        0,
//    //        bikeModel.localEulerAngles.y,
//    //        targetLean
//    //    );

//    //    bikeModel.localRotation = Quaternion.Lerp(
//    //        bikeModel.localRotation,
//    //        targetRotation,
//    //        Time.deltaTime * leanSpeed
//    //    );
//    //}
//}