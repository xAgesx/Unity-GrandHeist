using UnityEngine;

public enum KeycardColor { Green, Blue, Red }

public class Keycard : MonoBehaviour {
    public KeycardColor cardColor;
    public Renderer cardRenderer;
    public Color highlightColor = Color.white;
    private bool playerNearby;
    private PlayerController playerRef;

    void Update() {
        if (playerNearby && Input.GetKeyDown(KeyCode.E)) {
            playerRef.AddKeycard(cardColor);
            UIManager.Instance.SetPrompt("");
            UIManager.Instance.outlines.RemoveAt(0);
            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) {
            playerNearby = true;
            playerRef = other.GetComponent<PlayerController>();
            SetHighlight(true);
            UIManager.Instance.ToggleOutline();
            UIManager.Instance.SetPrompt("Press [E] to pick up " + cardColor + " card");
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            playerNearby = false;
            SetHighlight(false);
            UIManager.Instance.SetPrompt("");
            UIManager.Instance.ToggleOutline();

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