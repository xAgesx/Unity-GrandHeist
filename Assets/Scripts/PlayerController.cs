using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour {
    public float walkSpeed = 3.5f;
    public float runSpeed = 7f;
    public float crouchSpeed = 1.5f;
    public float gravity = -15f;

    public float standHeight = 1.8f;
    public float crouchHeight = 0.9f;
    public float crouchTransitionSpeed = 8f;

    public float walkNoiseFill = 0.6f;
    public float runNoiseFill = 2.5f;
    public float walkNoiseRadius = 7f;
    public float runNoiseRadius = 16f;
    public LayerMask guardLayer;

    public float maxStamina = 100f;
    public float staminaDepleteRate = 25f;
    public float staminaRegenRate = 15f;

    public enum MoveState { Idle, Walk, Run, Crouch }
    public MoveState state { get; private set; }

    public bool IsCrouching {
        get { return state == MoveState.Crouch; }
    }

    public float currentStamina;
    bool enableRun = true;
    private float prevStamina = 30f;
    CharacterController cc;
    Transform cam;
    float yVel;
    public HashSet<KeycardColor> inventory = new HashSet<KeycardColor>();

    void Awake() {
        cc = GetComponent<CharacterController>();
        cam = Camera.main.transform;
        currentStamina = maxStamina;
        state = MoveState.Idle;
    }

    void Update() {
        HandleMovement();
        SmoothCrouch();
        UpdateUI();
    }

    public void AddKeycard(KeycardColor color) {
        inventory.Add(color);
        UIManager.Instance.UpdateKeycards(
            inventory.Contains(KeycardColor.Green),
            inventory.Contains(KeycardColor.Blue),
            inventory.Contains(KeycardColor.Red)
        );
    }

    void HandleMovement() {
        Debug.Log(prevStamina + " " + currentStamina);
        enableRun = (prevStamina > currentStamina || currentStamina > 10);
        

        bool canRun = currentStamina > 0f;
        bool wantRun = Input.GetKey(KeyCode.LeftShift) && canRun && enableRun;
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl);
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        bool moving = input.sqrMagnitude > 0.01f;

        if (wantCrouch) state = MoveState.Crouch;
        else if (!moving) state = MoveState.Idle;
        else if (wantRun) state = MoveState.Run;
        else state = MoveState.Walk;


        if (state == MoveState.Run) {
            prevStamina = currentStamina;

            currentStamina -= staminaDepleteRate * Time.deltaTime;
            

            if (currentStamina < 0) { currentStamina = 0; }
        } else {
            prevStamina = currentStamina;

            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina > maxStamina) currentStamina = maxStamina;

        }

        float speed = walkSpeed;
        if (state == MoveState.Crouch) speed = crouchSpeed;
        else if (state == MoveState.Run) speed = runSpeed;
        else if (state == MoveState.Idle) speed = 0f;

        Vector3 fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up).normalized;
        Vector3 rgt = Vector3.ProjectOnPlane(cam.right, Vector3.up).normalized;
        Vector3 dir = (fwd * input.y + rgt * input.x).normalized;

        if (cc.isGrounded && yVel < 0f) yVel = -2f;
        yVel += gravity * Time.deltaTime;
        cc.Move((dir * speed + Vector3.up * yVel) * Time.deltaTime);

        if (dir.sqrMagnitude > 0.01f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 12f * Time.deltaTime);

        if (moving) {
            if (state == MoveState.Run) EmitNoise(runNoiseRadius, runNoiseFill);
            else if (state == MoveState.Walk) EmitNoise(walkNoiseRadius, walkNoiseFill);
        }

    }

    void SmoothCrouch() {
        float target = IsCrouching ? crouchHeight : standHeight;
        if (Mathf.Abs(cc.height - target) > 0.01f) {
            float prev = cc.height;
            cc.height = Mathf.Lerp(cc.height, target, crouchTransitionSpeed * Time.deltaTime);
            cc.center = new Vector3(0f, cc.height * 0.5f, 0f);
            transform.position += new Vector3(0f, (cc.height - prev) * 0.5f, 0f);
        }
    }

    void EmitNoise(float radius, float fillPerSecond) {
        Collider[] hits = Physics.OverlapSphere(transform.position, radius, guardLayer);
        foreach (var h in hits) {
            GuardBrain brain = h.GetComponent<GuardBrain>();
            if (brain != null) {
                float dist = Vector3.Distance(transform.position, h.transform.position);
                float falloff = Mathf.Pow(1f - Mathf.Clamp01(dist / radius), 3f);
                brain.OnHearNoise(transform.position, fillPerSecond * falloff * Time.deltaTime);
            }
        }
    }

    void UpdateUI() {
        string noiseLabel = "SILENT";
        if (state == MoveState.Walk) noiseLabel = "LOW";
        if (state == MoveState.Run) noiseLabel = "LOUD";

        // UIManager.Instance.UpdateNoise(noiseLabel);
        // UIManager.Instance.UpdateStamina(currentStamina, maxStamina);
    }
}