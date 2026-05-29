using UnityEngine;

public class VaultCutsceneSetup : MonoBehaviour
{
    void Awake()
    {
        LockedDoor vaultDoor = FindVaultDoor();
        if (vaultDoor == null)
        {
            Debug.LogWarning("VaultCutsceneSetup: No vault door found.");
            return;
        }

        VaultCutsceneDirector director = vaultDoor.GetComponent<VaultCutsceneDirector>();
        if (director == null)
            director = vaultDoor.gameObject.AddComponent<VaultCutsceneDirector>();

        director.vaultDoorTimeline = vaultDoor.animator;
        director.camCtrl = FindObjectOfType<CameraController>();
        director.player = FindObjectOfType<PlayerController>();
        director.vaultDoorFrame = vaultDoor.transform.parent;

        if (GameManager.Instance != null && GameManager.Instance.overlayUI != null)
            director.hudGroup = GameManager.Instance.overlayUI.transform;

        if (SoundManager.Instance != null)
        {
            if (director.sfxVaultOpen == null)
                director.sfxVaultOpen = SoundManager.Instance.sfxDoorOpen;
            if (director.sfxAlarm == null)
                director.sfxAlarm = SoundManager.Instance.sfxAlarm;
        }
    }

    LockedDoor FindVaultDoor()
    {
        LockedDoor[] allDoors = FindObjectsOfType<LockedDoor>();
        foreach (LockedDoor door in allDoors)
        {
            if (door.requiredCard != KeycardColor.Red) continue;
            if (door.animator == null) continue;
            if (door.animator.playableAsset == null) continue;
            if (door.animator.playableAsset.name.Contains("Vault"))
                return door;
        }
        return null;
    }

}
