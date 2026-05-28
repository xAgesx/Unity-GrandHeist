using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class LockedDoor : MonoBehaviour {
    public PlayableDirector animator;
    public KeycardColor requiredCard;
    public VaultCutsceneDirector vaultCutscene;
    private bool opened;

    public bool Open(PlayerController player) {
        if (opened) return false;
        if (!player.inventory.Contains(requiredCard)) {SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxDoorLocked);return false;};
        opened = true;

        if (vaultCutscene == null)
            vaultCutscene = GetComponent<VaultCutsceneDirector>();
        if (vaultCutscene != null)
        {
            vaultCutscene.BeginCutscene();
            return true;
        }

        SoundManager.Instance.PlaySFX(SoundManager.Instance.sfxDoorOpen);
        if (animator != null) {
            animator.Play();
        }
        return true;
    }

    void Awake()
    {
        if (vaultCutscene == null)
            vaultCutscene = GetComponent<VaultCutsceneDirector>();
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
