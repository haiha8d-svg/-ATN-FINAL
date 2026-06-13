using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Cinemachine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    public bool raceStarted = false;
    public bool raceFinished = false;

    [Header("Countdown")]
    public TMP_Text countdownText; 
    public int countdownTime = 3;
        
    [Header("Lap UI")]
    public TMP_Text lapText;       // Hiển thị số vòng (Lap 1/3)
    public TMP_Text winnerText;    // Hiển thị YOU WIN hoặc BOT WINS

    [Header("Racers")]
    public Transform playerTransform; // Kéo thả Player vào đây
    public Transform botTransform;    // Kéo thả Bot vào đây

    [Header("Cinemachine Setup (Tùy chọn)")]
    [Tooltip("Tạo 1 Cinemachine Virtual Camera có góc quay đẹp, kéo vào đây. Code sẽ tự động chuyển sang Cam này lúc Win.")]
    public CinemachineVirtualCamera winnerCam;

    private int playerLaps = 0;
    private int botLaps = 0;
    private int targetLaps = 1;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        // Lấy số vòng đua từ người chơi chọn
        if (PlayerPrefs.HasKey("TargetLaps"))
        {
            targetLaps = PlayerPrefs.GetInt("TargetLaps");
        }

        UpdateLapUI();
        if (winnerText != null) winnerText.gameObject.SetActive(false);

        StartCoroutine(CountdownToStart());
    }

    IEnumerator CountdownToStart()
    {
        raceStarted = false;
        
        while (countdownTime > 0)
        {
            if (countdownText != null)
                countdownText.text = countdownTime.ToString();
            
            yield return new WaitForSeconds(1f);
            countdownTime--;
        }

        if (countdownText != null)
            countdownText.text = "GO!";
            
        raceStarted = true;

        yield return new WaitForSeconds(1f);

        if (countdownText != null)
            countdownText.gameObject.SetActive(false);
    }

    public void PlayerCompletedLap()
    {
        if (raceFinished || !raceStarted) return;
        
        playerLaps++;
        UpdateLapUI();

        if (playerLaps >= targetLaps)
        {
            EndRace("You Win!", playerTransform);
        }
    }

    public void BotCompletedLap()
    {
        if (raceFinished || !raceStarted) return;

        botLaps++;
        UpdateLapUI();

        if (botLaps >= targetLaps)
        {
            EndRace("Bot Win!", botTransform);
        }
    }

    void UpdateLapUI()
    {
        if (lapText != null)
        {
            lapText.text = "Player Lap: " + playerLaps + "/" + targetLaps + 
                         "\nBot Lap: " + botLaps + "/" + targetLaps;
        }
    }

    void EndRace(string winMessage, Transform winnerTransform)
    {
        raceFinished = true;
        raceStarted = false; // Tắt biến này thì 2 chiếc xe sẽ tự động dừng lại (vận tốc = 0)

        if (winnerText != null)
        {
            winnerText.gameObject.SetActive(true);
            winnerText.text = winMessage;
        }
        
        // Xử lý zoom camera nếu bạn đang dùng Cinemachine
        if (winnerCam != null && winnerTransform != null)
        {
            winnerCam.gameObject.SetActive(true);
            winnerCam.Follow = winnerTransform;
            winnerCam.LookAt = winnerTransform;
            winnerCam.Priority = 999; // Đẩy ưu tiên lên cao nhất để ép màn hình chuyển qua Cam này
        }
        else if (Camera.main != null)
        {
            var camController = Camera.main.GetComponentInParent<rayzngames.CameraController>();
            if (camController != null && winnerTransform != null)
            {
                camController.ZoomOnWinner(winnerTransform);
            }
            
            var camFollow = Camera.main.GetComponentInParent<CameraFollow>();
            if (camFollow != null && winnerTransform != null)
            {
                camFollow.target = winnerTransform;
                camFollow.offset = new Vector3(0, 4f, -6f); // Zoom lại gần
            }
        }

        Debug.Log("Race Ended: " + winMessage);
    }
}
