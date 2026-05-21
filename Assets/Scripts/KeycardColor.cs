using UnityEngine;

public enum KeycardColor { Green, Blue, Red }

public class Keycard : MonoBehaviour {
    public KeycardColor cardColor;
    public Renderer cardRenderer;
    public Color highlightColor = Color.white;
    private PlayerController playerRef;

    public void Pickup(PlayerController player) {
        player.AddKeycard(cardColor);
        SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxKeycardPickup);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            playerRef = other.GetComponent<PlayerController>();
            SetHighlight(true);
            playerRef.RegisterInteractable(gameObject);
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            SetHighlight(false);
            if (playerRef != null) {
                playerRef.UnregisterInteractable(gameObject);
                playerRef = null;
            }
        }
    }

    void SetHighlight(bool lit) {
        if (cardRenderer == null) return;
        if (lit) {
            cardRenderer.material.EnableKeyword("_EMISSION");
            cardRenderer.material.SetColor("_EmissionColor", highlightColor);
        } else {
            cardRenderer.material.SetColor("_EmissionColor", Color.black);
        }
    }
}