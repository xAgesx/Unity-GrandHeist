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
        Debug.Log("GLOBAL ALARM TRIGGERED!");
        
        GuardBrain[] allGuards = FindObjectsOfType<GuardBrain>();
        foreach (GuardBrain guard in allGuards)
        {
            guard.OnGlobalAlarm(lastKnownPlayerPosition);
        }
    }
}