using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    public static UIManager Instance { get; private set; }

    [Header("HUD Elements")]
    public Text statusText;

    void Awake() {
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject);
        }
    }

    public void UpdateHUD(string stateName, int keycards, string noiseStatus) {
        if (statusText != null) {
            statusText.text = "State: " + stateName + "\nKeycards: " + keycards + "\nNoise: " + noiseStatus;
        }
    }
}