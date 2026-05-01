using UnityEngine;

public class SecurityCamera : MonoBehaviour {
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
    public float raycastStartOffset = 0.5f;

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
    private bool cameraIsGameOver;
    private static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    void Start() {
        initialLocalRotation = transform.localRotation;

        GameObject go = GameObject.FindWithTag("Player");
        if (go != null) {
            player = go.transform;
        }

        if (childSpotlight != null) {
            childSpotlight.color = colorIdle;
        }
    }

    void Update() {
        if (cameraIsGameOver) {
            return;
        }

        bool canSee = CheckSight();

        HandleDetectionLogic(canSee);
        HandleRotation(canSee);
        UpdateVisuals(canSee);
    }

    bool CheckSight() {
        if (player == null || childSpotlight == null) {
            return false;
        }

        // Check Feet, Waist, and Head to fix blind spots
        Vector3 feet = player.position;
        Vector3 waist = player.position + Vector3.up * 0.9f;
        Vector3 head = player.position + Vector3.up * 1.8f;

        if (IsPointInLightCone(feet) || IsPointInLightCone(waist) || IsPointInLightCone(head)) {
            return true;
        }

        return false;
    }

    bool IsPointInLightCone(Vector3 targetPoint) {
        Vector3 toTarget = targetPoint - transform.position;
        float dist = toTarget.magnitude;

        if (dist > childSpotlight.range) {
            return false;
        }

        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > (childSpotlight.spotAngle * 0.5f)) {
            return false;
        }

        Vector3 rayStart = transform.position + (transform.forward * raycastStartOffset);
        if (Physics.Linecast(rayStart, targetPoint, obstacleLayers, QueryTriggerInteraction.Ignore)) {
            return false;
        }

        return true;
    }

    void HandleDetectionLogic(bool canSee) {
        if (canSee) {
            detectionMeter = detectionMeter + Time.deltaTime;

            if (AlertManager.Instance != null) {
                AlertManager.Instance.TriggerAlarm(player.position);
            }

            if (detectionMeter >= timeToLose) {
                TriggerLose();
            }
        } else {
            detectionMeter = detectionMeter - Time.deltaTime;
            if (detectionMeter < 0) { detectionMeter = 0; }
        }
    }

    void TriggerLose() {
        cameraIsGameOver = true;
        if (GameManager.Instance != null) {
            GameManager.Instance.TriggerGameOver();
        }
    }

    void HandleRotation(bool canSee) {
        if (canSee) {
            Vector3 targetDir = (player.position + Vector3.up * 1.0f) - transform.position;
            if (targetDir != Vector3.zero) {
                Quaternion targetRot = Quaternion.LookRotation(targetDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, lockOnSpeed * Time.deltaTime);
            }
        } else {
            float phase = Mathf.Sin(Time.time * sweepSpeed * Mathf.PI);
            float currentAngle = phase * (sweepAngle * 0.5f);

            Vector3 euler = Vector3.zero;
            switch (sweepAxis) {
                case Axis.X: euler = new Vector3(currentAngle, 0, 0); break;
                case Axis.Y: euler = new Vector3(0, currentAngle, 0); break;
                case Axis.Z: euler = new Vector3(0, 0, currentAngle); break;
            }

            Quaternion sweepRot = initialLocalRotation * Quaternion.Euler(euler);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, sweepRot, returnToSweepSpeed * Time.deltaTime);
        }
    }

    void UpdateVisuals(bool canSee) {
        Color targetColor;
        if (canSee) { targetColor = colorAlert; } else { targetColor = colorIdle; }

        if (childSpotlight != null) {
            childSpotlight.color = Color.Lerp(childSpotlight.color, targetColor, 10f * Time.deltaTime);
        }

        if (cameraRenderer != null) {
            cameraRenderer.materials[emissionMaterialIndex].SetColor(EmissionColor, targetColor);
        }
    }
}