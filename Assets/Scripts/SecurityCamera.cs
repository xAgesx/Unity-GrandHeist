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
        //PS :Change the Value ( .35f) to play around with the precision of the light cone
        float angle = Vector3.Angle(transform.forward, toTarget);
        if (angle > (childSpotlight.spotAngle * 0.35f)) {
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
            UIManager.Instance.UpdateDetectionUI(detectionMeter, timeToLose);

            if (AlertManager.Instance != null) {
                AlertManager.Instance.TriggerAlarm(player.position);
            }

            if (detectionMeter >= timeToLose) {
                TriggerLose();
            }
        } else {
            detectionMeter = detectionMeter - Time.deltaTime;
            if (detectionMeter < 0) { detectionMeter = 0; }
            UIManager.Instance.UpdateDetectionUI(detectionMeter, timeToLose);
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
    public void OnDrawGizmos() {
    if (childSpotlight == null) return;
    float range = childSpotlight.range;
    float spotAngle = childSpotlight.spotAngle;
    float halfAngle = spotAngle * 0.5f * Mathf.Deg2Rad;
    float radius = range * Mathf.Tan(halfAngle);
    Gizmos.color = Color.Lerp(colorIdle, colorAlert, detectionMeter / Mathf.Max(timeToLose, 0.001f));
    DrawConeWireframe(range, radius);
    DrawSweepBounds(range);
}
void DrawConeWireframe(float range, float radius) {
    Vector3 origin = transform.position;
    Vector3 forward = transform.forward;
    Vector3 up = transform.up;
    Vector3 right = transform.right;
    // Far ring center
    Vector3 farCenter = origin + forward * range;
    // 4 edge lines (top, bottom, left, right)
    Vector3 top = farCenter + up * radius;
    Vector3 bottom = farCenter - up * radius;
    Vector3 left = farCenter - right * radius;
    Vector3 rightPt = farCenter + right * radius;
    Gizmos.DrawLine(origin, top);
    Gizmos.DrawLine(origin, bottom);
    Gizmos.DrawLine(origin, left);
    Gizmos.DrawLine(origin, rightPt);
    // Far ring (16 segments)
    DrawCircle(farCenter, forward, up, right, radius, 16);
    // Intermediate rings at 1/3 and 2/3
    DrawCircle(origin + forward * (range * 0.33f), forward, up, right, radius * 0.33f, 12);
    DrawCircle(origin + forward * (range * 0.66f), forward, up, right, radius * 0.66f, 12);
}
void DrawCircle(Vector3 center, Vector3 forward, Vector3 up, Vector3 right, float radius, int segments) {
    float step = 360f / segments;
    Vector3 prev = center + up * radius;
    for (int i = 1; i <= segments; i++) {
        float a = i * step * Mathf.Deg2Rad;
        Vector3 dir = (up * Mathf.Cos(a) + right * Mathf.Sin(a)).normalized;
        Vector3 next = center + dir * radius;
        Gizmos.DrawLine(prev, next);
        prev = next;
    }
}
void DrawSweepBounds(float range) {
    if (sweepAngle < 0.01f) return;
    float halfSweep = sweepAngle * 0.5f;
    Vector3 origin = transform.position;
    Vector3 baseDir = transform.forward;
    Vector3 axisVec = sweepAxis == Axis.X ? Vector3.right
                   : sweepAxis == Axis.Y ? Vector3.up
                   : Vector3.forward;
    Color sweepColor = Gizmos.color;
    sweepColor.a *= 0.4f;
    Gizmos.color = sweepColor;
    for (int side = -1; side <= 1; side += 2) {
        float angle = side * halfSweep;
        Quaternion rot = Quaternion.AngleAxis(angle, axisVec);
        Vector3 dir = rot * baseDir;
        Gizmos.DrawLine(origin, origin + dir * range);
    }
}
void OnDrawGizmosSelected() {
    if (player == null || childSpotlight == null) return;
    Vector3 origin = transform.position + transform.forward * raycastStartOffset;
    Vector3[] points = new Vector3[] {
        player.position,
        player.position + Vector3.up * 0.9f,
        player.position + Vector3.up * 1.8f
    };
    foreach (Vector3 point in points) {
        Vector3 toTarget = point - transform.position;
        bool inRange = toTarget.magnitude <= childSpotlight.range;
        bool inAngle = Vector3.Angle(transform.forward, toTarget) <= childSpotlight.spotAngle * 0.5f;
        bool blocked = Physics.Linecast(origin, point, obstacleLayers, QueryTriggerInteraction.Ignore);
        if (!inRange || !inAngle) {
            Gizmos.color = Color.yellow;
        } else if (blocked) {
            Gizmos.color = Color.red;
        } else {
            Gizmos.color = Color.green;
        }
        Gizmos.DrawLine(origin, point);
        Gizmos.DrawSphere(point, 0.1f);
    }
    if (detectionMeter > 0) {
        float t = detectionMeter / Mathf.Max(timeToLose, 0.001f);
        Gizmos.color = Color.Lerp(Color.yellow, Color.red, t);
        Gizmos.DrawWireSphere(player.position + Vector3.up * 1.0f, 0.3f + t * 0.2f);
    }
}
}