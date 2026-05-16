using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour {
    public static UIManager Instance { get;  set; }

    [Header("HUD")]
    public Text statusText;
    public Text noiseText;
    public TextMeshProUGUI promptText;

    public List<Outline> outlines;
    public int currentOutlineIndex;

    [Header("Keycards Visuals")]
    public Image iconGreen;
    public Image iconBlue;
    public Image iconRed;
    public Color inactiveColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public Color activeColor = Color.white;

    [Header("Detection Meter")]
    public Image detectionCircle;
    public CanvasGroup detectionCanvasGroup;
    public float fadeSpeed = 5f;
    [Header("Sound Indicator")]
    public Image soundIcon;
    public Animator soundAnimator;
    public Sprite spriteMute;
    public Sprite spriteEmitting;
    public Sprite spriteLoud;
    [Header("Hethi Stamina")]
    public Slider staminaSlider;
    public Image staminaFillImage;
    public Sprite staminaFillImageExhausted;
    public Sprite staminaFillImageDefault;

    void Awake() {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        iconGreen.color = iconBlue.color = iconRed.color = inactiveColor;
        promptText.text = "";

        for (int i = 0; i < outlines.Count; i++) {
            outlines[i].enabled = i == 0;
        }
    }

    public void AdvanceOutline() {
        if (currentOutlineIndex < outlines.Count && outlines[currentOutlineIndex] != null) {
            outlines[currentOutlineIndex].enabled = false;
        }
        currentOutlineIndex = Mathf.Min(currentOutlineIndex + 1, outlines.Count - 1);
        if (currentOutlineIndex < outlines.Count && outlines[currentOutlineIndex] != null) {
            outlines[currentOutlineIndex].enabled = true;
        }
    }

    public void UpdateHUD(string state, int cardCount, string noiseMsg) {
        if (statusText) statusText.text = $"State: {state}";
        if (noiseText) noiseText.text = $"Sound: {noiseMsg}";
    }

    public void UpdateKeycards(bool hasGreen, bool hasBlue, bool hasRed) {
        iconGreen.color = hasGreen ? activeColor : inactiveColor;
        iconBlue.color = hasBlue ? activeColor : inactiveColor;
        iconRed.color = hasRed ? activeColor : inactiveColor;
    }
    public void UpdateDetectionUI(float currentTimer, float maxTimer) {
        if (currentTimer > 0) {
            detectionCanvasGroup.alpha = 1f;
            detectionCircle.fillAmount = 1f - (currentTimer / maxTimer);
        } else {
            if (detectionCanvasGroup.alpha > 0) {
                detectionCanvasGroup.alpha = Mathf.MoveTowards(detectionCanvasGroup.alpha, 0f, fadeSpeed * Time.deltaTime);
            }
        }
    }
    public void UpdateSoundIndicator(float speed, int stateIndex) {
        if (soundAnimator != null) {
            soundAnimator.speed = speed;
        }

        if (soundIcon != null) {
            if (stateIndex == 0) {
                soundIcon.sprite = spriteMute;
            } else if (stateIndex == 1) {
                soundIcon.sprite = spriteEmitting;
            } else if (stateIndex == 2) {
                soundIcon.sprite = spriteLoud;
            }
        }
    }

    public void SetPrompt(string message) {
        promptText.text = message;
        
    }
    public void UpdateStaminaUI(float currentStamina, float maxStamina) {
        if (staminaSlider != null) {
            staminaSlider.value = currentStamina / maxStamina;
            
            staminaFillImage.sprite = (currentStamina < 10)?staminaFillImageExhausted : staminaFillImageDefault;
            
            
        }
    }
}