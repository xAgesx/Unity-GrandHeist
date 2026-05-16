using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class LockedDoor : MonoBehaviour {
    public PlayableDirector animator;
    public KeycardColor requiredCard;
    private bool opened;

    public bool Open(PlayerController player) {
        if (opened) return false;
        if (!player.inventory.Contains(requiredCard)) return false;
        opened = true;
        if (animator != null) {
            animator.Play();
        }
        return true;
    }

    void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !opened) {
            other.GetComponent<PlayerController>()?.RegisterInteractable(gameObject);
        }
    }

    void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            other.GetComponent<PlayerController>()?.UnregisterInteractable(gameObject);
        }
    }
}
