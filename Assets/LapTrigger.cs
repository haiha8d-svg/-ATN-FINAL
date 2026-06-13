using UnityEngine;

public class LapTrigger : MonoBehaviour
{
    private float lastPlayerTime = -10f;
    private float lastBotTime = -10f;
    
    [Tooltip("Thời gian chờ (giây) để tránh đếm trùng khi xe đỗ trên vạch")]
    public float cooldown = 5f; 

    void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance == null || !GameManager.Instance.raceStarted) return;

        // Nếu người chơi chạm vạch
        if (other.CompareTag("Player"))
        {
            // Kiểm tra xem đã qua 5s kể từ lần cuối chạm vạch chưa
            if (Time.time - lastPlayerTime > cooldown)
            {
                lastPlayerTime = Time.time;
                GameManager.Instance.PlayerCompletedLap();
            }
        }
        // Nếu Bot chạm vạch
        else if (other.CompareTag("Bot"))
        {
            if (Time.time - lastBotTime > cooldown)
            {
                lastBotTime = Time.time;
                GameManager.Instance.BotCompletedLap();
            }
        }
    }
}
