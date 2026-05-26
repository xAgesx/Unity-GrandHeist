using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour {
    public static GameManager Instance { get; private set; }

    public bool isGameOver;
    public bool isGameWon;
    public GameObject overlayUI;
    public GameObject pausePanel;
    public Image musicButtonImage;
    public Image soundButtonImage;
    public Sprite musicOnSprite;
    public Sprite musicOffSprite;
    public Sprite soundOnSprite;
    public Sprite soundOffSprite;

    public bool IsPaused { get; private set; }
    public bool MusicOn { get; private set; } = true;
    public bool SoundOn { get; private set; } = true;

    float prevMusicVolume = 1f;
    float prevSfxVolume = 1f;

    public float elapsedTime;
    bool timerRunning;

    public float ElapsedTime => elapsedTime;

    void Awake() {
        Time.timeScale = 1f;

        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
            return;
        }

        if (GetComponent<SoundManager>() == null)
            gameObject.AddComponent<SoundManager>();
    }

    void Start() {
        timerRunning = true;
    }

    void Update() {
        if (Input.GetKeyDown(KeyCode.R)) {
            if (IsPaused) TogglePause();
            RestartLevel();
        }

        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (isGameOver || isGameWon) return;
            TogglePause();
        }

        if (timerRunning && !IsPaused && !isGameOver && !isGameWon) {
            elapsedTime += Time.deltaTime;
            if (UIManager.Instance != null)
                UIManager.Instance.UpdateTimerDisplay(elapsedTime);
        }
    }

    public void TogglePause() {
        IsPaused = !IsPaused;

        if (IsPaused) {
            if (overlayUI != null) overlayUI.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
            Time.timeScale = 0f;
        } else {
            if (overlayUI != null) overlayUI.SetActive(true);
            if (pausePanel != null) pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }
    }

    public void Resume() {
        if (!IsPaused) return;
        TogglePause();
    }

    public void ExitToMainMenu() {
        IsPaused = false;
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void RestartLevel() {
        int currentSceneIndex = SceneManager.GetActiveScene().buildIndex;
        SceneManager.LoadScene(currentSceneIndex);
    }

    public void ToggleMusic() {
        MusicOn = !MusicOn;
        if (SoundManager.Instance == null) return;

        if (MusicOn) {
            SoundManager.Instance.SetMusicVolume(prevMusicVolume);
        } else {
            prevMusicVolume = SoundManager.Instance.musicVolume;
            SoundManager.Instance.SetMusicVolume(0f);
        }

        if (musicButtonImage != null)
            musicButtonImage.sprite = MusicOn ? musicOnSprite : musicOffSprite;
    }

    public void ToggleSound() {
        SoundOn = !SoundOn;
        if (SoundManager.Instance == null) return;

        if (SoundOn) {
            SoundManager.Instance.SetSFXVolume(prevSfxVolume);
        } else {
            prevSfxVolume = SoundManager.Instance.sfxVolume;
            SoundManager.Instance.SetSFXVolume(0f);
        }

        if (soundButtonImage != null)
            soundButtonImage.sprite = SoundOn ? soundOnSprite : soundOffSprite;
    }

    public void TriggerGameOver() {
        if (isGameOver) {
            return;
        }

        isGameOver = true;

        if (GameOverUI.Instance != null) {
            GameOverUI.Instance.ShowGameOver();
        }

        if (SoundManager.Instance != null) {
            SoundManager.Instance.PlayMusic(SoundManager.Instance.musicGameOver);
        }

        StartCoroutine(PauseAfterDelay());
    }

    IEnumerator PauseAfterDelay() {
        yield return new WaitForSecondsRealtime(1f);
        Time.timeScale = 0f;
    }

    public void TriggerWin() {
        if (isGameWon || isGameOver) return;
        isGameWon = true;
        timerRunning = false;

        PlayerPrefs.SetFloat("LastTime", elapsedTime);
        PlayerPrefs.SetInt("GameJustWon", 1);
        PlayerPrefs.Save();

        if (SoundManager.Instance != null)
            SoundManager.Instance.StopMusic();

        SceneManager.LoadScene(2);
    }
}