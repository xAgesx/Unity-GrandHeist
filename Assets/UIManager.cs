using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("HUD")]
    public Text statusText;
    public Text noiseText;
    public Text promptText;

    [Header("Keycards Visuals")]
    public Image iconGreen;
    public Image iconBlue;
    public Image iconRed;
    public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color activeColor = Color.white;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Set icons to inactive at start
        iconGreen.color = iconBlue.color = iconRed.color = inactiveColor;
        promptText.text = "";
    }

    public void UpdateHUD(string state, int cardCount, string noiseMsg)
    {
        if (statusText) statusText.text = $"State: {state}";
        if (noiseText) noiseText.text = $"Sound: {noiseMsg}";
    }

    public void UpdateKeycards(bool hasGreen, bool hasBlue, bool hasRed)
    {
        iconGreen.color = hasGreen ? activeColor : inactiveColor;
        iconBlue.color = hasBlue ? activeColor : inactiveColor;
        iconRed.color = hasRed ? activeColor : inactiveColor;
    }

    public void SetPrompt(string message)
    {
        promptText.text = message;
    }
}