using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    public enum Axis { X, Y, Z }

    [Header("Sweep Settings")]
    public float sweepAngle = 60f;
    public float sweepSpeed = 1f;
    public Axis sweepAxis = Axis.Y;
    public float returnToSweepSpeed = 2f;

    [Header("Detection (Tied to Spotlight)")]
    public Light childSpotlight; 
    public LayerMask obstacleLayers;
    public float lockOnSpeed = 5f;
    public float raycastStartOffset = 0.5f; // Starts the check slightly in front of lens

    [Header("Game Rules")]
    public float timeToLose = 0.5f; 

    [Header("Visuals")]
    public Color colorIdle = Color.green; 
    public Color colorAlert = Color.red;
    public Renderer cameraRenderer;
    public int emissionMaterialIndex = 0;

    public float detectionMeter;
    
    private Quaternion initialLocalRotation;
    private Transform player;
    private bool isGameOver;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    public float Offset = .5f;

    void Start()
    {
        initialLocalRotation = transform.localRotation;
        
        GameObject go = GameObject.FindWithTag("Player");
        if (go != null)
        {
            player = go.transform;
        }

        if (childSpotlight != null)
        {
            childSpotlight.color = colorIdle;
        }
    }

    void Update()
    {
        if (isGameOver)
        {
            return;
        }

        bool canSee = CheckSight();

        HandleDetectionLogic(canSee);
        HandleRotation(canSee);
        UpdateVisuals(canSee);
    }

    bool CheckSight()
    {
        if (player == null || childSpotlight == null)
        {
            return false;
        }

        // We check 3 points to cover the player's entire height
        // This removes the blind spot for players close to the camera
        Vector3 feet = player.position;
        Vector3 waist = player.position + Vector3.up * 0.9f;
        Vector3 head = player.position + Vector3.up * 1.8f;

        if (IsPointInLightCone(feet) || IsPointInLightCone(waist) || IsPointInLightCone(head))
        {
            return true;
        }

        return false;
    }

    bool IsPointInLightCone(Vector3 targetPoint)
    {
        // Calculate vector from camera to the target part of player
        Vector3 toTarget = targetPoint - transform.position;
        float dist = toTarget.magnitude;

        // 1. Distance check
        if (dist > childSpotlight.range)
        {
            return false;
        }

        // 2. Cone Angle check
        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > (childSpotlight.spotAngle * Offset))
        {
            return false;
        }

        // 3. Obstacle check (with offset to prevent hitting the camera itself)
        // We start the linecast slightly forward from the camera center
        Vector3 rayStart = transform.position + (transform.forward * raycastStartOffset);
        
        if (Physics.Linecast(rayStart, targetPoint, obstacleLayers, QueryTriggerInteraction.Ignore))
        {
            return false;
        }

        return true;
    }

    void HandleDetectionLogic(bool canSee)
    {
        if (canSee)
        {
            detectionMeter = detectionMeter + Time.deltaTime;

            if (AlertManager.Instance != null)
            {
                AlertManager.Instance.TriggerAlarm(player.position);
            }

            if (detectionMeter >= timeToLose)
            {
                TriggerLose();
            }
        }
        else
        {
            detectionMeter = detectionMeter - Time.deltaTime;
            if (detectionMeter < 0)
            {
                detectionMeter = 0;
            }
        }
    }

    void TriggerLose()
    {
        isGameOver = true;
        if (GameOverUI.Instance != null)
        {
            GameOverUI.Instance.ShowGameOver();
        }
    }

    void HandleRotation(bool canSee)
    {
        if (canSee)
        {
            Vector3 targetDir = (player.position + Vector3.up * 1.0f) - transform.position;
            if (targetDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lockOnSpeed * Time.deltaTime);
            }
        }
        else
        {
            float phase = Mathf.Sin(Time.time * sweepSpeed * Mathf.PI);
            float currentAngle = phase * (sweepAngle * 0.5f);

            Vector3 euler = Vector3.zero;
            switch (sweepAxis)
            {
                case Axis.X: euler = new Vector3(currentAngle, 0, 0); break;
                case Axis.Y: euler = new Vector3(0, currentAngle, 0); break;
                case Axis.Z: euler = new Vector3(0, 0, currentAngle); break;
            }

            Quaternion sweepRot = initialLocalRotation * Quaternion.Euler(euler);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, sweepRot, returnToSweepSpeed * Time.deltaTime);
        }
    }

    void UpdateVisuals(bool canSee)
    {
        Color targetColor;
        if (canSee)
        {
            targetColor = colorAlert;
        }
        else
        {
            targetColor = colorIdle;
        }

        if (childSpotlight != null)
        {
            childSpotlight.color = Color.Lerp(childSpotlight.color, targetColor, 10f * Time.deltaTime);
        }

        if (cameraRenderer != null)
        {
            cameraRenderer.materials[emissionMaterialIndex].SetColor(EmissionColor, targetColor);
        }
    }
}