using UnityEngine;

/// <summary>
/// Security Camera with:
///   • Ping-pong sweep rotation
///   • Cone FOV detection (angle + distance)
///   • Line-of-sight raycast (obstacles block view)
///   • Crouching players behind cover are hidden
///   • Graduated alert meter — must be in view for N seconds before alarm
///   • Visual feedback via emission color (idle → suspicious → alert)
/// 
/// Setup:
///   - Attach to the camera model pivot (the part that rotates).
///   - Camera "eye" should face forward (blue axis = look direction).
///   - Assign playerLayer, obstacleLayers, and optionally cameraRenderer.
/// </summary>
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

    /// <summary>
    /// Crouching players are only detected at this fraction of viewDistance.
    /// Set to 1 to disable the bonus.
    /// </summary>
    public float crouchDetectionMultiplier = 0.4f;

    [Header("Alert Meter")]
    [Tooltip("Seconds player must stay in view before alarm fires.")]
    public float detectionTime  = 1.5f;
    [Tooltip("Seconds to drain the meter after losing sight.")]
    public float cooldownTime   = 3f;

    [Header("Visual (optional)")]
    public Renderer cameraRenderer;     
    public int      emissionMaterialIndex = 0;
    public Color    colorIdle       = Color.green;
    public Color    colorSuspicious = Color.yellow;
    public Color    colorAlert      = Color.red;

    Quaternion _baseRotation;
    float      _detectionMeter;   
    bool       _alarmFired;
    Transform  _player;
    PlayerController _playerCtrl;

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");


    void Start()
    {
        _baseRotation = transform.localRotation;

        var go = GameObject.FindWithTag("Player");
        if (go != null)
        {
            _player     = go.transform;
            _playerCtrl = go.GetComponent<PlayerController>();
        }

        SetEmission(colorIdle);
    }

    void Update()
    {
        Sweep();
        Detect();
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
            _detectionMeter += Time.deltaTime;
            float t = Mathf.Clamp01(_detectionMeter / detectionTime);
            SetEmission(Color.Lerp(colorSuspicious, colorAlert, t));

            if (!_alarmFired && _detectionMeter >= detectionTime)
            {
                _alarmFired = true;
                AlertManager.Instance?.TriggerAlarm(_player.position);
            }
        }
        else
        {
            _detectionMeter -= Time.deltaTime * (detectionTime / cooldownTime);
            _detectionMeter  = Mathf.Max(0f, _detectionMeter);
            _alarmFired      = _detectionMeter > 0f && _alarmFired; 

            Color idle = _detectionMeter > 0f
                ? Color.Lerp(colorIdle, colorSuspicious, _detectionMeter / detectionTime)
                : colorIdle;
            SetEmission(idle);
        }
    }

    bool CanSeePlayer()
    {
        if (_player == null) return false;

        bool crouching  = _playerCtrl != null && _playerCtrl.IsCrouching;
        float maxDist   = viewDistance * (crouching ? crouchDetectionMultiplier : 1f);

        Vector3 toPlayer = _player.position - transform.position;
        float   dist     = toPlayer.magnitude;

        if (dist > maxDist) return false;

        if (Vector3.Angle(transform.forward, toPlayer) > viewAngle * 0.5f) return false;

        Vector3 eyePos = _player.position + Vector3.up * 1.5f;
        if (crouching) eyePos = _player.position + Vector3.up * 0.6f;

        if (Physics.Linecast(transform.position, eyePos, obstacleLayers,
                             QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }


    void SetEmission(Color color)
    {
        if (cameraRenderer == null) return;
        var mat = cameraRenderer.materials[emissionMaterialIndex];
        mat.SetColor(EmissionColor, color);
    }

    // ── Gizmos ────────────────────────────────────────────────────

    void OnDrawGizmosSelected()
    {
        DrawCone(viewDistance, colorIdle * 0.6f);
        DrawCone(viewDistance * crouchDetectionMultiplier, colorSuspicious * 0.6f);
    }

    void DrawCone(float dist, Color col)
    {
        Gizmos.color = col;
        float half = viewAngle * 0.5f;
        Vector3 origin = transform.position;
        Gizmos.DrawLine(origin, origin + Quaternion.Euler( half, 0, 0) * transform.forward * dist);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(-half, 0, 0) * transform.forward * dist);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0,  half, 0) * transform.forward * dist);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, -half, 0) * transform.forward * dist);
        Gizmos.DrawLine(origin, origin + transform.forward * dist);
    }
}