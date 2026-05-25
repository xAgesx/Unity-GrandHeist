using UnityEngine;
using UnityEngine.SceneManagement ;

public class MenuManager : MonoBehaviour {
    public static int previousSceneIndex;
    public GameObject submitButton;

    public void Start() {
        if (SceneManager.GetActiveScene().buildIndex != 2) {
            previousSceneIndex = SceneManager.GetActiveScene().buildIndex;

        } else {
            //update LeaderBoard scene based on previousScene(Menu / Game) to add a submit button
            if (previousSceneIndex == 1) {
                if (submitButton != null) {
                    submitButton.SetActive(true);
                    
                }

                
            }
        }
    }


    public void Quit() {
        Application.Quit();
        Debug.Log("Quit");
    }
    public void StartGame() {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
        Time.timeScale = 1;
    }
    //Load Leaderboard scene
    public void Leaderboard() {
        SceneManager.LoadScene(2);

    }
    public void Back() {
        SceneManager.LoadScene(previousSceneIndex);
    }
    public void BackToMenu() {
        SceneManager.LoadScene(0);
    }

    
}
