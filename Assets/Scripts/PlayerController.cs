
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed   = 3.5f;
    public float runSpeed    = 7f;
    public float crouchSpeed = 1.5f;
    public float gravity     = -15f;

    [Header("Crouch")]
    public float standHeight           = 1.8f;
    public float crouchHeight          = 0.9f;
    public float crouchTransitionSpeed = 8f;

    [Header("Noise — fill per second at point-blank")]
    public float walkNoiseFill   = 0.6f;
    public float runNoiseFill    = 2.5f;
    public float walkNoiseRadius = 7f;
    public float runNoiseRadius  = 16f;
    public LayerMask guardLayer;

    [Header("UI")]
    public Text playerStatusText;

    public enum MoveState { Idle, Walk, Run, Crouch }
    public MoveState State { get; private set; } = MoveState.Idle;
    public bool IsCrouching => State == MoveState.Crouch;
    public int  Keycards    { get; private set; }

    CharacterController _cc;
    Transform           _cam;
    float               _yVel;
    public HashSet<KeycardColor> Inventory = new HashSet<KeycardColor>();

    void Awake()
    {
        _cc  = GetComponent<CharacterController>();
        _cam = Camera.main?.transform;
        _cc.height = standHeight;
        _cc.center = new Vector3(0f, standHeight * 0.5f, 0f);
    }

    void Update()
    {
        HandleMovement();
        SmoothCrouch();
        UpdateUI();
    }
     public void AddKeycard(KeycardColor color)
    {
        Inventory.Add(color);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateKeycards(
                Inventory.Contains(KeycardColor.Green),
                Inventory.Contains(KeycardColor.Blue),
                Inventory.Contains(KeycardColor.Red)
            );
        }
    }

    void HandleMovement()
    {
        bool wantRun    = Input.GetKey(KeyCode.LeftShift);
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl);
        Vector2 input   = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool moving     = input.sqrMagnitude > 0.01f;

        if (wantCrouch)   State = MoveState.Crouch;
        else if (!moving) State = MoveState.Idle;
        else if (wantRun) State = MoveState.Run;
        else              State = MoveState.Walk;

        float speed = State switch
        {
            MoveState.Crouch => crouchSpeed,
            MoveState.Run    => runSpeed,
            MoveState.Walk   => walkSpeed,
            _                => 0f
        };

        Vector3 fwd  = _cam ? Vector3.ProjectOnPlane(_cam.forward, Vector3.up).normalized : transform.forward;
        Vector3 rgt  = _cam ? Vector3.ProjectOnPlane(_cam.right,   Vector3.up).normalized : transform.right;
        Vector3 dir  = (fwd * input.y + rgt * input.x).normalized;

        if (_cc.isGrounded && _yVel < 0f) _yVel = -2f;
        _yVel += gravity * Time.deltaTime;
        _cc.Move((dir * speed + Vector3.up * _yVel) * Time.deltaTime);

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);

        if (moving)
        {
            switch (State)
            {
                case MoveState.Run:  EmitNoise(runNoiseRadius,  runNoiseFill);  break;
                case MoveState.Walk: EmitNoise(walkNoiseRadius, walkNoiseFill); break;
            }
        }
    }

    void SmoothCrouch()
    {
        float target = IsCrouching ? crouchHeight : standHeight;
        if (Mathf.Abs(_cc.height - target) > 0.01f)
        {
            float prev   = _cc.height;
            _cc.height   = Mathf.Lerp(_cc.height, target, crouchTransitionSpeed * Time.deltaTime);
            _cc.center   = new Vector3(0f, _cc.height * 0.5f, 0f);
            transform.position += new Vector3(0f, (_cc.height - prev) * 0.5f, 0f);
        }
    }

    void EmitNoise(float radius, float fillPerSecond)
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, guardLayer);
        foreach (var h in hits)
        {
            var brain = h.GetComponent<GuardBrain>();
            if (brain == null) continue;
            
            float dist = Vector3.Distance(transform.position, h.transform.position);
            
            // Exponential falloff makes guards hyper-sensitive close up, but allows walking further away
            float falloff = Mathf.Pow(1f - Mathf.Clamp01(dist / radius), 3f);
            
            brain.OnHearNoise(transform.position, fillPerSecond * falloff * Time.deltaTime);
        }
    }

    void UpdateUI()
{
    if (UIManager.Instance != null)
    {
        string noiseMsg;
        if (State == MoveState.Idle || State == MoveState.Crouch)
        {
            noiseMsg = "Silent";
        }
        else
        {
            noiseMsg = "Emitting";
        }

        UIManager.Instance.UpdateHUD(State.ToString(), Keycards, noiseMsg);
    }
}

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Keycard"))
        {
            Keycards++;
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