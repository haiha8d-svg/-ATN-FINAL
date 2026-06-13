using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SpeedSelectionUI : MonoBehaviour
{
    [Header("Fade")]
    public Image fadeImage;
    public float fadeDuration = 1f;

    private bool isLoadingScene = false;

    void Start()
    {
        // Đảm bảo fade image bật
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(true);

            // Bắt đầu bằng màu đen full
            Color c = fadeImage.color;
            fadeImage.color = new Color(c.r, c.g, c.b, 1f);

            // Fade vào
            StartCoroutine(FadeIn());
        }
    }

    //==================================================
    // CHỌN TỐC ĐỘ
    //==================================================

    public void SelectSpeed5()
    {
        SetSpeed(5f);
    }

    public void SelectSpeed10()
    {
        SetSpeed(10f);
    }

    public void SelectSpeed15()
    {
        SetSpeed(15f);
    }

    void SetSpeed(float speed)
    {
        PlayerPrefs.SetFloat("BotTargetSpeed", speed);
        PlayerPrefs.Save();

        Debug.Log("Đã chọn tốc độ Bot: " + speed + " km/h");
    }

    //==================================================
    // START GAME
    //==================================================

    public void StartGame()
    {
        if (isLoadingScene) return;

        // Nếu chưa chọn tốc độ
        if (!PlayerPrefs.HasKey("BotTargetSpeed"))
        {
            PlayerPrefs.SetFloat("BotTargetSpeed", 5f);
            PlayerPrefs.Save();
        }

        StartCoroutine(FadeOutAndLoad("Track"));
    }

    //==================================================
    // FADE IN
    //==================================================

    IEnumerator FadeIn()
    {
        float time = 0f;

        Color color = fadeImage.color;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeDuration;

            float alpha = Mathf.Lerp(1f, 0f, t);

            fadeImage.color =
                new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        // Đảm bảo alpha = 0 hoàn toàn
        fadeImage.color =
            new Color(color.r, color.g, color.b, 0f);

        // Tắt image sau khi fade xong
        fadeImage.gameObject.SetActive(false);
    }

    //==================================================
    // FADE OUT + LOAD SCENE
    //==================================================

    IEnumerator FadeOutAndLoad(string sceneName)
    {
        isLoadingScene = true;

        // Bật fade image lại
        fadeImage.gameObject.SetActive(true);

        float time = 0f;

        Color color = fadeImage.color;

        // Đảm bảo bắt đầu từ alpha 0
        fadeImage.color =
            new Color(color.r, color.g, color.b, 0f);

        while (time < fadeDuration)
        {
            time += Time.deltaTime;

            float t = time / fadeDuration;

            float alpha = Mathf.Lerp(0f, 1f, t);

            fadeImage.color =
                new Color(color.r, color.g, color.b, alpha);

            yield return null;
        }

        // Đảm bảo full đen trước khi load
        fadeImage.color =
            new Color(color.r, color.g, color.b, 1f);

        yield return new WaitForSeconds(0.1f);

        SceneManager.LoadScene(sceneName);
    }
}