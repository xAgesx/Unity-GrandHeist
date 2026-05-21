using UnityEngine;
using UnityEngine.SceneManagement;
public class SceneLoader : MonoBehaviour {
    public void PlayGame() {
        SceneManager.LoadScene("Game");
    }
    public void QuitGame(){
        Application.Quit();
    }
}