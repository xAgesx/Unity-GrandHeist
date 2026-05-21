using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour {
    Animator anim;

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
    float footstepTimer;
    Transform cam;
    float yVel;
    public HashSet<KeycardColor> inventory = new HashSet<KeycardColor>();

    public List<GameObject> interactablesInRange = new List<GameObject>();

    void Awake() {
        cc = GetComponent<CharacterController>();
        anim = GetComponent<Animator>();
        anim.applyRootMotion = false;
        cam = Camera.main.transform;
        currentStamina = maxStamina;
        state = MoveState.Idle;
        anim.applyRootMotion = false;
    }

    void Update() {
        HandleInteraction();
        HandleMovement();
        SmoothCrouch();
        UpdateUI();
    }

    void HandleInteraction() {
        if (Input.GetKeyDown(KeyCode.E) && interactablesInRange.Count > 0) {
            GameObject target = interactablesInRange[0];

            if (target.CompareTag("Keycard")) {
                Keycard kc = target.GetComponent<Keycard>();
                if (kc != null) {
                    kc.Pickup(this);
                    interactablesInRange.RemoveAt(0);
                    RefreshInteractionFocus();
                    UIManager.Instance.AdvanceOutline();
                }
            } else if (target.CompareTag("Door")) {
                LockedDoor door = target.GetComponent<LockedDoor>();
                if (door != null && door.Open(this)) {
                    interactablesInRange.RemoveAt(0);
                    RefreshInteractionFocus();
                    UIManager.Instance.AdvanceOutline();
                }
            }
        }
    }

    public void RegisterInteractable(GameObject obj) {
        if (!interactablesInRange.Contains(obj)) {
            interactablesInRange.Add(obj);
            RefreshInteractionFocus();
        }
    }

    public void UnregisterInteractable(GameObject obj) {
        if (interactablesInRange.Remove(obj)) {
            RefreshInteractionFocus();
        }
    }

    private void RefreshInteractionFocus() {
        if (interactablesInRange.Count > 0) {
            GameObject first = interactablesInRange[0];

            if (first.CompareTag("Keycard")) {
                Keycard kc = first.GetComponent<Keycard>();
                if (kc != null) {
                    UIManager.Instance.SetPrompt("Press [E] to pick up " + kc.cardColor + " card");
                }
            } else if (first.CompareTag("Door")) {
                LockedDoor door = first.GetComponent<LockedDoor>();
                if (door != null) {
                    if (inventory.Contains(door.requiredCard)) {
                        UIManager.Instance.SetPrompt("Press [E] to open door");
                    } else {
                        UIManager.Instance.SetPrompt("Requires " + door.requiredCard + " card");
                    }
                }
            }
        } else {
            if (UIManager.Instance != null) {
                UIManager.Instance.SetPrompt("");
            }
        }
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

        enableRun = (prevStamina > currentStamina || currentStamina > 10);


        bool canRun = currentStamina > 0f;
        bool wantRun = Input.GetKey(KeyCode.LeftShift) && canRun && enableRun;
        bool wantCrouch = Input.GetKey(KeyCode.LeftControl) | Input.GetKey(KeyCode.C);
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

        UpdateAnimator();

        float interval = state switch {
            MoveState.Crouch => 0.6f,
            MoveState.Walk => 0.45f,
            MoveState.Run => 0.3f,
            _ => 0f
        };
        if (interval > 0f && moving) {
            footstepTimer -= Time.deltaTime;
            if (footstepTimer <= 0f) {
                footstepTimer = interval;
                SoundManager.Instance.PlayFootstep(state);
            }
        } else {
            footstepTimer = 0f;
        }
    }

    void UpdateAnimator() {
        float speed = 0f;
        if (state == MoveState.Crouch) speed = 0.15f;
        else if (state == MoveState.Walk) speed = 0.3f;
        else if (state == MoveState.Run) speed = 1f;
        anim.SetFloat("Speed", speed);
        anim.SetBool("IsCrouching", IsCrouching);
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
        GuardBrain[] guards = FindObjectsOfType<GuardBrain>();
        for (int i = 0; i < guards.Length; i++) {
            float dist = Vector3.Distance(transform.position, guards[i].transform.position);
            if (dist > radius) continue;
            float falloff = Mathf.Pow(1f - Mathf.Clamp01(dist / radius), 3f);
            guards[i].OnHearNoise(transform.position, fillPerSecond * falloff * Time.deltaTime);
        }
    }

    void UpdateUI() {
        float animationSpeed = 0f;
        int soundSpriteIndex = 0;

        switch (state) {
            case MoveState.Idle:
                animationSpeed = 0f;
                soundSpriteIndex = 0;
                break;
            case MoveState.Crouch:
                animationSpeed = 0f;
                soundSpriteIndex = 0;
                break;
            case MoveState.Walk:
                animationSpeed = 0.5f;
                soundSpriteIndex = 1;
                break;
            case MoveState.Run:
                animationSpeed = 1f;
                soundSpriteIndex = 2;
                break;
        }

        if (UIManager.Instance != null) {
            UIManager.Instance.UpdateSoundIndicator(animationSpeed, soundSpriteIndex);
            UIManager.Instance.UpdateStaminaUI(currentStamina, maxStamina);
        }
    }
}