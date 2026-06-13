using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

public class BikeController : MonoBehaviour
{
    private UdpClient udpClient;
    private IPEndPoint sendEndPoint;

    [Header("Network Settings")]
    public string ipAddress = "127.0.0.1";
    public int receivePort = 5005;
    public int sendPort = 5006;

    [Header("Communication Timers")]
    public float sendRate = 0.05f;
    private float nextSendTime = 0f;

    private readonly object dataLock = new object();
    private string lockedMessage = "";

    [System.Serializable]
    public class SensorData
    {
        public float yaw;
        public float speed;
    }

    private SensorData sensor = new SensorData();

    [Header("Movement")]
    public float scale = 0.2f;

    [Header("References")]
    public Transform bikeModel;
    private Rigidbody rb;
    public float currentSlopeAngle = 0f;

    void Start()
    {
        udpClient = new UdpClient(receivePort);
        udpClient.BeginReceive(new AsyncCallback(ReceiveData), null);

        sendEndPoint = new IPEndPoint(IPAddress.Parse(ipAddress), sendPort);

        rb = GetComponent<Rigidbody>();
        rb.constraints = RigidbodyConstraints.FreezeRotationX;
        rb.centerOfMass = new Vector3(0, -0.5f, 0);
        rb.angularDamping = 2f;


        rb.MoveRotation(transform.rotation);
    }

    void ReceiveData(IAsyncResult result)
    {
        try
        {
            if (udpClient == null) return; // Guard clause

            IPEndPoint source = new IPEndPoint(IPAddress.Any, receivePort);
            byte[] data = udpClient.EndReceive(result, ref source);
            string incomingString = Encoding.UTF8.GetString(data);

            lock (dataLock)
            {
                lockedMessage = incomingString;
            }

            // Trigger the NEXT async receive operation safely out here
            udpClient.BeginReceive(new AsyncCallback(ReceiveData), null);
        }
        catch (Exception e)
        {
            // Don't recursively restart here, let it log cleanly
            UnityEngine.Debug.Log($"UDP Receive closed or error: {e.Message}");
        }
    }

    public void SetSlopeAngle(float angle)
    {
        currentSlopeAngle = angle;
    }

    public void ResetSlopeAngle()
    {
        currentSlopeAngle = 0f;
    }

    void FixedUpdate()
    {
        string messageToParse = "";
        lock (dataLock)
        {
            if (!string.IsNullOrEmpty(lockedMessage))
            {
                messageToParse = lockedMessage;
                lockedMessage = "";
            }
        }

        if (!string.IsNullOrEmpty(messageToParse))
        {
            try
            {
                sensor = JsonUtility.FromJson<SensorData>(messageToParse);
            }
            catch
            {
                UnityEngine.Debug.LogWarning("Failed to parse UDP JSON: " + messageToParse);
            }
        }

        rb.linearVelocity = -sensor.speed * transform.forward * scale;
        transform.rotation = Quaternion.Euler(0, -sensor.yaw, 0);

        UnityEngine.Debug.Log("Speed:" + sensor.speed + "| Yaw:" + sensor.yaw);

        if (Time.time >= nextSendTime)
        {
            {
                SendDataToArduino();
                nextSendTime = Time.time + sendRate;
            }
        }
    }

    void SendDataToArduino()
    {
        byte[] sendData = Encoding.UTF8.GetBytes(currentSlopeAngle.ToString("F2"));

        if (udpClient != null)
        {
            udpClient.Send(sendData, sendData.Length, sendEndPoint);
            UnityEngine.Debug.Log("Sent slope angle: " + currentSlopeAngle);
        }
    }

    void OnApplicationQuit()
    {
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }
    }
}
