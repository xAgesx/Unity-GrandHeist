using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isGameOver;

    void Awake()
    {
        Time.timeScale = 1f;

        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (GetComponent<SoundManager>() == null)
            gameObject.AddComponent<SoundManager>();
    }

    void Update()
    {
        // Global restart check
        if (Input.GetKeyDown(KeyCode.R))
        {
            RestartLevel();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Application.Quit();
        }
    }

    public void RestartLevel()
    {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void TriggerGameOver()
    {
        if (isGameOver)
        {
            return;
        }

        isGameOver = true;

        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.ShowGameOver();
        }

        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayMusic(SoundManager.Instance.musicGameOver);
        }

        StartCoroutine(PauseAfterDelay());
    }

    IEnumerator PauseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 0f;
    }
}