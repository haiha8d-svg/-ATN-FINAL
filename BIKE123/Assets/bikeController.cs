using System.Net.Sockets;
using System.Net;
using System.Text;
using System;
using UnityEngine;

public class BikeController : MonoBehaviour
{
    float moveInput, steerInput;

    UdpClient udpClient;
    int port = 5005;
    string lastMessage = "";

    [System.Serializable]
    public class SensorData
    {
        public float yaw;
        public float speed;
    }

    [Header("Movement")]
    public float acceleration = 5f;
    public float maxSpeed = 20f;
    private float currentSpeed = 0f;

    [Header("Steering")]
    public float steerStrength = 5f;
    public float turnSpeed = 50f;
    public float leanAngle = 15f;
    public float leanSpeed = 5f;

    [Header("Upright Stabilization")]
    public float uprightStrength = 5f;

    [Header("References")]
    public Transform bikeModel;

    private Rigidbody rb;


    void Start()
    {
        udpClient = new UdpClient(port);
        udpClient.BeginReceive(new AsyncCallback(ReceiveData), null);

        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.angularDamping = 2f;


        rb.MoveRotation(transform.rotation);

    }

    void ReceiveData(IAsyncResult result)
    {
        IPEndPoint source = new IPEndPoint(IPAddress.Any, port);
        byte[] data = udpClient.EndReceive(result, ref source);
        lastMessage = Encoding.UTF8.GetString(data);
        udpClient.BeginReceive(new AsyncCallback(ReceiveData), null);
    }

//    void Movement()
//    {
//        //rb.velocity = Vector3.Lerp(rb.velocity, -maxSpeed * transform.foward, Time.fixedDeltaTime * acceleration);
//        rb.linearVelocity = Vector3.Lerp(speed, -maxSpeed * transform.foward);
//        Debug.Log("Speed: " + speed); 
//        //Debug.Log("Speed: " + speed); 
//}

//    void Rotation()
//    {
//        transform.Rotate(0, steerInput * moveInput * steerStrength * Time.fixedDeltaTime, 0, Space.World);
//    }

    void FixedUpdate()
    {
    if (!string.IsNullOrEmpty(lastMessage))
    {
        try
        {
            SensorData sensor = JsonUtility.FromJson<SensorData>(lastMessage);
            //Movement();
            //Rotation();
            }
        catch
        {
            Debug.LogWarning("Failed UDP message: " + lastMessage);
        }
    }

        lastMessage = "";

        //HandleSpeedControl();
        //HandleMovement();
        //HandleSteering();
        //KeepUpright();
        //HandleLeaning();
    }

    void OnApplicationQuit()
    {
        udpClient.Close();
    }



    //void HandleSpeedControl()
    //{
    //    if (Input.GetKey(KeyCode.W))
    //        currentSpeed += acceleration * Time.fixedDeltaTime;

    //    if (Input.GetKey(KeyCode.S))
    //        currentSpeed -= deceleration * Time.fixedDeltaTime;

    //    currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, maxSpeed);
    //}

    //void HandleMovement()
    //{
    //    if (Input.GetKey(KeyCode.Space))
    //        rb.AddForce(-transform.forward * currentSpeed, ForceMode.Acceleration);
    //}

    //void HandleSteering()
    //{
    //    float turn = Input.GetAxis("Horizontal");
    //    rb.AddTorque(Vector3.up * turn * turnSpeed * Time.fixedDeltaTime, ForceMode.VelocityChange);
    //}

    //void KeepUpright()
    //{
    //    Quaternion targetRotation = Quaternion.FromToRotation(transform.up, Vector3.up);
    //    Vector3 torque = new Vector3(targetRotation.x, targetRotation.y, targetRotation.z) * uprightStrength;
    //    rb.AddTorque(torque);
    //}

    //void HandleLeaning()
    //{
    //    float turn = Input.GetAxis("Horizontal");
    //    float targetLean = turn * leanAngle * (currentSpeed / maxSpeed);

    //    Quaternion targetRotation = Quaternion.Euler(
    //        0,
    //        bikeModel.localEulerAngles.y,
    //        targetLean
    //    );

    //    bikeModel.localRotation = Quaternion.Lerp(
    //        bikeModel.localRotation,
    //        targetRotation,
    //        Time.deltaTime * leanSpeed
    //    );
    //}
}