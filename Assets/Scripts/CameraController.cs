using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform target;                     
    public Vector3   targetOffset = new(0, 1.6f, 0); 
    public Vector3   crouchOffset = new(0, 1.0f, 0); 

    [Header("Orbit")]
    public float mouseSensitivity = 3f;
    public float minPitch = -30f;
    public float maxPitch =  60f;

    [Header("Zoom")]
    public float defaultDistance = 5f;
    public float minDistance     = 1.5f;
    public float maxDistance     = 10f;
    public float zoomSpeed       = 4f;
    public float zoomSmoothing   = 8f;

    [Header("Smoothing")]
    public float positionSmoothing = 10f;
    public float rotationSmoothing = 15f;

    [Header("Collision")]
    public LayerMask collisionMask;              
    public float     collisionRadius = 0.2f;

    float _yaw;
    float _pitch;
    float _wantedDistance;
    float _currentDistance;

    PlayerController _playerCtrl;

    bool _cutsceneActive;
    Vector3 _cutscenePos;
    Quaternion _cutsceneRot;

    public void SetCutsceneActive(bool active)
    {
        _cutsceneActive = active;
    }

    public void SetCutsceneTransform(Vector3 pos, Quaternion rot)
    {
        _cutscenePos = pos;
        _cutsceneRot = rot;
        transform.position = pos;
        transform.rotation = rot;
    }

    void Awake()
    {
        if (target == null)
        {
            var go = GameObject.FindWithTag("Player");
            if (go) target = go.transform;
        }

        _playerCtrl = target?.GetComponent<PlayerController>();

        Vector3 angles = transform.eulerAngles;
        _yaw   = angles.y;
        _pitch = angles.x;

        _wantedDistance  = defaultDistance;
        _currentDistance = defaultDistance;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (_cutsceneActive)
        {
            transform.position = Vector3.Lerp(transform.position, _cutscenePos, positionSmoothing * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _cutsceneRot, rotationSmoothing * Time.deltaTime);
            return;
        }

        HandleInput();
        CalculatePosition();
    }

    void HandleInput()
    {
        if (Input.GetMouseButton(1))
        {
            _yaw   += Input.GetAxisRaw("Mouse X") * mouseSensitivity;
            _pitch -= Input.GetAxisRaw("Mouse Y") * mouseSensitivity;
            _pitch  = Mathf.Clamp(_pitch, minPitch, maxPitch);
        }
        float scroll = Input.GetAxisRaw("Mouse ScrollWheel");
        if (Mathf.Abs(scroll) > 0.001f)
            _wantedDistance = Mathf.Clamp(_wantedDistance - scroll * zoomSpeed, minDistance, maxDistance);
    }

    void CalculatePosition()
    {
        Vector3 pivotOffset = (_playerCtrl != null && _playerCtrl.IsCrouching)
                              ? crouchOffset
                              : targetOffset;
        Vector3 pivot = target.position + pivotOffset;

        Quaternion desiredRot = Quaternion.Euler(_pitch, _yaw, 0f);
        Vector3    desiredDir = desiredRot * Vector3.back;

        float colDist = _wantedDistance;
        if (Physics.SphereCast(pivot, collisionRadius, desiredDir, out RaycastHit hit,
                               _wantedDistance, collisionMask, QueryTriggerInteraction.Ignore))
        {
            colDist = Mathf.Clamp(hit.distance - collisionRadius, minDistance, _wantedDistance);
        }

        _currentDistance = Mathf.Lerp(_currentDistance, colDist, zoomSmoothing * Time.deltaTime);

        Vector3    targetPos = pivot + desiredDir * _currentDistance;
        Quaternion targetRot = Quaternion.LookRotation(pivot - targetPos);

        transform.position = Vector3.Lerp(transform.position, targetPos, positionSmoothing * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSmoothing * Time.deltaTime);
    }
}