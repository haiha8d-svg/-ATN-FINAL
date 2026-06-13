using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class Loader : MonoBehaviour
{
    [Header("Loading")]
    public GameObject loadingScreen;
    public Image fill;
    public int sceneToLoad = 1;

    [Header("Speed Selection")]
    public GameObject speedSelectionPanel;

    [Header("Popup Notification")]
    public GameObject popupPanel;

    // TMP TEXT
    public TMP_Text popupText;

    public CanvasGroup popupCanvasGroup;

    [Header("UI To Hide When Loaded")]
    public GameObject[] objectsToHideWhenLoaded;

    private AsyncOperation operation;
    private Coroutine popupCoroutine;

    void Start()
    {
        // Hiện loading
        if (loadingScreen != null)
            loadingScreen.SetActive(true);

        // Ẩn bảng chọn tốc độ
        if (speedSelectionPanel != null)
            speedSelectionPanel.SetActive(false);

        // Ẩn popup lúc đầu
        if (popupPanel != null)
            popupPanel.SetActive(false);

        StartCoroutine(LoadAsync(sceneToLoad));
    }

    IEnumerator LoadAsync(int sceneIndex)
    {
        operation = SceneManager.LoadSceneAsync(sceneIndex);

        // Chặn chuyển scene
        operation.allowSceneActivation = false;

        float visualProgress = 0f;

        while (!operation.isDone)
        {
            float targetProgress = Mathf.Clamp01(operation.progress / 0.9f);

            visualProgress = Mathf.MoveTowards(
                visualProgress,
                targetProgress,
                0.5f * Time.deltaTime
            );

            // Thanh loading
            if (fill != null)
            {
                fill.fillAmount = visualProgress;
            }

            // Khi load đầy
            if (visualProgress >= 1f)
            {
                if (speedSelectionPanel != null &&
                    !speedSelectionPanel.activeSelf)
                {
                    // Ẩn UI loading
                    if (objectsToHideWhenLoaded != null)
                    {
                        foreach (GameObject go in objectsToHideWhenLoaded)
                        {
                            if (go != null)
                                go.SetActive(false);
                        }
                    }

                    if (loadingScreen != null)
                        loadingScreen.SetActive(false);

                    // Hiện chọn tốc độ
                    speedSelectionPanel.SetActive(true);
                }
                else if (speedSelectionPanel == null)
                {
                    operation.allowSceneActivation = true;
                }
            }

            yield return null;
        }
    }

    //==================================================
    // CHỌN TỐC ĐỘ
    //==================================================

    public void SelectSpeed5()
    {
        SetBotSpeed(5f);
    }

    public void SelectSpeed10()
    {
        SetBotSpeed(10f);
    }

    public void SelectSpeed15()
    {
        SetBotSpeed(15f);
    }

    void SetBotSpeed(float speed)
    {
        // Lưu tốc độ
        PlayerPrefs.SetFloat("BotTargetSpeed", speed);
        PlayerPrefs.Save();

        Debug.Log("Đã chọn tốc độ Bot: " + speed + " m/s");

        // Hiện popup TMP
        ShowPopup($"Bạn đã chọn tốc độ <color=yellow>{speed} m/s</color>");
    }

    //==================================================
    // POPUP EFFECT
    //==================================================

    void ShowPopup(string message)
    {
        if (popupPanel == null) return;

        // Nếu popup cũ đang chạy
        if (popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }

        popupCoroutine = StartCoroutine(PopupAnimation(message));
    }

    IEnumerator PopupAnimation(string message)
    {
        popupPanel.SetActive(true);

        // Set TMP text
        if (popupText != null)
        {
            popupText.text = message;
        }

        // Reset alpha
        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = 0f;
        }

        // Scale nhỏ lúc bắt đầu
        popupPanel.transform.localScale = Vector3.one * 0.7f;

        float time = 0f;

        // Fade In + Zoom In
        while (time < 0.25f)
        {
            time += Time.deltaTime;

            float t = time / 0.25f;

            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.alpha =
                    Mathf.Lerp(0f, 1f, t);
            }

            popupPanel.transform.localScale =
                Vector3.Lerp(
                    Vector3.one * 0.7f,
                    Vector3.one,
                    t
                );

            yield return null;
        }

        // Giữ popup
        yield return new WaitForSeconds(1.5f);

        // Fade Out
        time = 0f;

        while (time < 0.25f)
        {
            time += Time.deltaTime;

            float t = time / 0.25f;

            if (popupCanvasGroup != null)
            {
                popupCanvasGroup.alpha =
                    Mathf.Lerp(1f, 0f, t);
            }

            popupPanel.transform.localScale =
                Vector3.Lerp(
                    Vector3.one,
                    Vector3.one * 0.8f,
                    t
                );

            yield return null;
        }

        popupPanel.SetActive(false);
    }

    //==================================================
    // NHẬP SỐ VÒNG
    //==================================================

    public void SetLapsFromInput(string input)
    {
        int laps = 1;

        if (int.TryParse(input, out laps))
        {
            if (laps <= 0)
                laps = 1;

            PlayerPrefs.SetInt("TargetLaps", laps);
            PlayerPrefs.Save();

            Debug.Log("Đã thiết lập số vòng: " + laps);
        }
        else
        {
            Debug.LogWarning("Input không hợp lệ!");
        }
    }

    //==================================================
    // START GAME
    //==================================================

    public void StartGame()
    {
        if (!PlayerPrefs.HasKey("BotTargetSpeed"))
        {
            PlayerPrefs.SetFloat("BotTargetSpeed", 5f);
        }

        if (!PlayerPrefs.HasKey("TargetLaps"))
        {
            PlayerPrefs.SetInt("TargetLaps", 1);
        }

        PlayerPrefs.Save();

        if (operation != null)
        {
            operation.allowSceneActivation = true;
        }
    }
}