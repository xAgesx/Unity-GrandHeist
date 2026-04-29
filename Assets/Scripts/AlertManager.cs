using UnityEngine;

public class AlertManager : MonoBehaviour
{
    public static AlertManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void TriggerAlarm(Vector3 lastKnownPlayerPosition)
    {
        GuardBrain[] allGuards = FindObjectsByType<GuardBrain>(FindObjectsSortMode.None);
        foreach (var guard in allGuards)
            guard.OnGlobalAlarm(lastKnownPlayerPosition);
    }
}