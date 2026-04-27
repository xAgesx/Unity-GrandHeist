using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles the Game Over screen.
///
/// Setup:
///   - Create a Canvas (Screen Space Overlay).
///   - Add a full-screen black Image as background (name it "Background").
///   - Add a Text or TextMeshPro component for the message.
///   - Add a Text or TextMeshPro component for the restart hint.
///   - Attach this script to the Canvas.
///   - Assign the references in the Inspector.
///   - The Canvas starts disabled — this script enables it on game over.
///
/// If you have TextMeshPro installed, swap Text for TMP_Text and
/// uncomment the TMPro using directive below.
/// </summary>

// using TMPro;

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject panel;          // the root panel to show/hide
    public Text       messageText;    // swap for TMP_Text if using TextMeshPro
    public Text       hintText;

    [Header("Message")]
    public string deathMessage  = "YOU'VE BEEN CAUGHT";
    public string restartHint   = "Press R to try again";

    [Header("Fade")]
    public float fadeDuration = 0.6f;

    CanvasGroup canvasGroup;
    bool        isShowing;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }

        canvasGroup = panel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = panel.AddComponent<CanvasGroup>();

        panel.SetActive(false);
    }

    void Update()
    {
        if (isShowing && Input.GetKeyDown(KeyCode.R))
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ShowGameOver()
    {
        if (isShowing) return;
        isShowing = true;

        if (messageText != null) messageText.text = deathMessage;
        if (hintText != null)    hintText.text    = restartHint;

        panel.SetActive(true);
        canvasGroup.alpha = 0f;

        StartCoroutine(FadeIn());

        // Optionally freeze time after fade
        // Time.timeScale = 0f;
    }

    System.Collections.IEnumerator FadeIn()
    {
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(elapsed / fadeDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }
}
