using UnityEngine;

public class SecurityCamera : MonoBehaviour
{
    [Header("Sweep")]
    public float sweepAngle = 60f;
    public float sweepSpeed = 1f;
    public Axis  sweepAxis  = Axis.Y;
    public enum Axis { X, Y, Z }

    [Header("Detection")]
    public float viewDistance = 15f;
    public float viewAngle    = 50f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayers;
    public float crouchDetectionMultiplier = 0.4f;

    [Header("Alert Meter")]
    public float detectionTime = 1.5f;
    public float cooldownTime  = 3f;

    [Header("Spotlight")]
    public Light spotlight;
    public float spotIntensity      = 3f;
    public Color spotPatrolColor    = new Color(1f, 0.95f, 0.8f);
    public Color spotSuspiciousColor = Color.yellow;
    public Color spotAlertColor     = Color.red;

    [Header("Visual (optional)")]
    public Renderer cameraRenderer;
    public int      emissionMaterialIndex = 0;
    public Color    colorIdle       = Color.green;
    public Color    colorSuspicious = Color.yellow;
    public Color    colorAlert      = Color.red;

    public float DetectionMeter { get; private set; }

    Quaternion       _baseRotation;
    bool             _alarmFired;
    Transform        _player;
    PlayerController _playerCtrl;

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        _baseRotation = transform.localRotation;
        var go = GameObject.FindWithTag("Player");
        if (go != null) { _player = go.transform; _playerCtrl = go.GetComponent<PlayerController>(); }

        SetupSpotlight();
        SetEmission(colorIdle);
    }

    void Update()
    {
        Sweep();
        Detect();
        UpdateSpotlight();
    }

    void SetupSpotlight()
    {
        if (!spotlight) return;
        spotlight.type      = LightType.Spot;
        spotlight.spotAngle = viewAngle;
        spotlight.range     = viewDistance;
        spotlight.intensity = spotIntensity;
        spotlight.color     = spotPatrolColor;
        spotlight.shadows   = LightShadows.None;
    }

    void UpdateSpotlight()
    {
        if (!spotlight) return;
        float t = Mathf.Clamp01(DetectionMeter / detectionTime);
        spotlight.color     = Color.Lerp(spotPatrolColor, spotAlertColor, t * t);
        spotlight.spotAngle = viewAngle;
        spotlight.range     = viewDistance;
    }

    void Sweep()
    {
        float angle = Mathf.Sin(Time.time * sweepSpeed * Mathf.PI) * (sweepAngle * 0.5f);
        Vector3 euler = sweepAxis switch
        {
            Axis.X => new Vector3(angle, 0, 0),
            Axis.Z => new Vector3(0, 0, angle),
            _      => new Vector3(0, angle, 0),
        };
        transform.localRotation = _baseRotation * Quaternion.Euler(euler);
    }

    void Detect()
    {
        bool canSee = CanSeePlayer();

        if (canSee)
        {
            DetectionMeter += Time.deltaTime;
            float t = Mathf.Clamp01(DetectionMeter / detectionTime);
            SetEmission(Color.Lerp(colorSuspicious, colorAlert, t));

            if (!_alarmFired && DetectionMeter >= detectionTime)
            {
                _alarmFired = true;
                AlertManager.Instance?.TriggerAlarm(_player.position);
            }
        }
        else
        {
            DetectionMeter -= Time.deltaTime * (detectionTime / cooldownTime);
            DetectionMeter  = Mathf.Max(0f, DetectionMeter);
            _alarmFired     = DetectionMeter > 0f && _alarmFired;
            SetEmission(DetectionMeter > 0f
                ? Color.Lerp(colorIdle, colorSuspicious, DetectionMeter / detectionTime)
                : colorIdle);
        }
    }

    bool CanSeePlayer()
    {
        if (_player == null) return false;
        bool crouching = _playerCtrl != null && _playerCtrl.IsCrouching;
        float maxDist  = viewDistance * (crouching ? crouchDetectionMultiplier : 1f);
        Vector3 toPlayer = _player.position - transform.position;
        if (toPlayer.magnitude > maxDist) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f) return false;
        Vector3 eyePos = _player.position + Vector3.up * (crouching ? 0.6f : 1.5f);
        return !Physics.Linecast(transform.position, eyePos, obstacleLayers, QueryTriggerInteraction.Ignore);
    }

    void SetEmission(Color color)
    {
        if (!cameraRenderer) return;
        cameraRenderer.materials[emissionMaterialIndex].SetColor(EmissionColor, color);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = colorIdle * 0.6f;
        float half = viewAngle * 0.5f;
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler( half, 0, 0) * transform.forward * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(-half, 0, 0) * transform.forward * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0,  half, 0) * transform.forward * viewDistance);
        Gizmos.DrawLine(transform.position, transform.position + Quaternion.Euler(0, -half, 0) * transform.forward * viewDistance);
    }
}