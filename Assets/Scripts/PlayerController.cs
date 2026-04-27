using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed   = 3.5f;
    public float runSpeed    = 7f;
    public float crouchSpeed = 1.5f;
    public float gravity     = -15f;

    [Header("Crouch")]
    public float standHeight  = 1.8f;
    public float crouchHeight = 0.9f;
    public float crouchTransitionSpeed = 8f;

    [Header("Noise")]
    public float walkNoiseRadius   = 6f;
    public float runNoiseRadius    = 14f;
    public float crouchNoiseRadius = 0f;   
    public LayerMask guardLayer;

    public enum MoveState { Idle, Walk, Run, Crouch }
    public MoveState State { get; private set; } = MoveState.Idle;
    public bool IsCrouching => State == MoveState.Crouch;
    public int  Keycards    { get; private set; }

    CharacterController _cc;
    Transform           _cam;
    float               _yVel;
    float               _targetCCHeight;

    void Awake()
    {
        _cc  = GetComponent<CharacterController>();
        _cam = Camera.main?.transform;

        _cc.height = standHeight;
        _targetCCHeight = standHeight;
    }

    void Update()
    {
        HandleMovement();
        SmoothCrouch();
    }


    void HandleMovement()
    {
        bool wantRun    = Input.GetKey(KeyCode.LeftShift);
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl);

        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool moving = input.sqrMagnitude > 0.01f;

        if (wantCrouch)               State = MoveState.Crouch;
        else if (!moving)             State = MoveState.Idle;
        else if (wantRun)             State = MoveState.Run;
        else                          State = MoveState.Walk;

        // Speed
        float speed = State switch
        {
            MoveState.Crouch => crouchSpeed,
            MoveState.Run    => runSpeed,
            MoveState.Walk   => walkSpeed,
            _                => 0f
        };

        Vector3 camForward = _cam != null ? Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized
                                          : transform.forward;
        Vector3 camRight   = _cam != null ? Vector3.ProjectOnPlane(_cam.right,   Vector3.up).normalized
                                          : transform.right;
        Vector3 moveDir    = (camForward * input.y + camRight * input.x).normalized;

        if (_cc.isGrounded && _yVel < 0f) _yVel = -2f;
        _yVel += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * speed + Vector3.up * _yVel;
        _cc.Move(velocity * Time.deltaTime);

        if (moveDir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                   Quaternion.LookRotation(moveDir),
                                                   12f * Time.deltaTime);

        if (moving)
        {
            float noiseR = State switch
            {
                MoveState.Run    => runNoiseRadius,
                MoveState.Walk   => walkNoiseRadius,
                MoveState.Crouch => crouchNoiseRadius,
                _                => 0f
            };
            if (noiseR > 0f) EmitNoise(noiseR);
        }
    }

    void SmoothCrouch()
    {
        _targetCCHeight = IsCrouching ? crouchHeight : standHeight;
        if (Mathf.Abs(_cc.height - _targetCCHeight) > 0.01f)
        {
            _cc.height = Mathf.Lerp(_cc.height, _targetCCHeight, crouchTransitionSpeed * Time.deltaTime);
            // Keep feet on ground when crouching
            _cc.center = new Vector3(0f, _cc.height * 0.5f, 0f);
        }
    }

    void EmitNoise(float radius)
    {
        Collider[] guards = Physics.OverlapSphere(transform.position, radius, guardLayer);
        foreach (var g in guards)
            g.GetComponent<GuardBrain>()?.OnHearNoise(transform.position);
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Keycard"))
        {
            Keycards++;
            Debug.Log($"Keycard collected! Total: {Keycards}");
            Destroy(other.gameObject);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, walkNoiseRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, runNoiseRadius);
    }
}