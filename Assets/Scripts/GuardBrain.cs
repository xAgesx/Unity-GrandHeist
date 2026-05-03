
using TMPro;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardBrain : MonoBehaviour {
    [Header("Field of View")]
    public float sightDistance = 15f;
    public float sightAngle = 100f;
    public float crouchSightMod = 0.5f;
    public LayerMask obstacleLayers;
    public TextMeshProUGUI notif;

    [Header("Patrol")]
    public Transform[] waypoints;
    public float waypointWaitTime = 0.5f;

    [Header("Wander")]
    public float wanderRadius = 20f;
    public float wanderWaitTime = 1f;

    [Header("Search")]
    public float searchRadius = 8f;
    public int searchPointCount = 4;
    public float searchWaitTime = 0.5f;
    public float searchDuration = 6f;

    [Header("Movement")]
    public float patrolSpeed = 2.5f;
    public float chaseSpeed = 6.5f;

    [Header("Catch")]
    public float catchTime = 1f;

    [Header("Sound")]
    public float soundMeterMax = 2f;
    public float soundMeterDecay = 0.5f;

    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed";

    [Header("Flashlight")]
    public Light flashlight;
    public Color flashlightPatrol = new Color(1f, 0.85f, 0.6f);
    public Color flashlightAlerted = Color.yellow;
    public Color flashlightDetect = Color.red;
    public float flashlightLerpSpeed = 8f;

    public enum GuardState { Patrol, TurnToSound, ChaseSound, Chase, Search }

    [Header("Debug")]
    public GuardState currentState;
    public bool canSeePlayer;
    public float soundMeter;
    public float stateTimer;
    public float catchTimer;
    public Vector3 lastKnownPos;

    NavMeshAgent agent;
    Transform player;
    PlayerController playerCtrl;
    Node bt;

    int waypointIndex;
    float waypointTimer;
    bool hasWanderTarget;
    float wanderTimer;
    Vector3[] searchPoints;
    int searchPointIndex;
    float searchPointTimer;

    void Start() {
        agent = GetComponent<NavMeshAgent>();
        var p = GameObject.FindWithTag("Player");
        if (p != null) { player = p.transform; playerCtrl = p.GetComponent<PlayerController>(); }

        bt = new Selector(
            new Sequence(new Condition(() => currentState == GuardState.Chase), new ActionNode(TickChase)),
            new Sequence(new Condition(() => currentState == GuardState.ChaseSound), new ActionNode(TickChaseSound)),
            new Sequence(new Condition(() => currentState == GuardState.Search), new ActionNode(TickSearch)),
            new Sequence(new Condition(() => currentState == GuardState.TurnToSound), new ActionNode(TickTurnToSound)),
            new ActionNode(TickPatrol)
        );

        SetupFlashlight();
        SetState(GuardState.Patrol);
    }

    void Update() {
        canSeePlayer = CheckSight();

        if (canSeePlayer) {
            catchTimer += Time.deltaTime;
            if (catchTimer >= catchTime) {
                CatchPlayer();
                return;
            }

            lastKnownPos = player.position;
            if (currentState != GuardState.Chase)
                SetState(GuardState.Chase);
        } else {
            catchTimer = Mathf.Max(0f, catchTimer - Time.deltaTime * 1.5f);
            UIManager.Instance.UpdateDetectionUI(catchTimer, catchTime);
        }

        TickSoundMeter();
        bt.Tick();
        UpdateFlashlight();
        UpdateAnimator();
    }

    void SetState(GuardState s) {
        currentState = s;
        stateTimer = 0f;
        if (notif) notif.text = "";

        if (agent.isOnNavMesh) agent.ResetPath();
        if (agent.isStopped) agent.isStopped = false;

        switch (s) {
            case GuardState.Patrol:
                waypointTimer = 0f;
                agent.speed = patrolSpeed;
                if (waypoints != null && waypoints.Length > 0)
                    agent.SetDestination(waypoints[waypointIndex].position);
                else { hasWanderTarget = false; PickWanderTarget(); }
                break;

            case GuardState.Chase:
                agent.speed = chaseSpeed;
                break;

            case GuardState.ChaseSound:
                agent.speed = chaseSpeed;
                agent.SetDestination(GetValidNavMeshPos(lastKnownPos));
                break;

            case GuardState.TurnToSound:
                agent.speed = chaseSpeed * 0.7f;
                agent.SetDestination(GetValidNavMeshPos(lastKnownPos));
                break;

            case GuardState.Search:
                searchPointIndex = 0;
                searchPointTimer = 0f;
                agent.speed = patrolSpeed;
                GenerateSearchPoints();
                if (searchPoints != null && searchPoints.Length > 0)
                    agent.SetDestination(searchPoints[0]);
                break;
        }
    }

    Vector3 GetValidNavMeshPos(Vector3 target) {
        if (NavMesh.SamplePosition(target, out NavMeshHit hit, 4f, NavMesh.AllAreas))
            return hit.position;
        return target;
    }

    bool CheckSight() {
        if (player == null) return false;

        Vector3 toPlayerFlat = new Vector3(player.position.x - transform.position.x, 0f, player.position.z - transform.position.z);
        float dist = toPlayerFlat.magnitude;

        bool crouch = playerCtrl != null && playerCtrl.IsCrouching;
        float maxDist = sightDistance * (crouch ? crouchSightMod : 1f);

        if (dist > maxDist) return false;

        Vector3 fwdFlat = new Vector3(transform.forward.x, 0f, transform.forward.z);
        bool inAngle = Vector3.Angle(fwdFlat, toPlayerFlat) <= sightAngle * 0.5f;
        if (!inAngle) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 target = player.position + Vector3.up * (crouch ? 0.6f : 1.2f);

        return !Physics.Linecast(origin, target, obstacleLayers, QueryTriggerInteraction.Ignore);
    }

    void TickSoundMeter() {
        soundMeter = Mathf.Max(0f, soundMeter - soundMeterDecay * Time.deltaTime);
    }

    public void OnHearNoise(Vector3 pos, float amount) {
        if (currentState == GuardState.Chase) return;

        soundMeter = Mathf.Min(soundMeterMax, soundMeter + amount);
        lastKnownPos = pos;

        if (soundMeter >= soundMeterMax) {
            soundMeter = 0f;
            SetState(GuardState.ChaseSound);
        } else if (currentState == GuardState.Patrol || currentState == GuardState.Search) {
            SetState(GuardState.TurnToSound);
        }
    }

    public void OnGlobalAlarm(Vector3 pos) {
        lastKnownPos = pos;
        SetState(GuardState.ChaseSound);
    }

    bool HasReachedPathTarget() {
        if (agent.pathPending) return false;
        if (agent.remainingDistance <= 1.5f) return true;
        if (agent.pathStatus == NavMeshPathStatus.PathPartial || agent.pathStatus == NavMeshPathStatus.PathInvalid) return true;
        return false;
    }

    NodeStatus TickChase() {
        if (!canSeePlayer) {
            SetState(GuardState.ChaseSound);
            return NodeStatus.Success;
        }

        agent.SetDestination(GetValidNavMeshPos(player.position));

        if (notif) notif.text = $"! {(catchTime - catchTimer):F1}s";
        return NodeStatus.Running;
    }

    NodeStatus TickChaseSound() {
        if (canSeePlayer) { SetState(GuardState.Chase); return NodeStatus.Success; }

        if (notif) notif.text = "!";
        stateTimer += Time.deltaTime;

        if (HasReachedPathTarget() || stateTimer > 6f) {
            SetState(GuardState.Search);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    NodeStatus TickSearch() {
        if (canSeePlayer) { SetState(GuardState.Chase); return NodeStatus.Success; }

        stateTimer += Time.deltaTime;
        if (notif) notif.text = "?";

        if (stateTimer >= searchDuration) {
            SetState(GuardState.Patrol);
            return NodeStatus.Success;
        }

        if (searchPoints == null || searchPointIndex >= searchPoints.Length) {
            SetState(GuardState.Patrol);
            return NodeStatus.Success;
        }

        if (HasReachedPathTarget()) {
            searchPointTimer += Time.deltaTime;
            if (searchPointTimer >= searchWaitTime) {
                searchPointTimer = 0f;
                searchPointIndex++;
                if (searchPointIndex < searchPoints.Length)
                    agent.SetDestination(searchPoints[searchPointIndex]);
            }
        }

        return NodeStatus.Running;
    }

    NodeStatus TickTurnToSound() {
        if (canSeePlayer) { SetState(GuardState.Chase); return NodeStatus.Success; }

        if (notif) notif.text = "?";
        stateTimer += Time.deltaTime;

        if (HasReachedPathTarget() || stateTimer > 4f) {
            SetState(GuardState.Search);
            return NodeStatus.Success;
        }

        return NodeStatus.Running;
    }

    NodeStatus TickPatrol() {
        agent.speed = patrolSpeed;
        if (notif) notif.text = soundMeter > 0f ? $"~ {soundMeter:F1}" : "";

        if (waypoints != null && waypoints.Length > 0) {
            if (HasReachedPathTarget()) {
                waypointTimer += Time.deltaTime;
                if (waypointTimer >= waypointWaitTime) {
                    waypointTimer = 0f;
                    waypointIndex = (waypointIndex + 1) % waypoints.Length;
                    agent.SetDestination(waypoints[waypointIndex].position);
                }
            }
        } else {
            if (HasReachedPathTarget()) {
                wanderTimer += Time.deltaTime;
                if (wanderTimer >= wanderWaitTime) {
                    wanderTimer = 0f;
                    hasWanderTarget = false;
                }
            }
            if (!hasWanderTarget) PickWanderTarget();
        }

        return NodeStatus.Running;
    }

    void GenerateSearchPoints() {
        searchPoints = new Vector3[searchPointCount + 1];
        searchPoints[0] = GetValidNavMeshPos(lastKnownPos);
        for (int i = 1; i <= searchPointCount; i++) {
            Vector2 r = Random.insideUnitCircle * searchRadius;
            Vector3 c = lastKnownPos + new Vector3(r.x, 0f, r.y);
            searchPoints[i] = GetValidNavMeshPos(c);
        }
    }

    void PickWanderTarget() {
        Vector2 r = Random.insideUnitCircle * wanderRadius;
        Vector3 c = transform.position + new Vector3(r.x, 0f, r.y);
        if (NavMesh.SamplePosition(c, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas)) {
            agent.SetDestination(hit.position);
            hasWanderTarget = true;
        }
    }

    void CatchPlayer() {
        if (notif != null) {
            notif.text = "CAUGHT";
        }

        if (GameManager.Instance != null) {
            GameManager.Instance.TriggerGameOver();
        }
    }

    void SetupFlashlight() {
        if (!flashlight) return;
        flashlight.type = LightType.Spot;
        flashlight.range = sightDistance;
        flashlight.spotAngle = sightAngle;
        flashlight.color = flashlightPatrol;
    }

    void UpdateFlashlight() {
        if (!flashlight) return;
        flashlight.range = sightDistance;
        flashlight.spotAngle = sightAngle;

        Color target = flashlightPatrol;
        if (canSeePlayer)
            target = flashlightDetect;
        else if (currentState == GuardState.ChaseSound || currentState == GuardState.Search || currentState == GuardState.TurnToSound)
            target = flashlightAlerted;

        flashlight.color = Color.Lerp(flashlight.color, target, flashlightLerpSpeed * Time.deltaTime);
    }

    void UpdateAnimator() {
        if (animator) animator.SetFloat(speedParam, agent.velocity.magnitude);
    }

    void OnDrawGizmosSelected() {
        Vector3 o = transform.position + Vector3.up * 1.5f;
        float h = sightAngle * 0.5f;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(o, o + Quaternion.Euler(0, h, 0) * transform.forward * sightDistance);
        Gizmos.DrawLine(o, o + Quaternion.Euler(0, -h, 0) * transform.forward * sightDistance);
        Gizmos.DrawLine(o, o + transform.forward * sightDistance);
    }
}