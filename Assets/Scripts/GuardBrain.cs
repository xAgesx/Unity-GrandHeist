using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class GuardBrain : MonoBehaviour
{
    [Header("Field of View")]
    public float sightDistance  = 12f;
    public float sightAngle     = 90f;
    public float crouchSightMod = 0.45f;
    public LayerMask playerLayer;
    public LayerMask obstacleLayers;

    [Header("Patrol — leave empty for random wander")]
    public Transform[] waypoints;
    public float waypointWaitTime = 1.5f;

    [Header("Random Wander (fallback)")]
    public float wanderRadius   = 20f;
    public float wanderWaitTime = 2f;

    [Header("Search")]
    public float searchRadius     = 6f;
    public int   searchPointCount = 3;
    public float searchWaitTime   = 2f;
    public float searchTimeout    = 12f;

    [Header("Movement")]
    public float patrolSpeed = 2.5f;
    public float searchSpeed = 3.5f;
    public float chaseSpeed  = 6f;

    [Header("Catch")]
    public float catchDistance  = 1.2f;
    public float exposureTime   = 1f;    // seconds in guard LOS before caught

    [Header("Animation")]
    public Animator animator;
    public string speedParam = "Speed";

    NavMeshAgent     navAgent;
    Transform        player;
    PlayerController playerCtrl;
    Node             behaviorTree;

    bool    canSeePlayer;
    bool    heardNoise;
    Vector3 lastKnownPosition;

    int   currentWaypoint;
    float waypointTimer;

    bool  hasWanderTarget;
    float wanderTimer;

    int       searchIndex;
    float     searchTimer;
    float     searchElapsed;
    Vector3[] searchPoints;
    float exposureTimer;

    void Start()
    {
        navAgent = GetComponent<NavMeshAgent>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            player     = playerObj.transform;
            playerCtrl = playerObj.GetComponent<PlayerController>();
        }

        BuildBehaviorTree();
        StartPatrol();
    }

    void Update()
    {
        UpdateSenses();
        behaviorTree.Tick();
        UpdateAnimator();
    }

    void BuildBehaviorTree()
    {
        Node chaseBranch = new Sequence(
            new Condition(IsChasing),
            new ActionNode(ChasePlayer)
        );

        Node searchBranch = new Sequence(
            new Condition(ShouldSearch),
            new ActionNode(SearchArea)
        );

        Node patrolBranch = (waypoints != null && waypoints.Length > 0)
            ? new ActionNode(PatrolWaypoints)
            : (Node)new ActionNode(PatrolWander);

        behaviorTree = new Selector(
            chaseBranch,
            searchBranch,
            patrolBranch
        );
    }

    void UpdateSenses()
    {
        canSeePlayer = CheckLineOfSight();
        if (canSeePlayer)
        {
            lastKnownPosition = player.position;
            heardNoise = false;
        }
    }

    bool CheckLineOfSight()
    {
        if (player == null) return false;

        bool isCrouching = playerCtrl != null && playerCtrl.IsCrouching;
        float maxDist    = sightDistance * (isCrouching ? crouchSightMod : 1f);

        Vector3 toPlayer = player.position - transform.position;
        if (toPlayer.magnitude > maxDist) return false;
        if (Vector3.Angle(transform.forward, toPlayer) > sightAngle * 0.5f) return false;

        Vector3 eyeTarget = player.position + Vector3.up * (isCrouching ? 0.6f : 1.5f);
        Vector3 eyeOrigin = transform.position + Vector3.up * 1.5f;

        if (Physics.Linecast(eyeOrigin, eyeTarget, obstacleLayers, QueryTriggerInteraction.Ignore))
            return false;

        return true;
    }

    public void OnHearNoise(Vector3 noisePosition)
    {
        if (canSeePlayer) return;
        heardNoise        = true;
        lastKnownPosition = noisePosition;
        BeginSearch();
    }

    public void OnGlobalAlarm(Vector3 playerPosition)
    {
        lastKnownPosition = playerPosition;
        heardNoise        = false;
        canSeePlayer      = true;
    }

    bool IsChasing()
    {
        return canSeePlayer;
    }

    bool ShouldSearch()
    {
        return heardNoise;
    }

    NodeStatus ChasePlayer()
    {
        navAgent.speed = chaseSpeed;
        navAgent.SetDestination(player.position);

        bool inRange = Vector3.Distance(transform.position, player.position) <= catchDistance;
        bool inSight = canSeePlayer;

        if (inRange || inSight)
        {
            exposureTimer += Time.deltaTime;
            if (exposureTimer >= exposureTime)
            {
                CatchPlayer();
                return NodeStatus.Success;
            }
        }
        else
        {
            exposureTimer = Mathf.Max(0f, exposureTimer - Time.deltaTime);
        }

        return NodeStatus.Running;
    }

    NodeStatus SearchArea()
    {
        navAgent.speed = searchSpeed;

        searchElapsed += Time.deltaTime;
        if (searchElapsed >= searchTimeout)
        {
            EndSearch();
            return NodeStatus.Failure;
        }

        if (searchPoints == null || searchPoints.Length == 0)
        {
            GenerateSearchPoints();
            searchIndex = 0;
            searchTimer = 0f;
        }

        if (searchIndex >= searchPoints.Length)
        {
            EndSearch();
            return NodeStatus.Failure;
        }

        navAgent.SetDestination(searchPoints[searchIndex]);

        bool arrived = !navAgent.pathPending && navAgent.remainingDistance < 0.5f;
        if (arrived)
        {
            searchTimer += Time.deltaTime;
            if (searchTimer >= searchWaitTime)
            {
                searchTimer = 0f;
                searchIndex++;
            }
        }

        return NodeStatus.Running;
    }

    NodeStatus PatrolWaypoints()
    {
        navAgent.speed = patrolSpeed;

        bool arrived = !navAgent.pathPending && navAgent.remainingDistance < 0.5f;
        if (arrived)
        {
            waypointTimer += Time.deltaTime;
            if (waypointTimer >= waypointWaitTime)
            {
                waypointTimer = 0f;
                GoToWaypoint((currentWaypoint + 1) % waypoints.Length);
            }
        }

        return NodeStatus.Running;
    }

    NodeStatus PatrolWander()
    {
        navAgent.speed = patrolSpeed;

        bool arrived = !navAgent.pathPending && navAgent.remainingDistance < 0.5f;
        if (arrived)
        {
            wanderTimer += Time.deltaTime;
            if (wanderTimer >= wanderWaitTime)
            {
                wanderTimer     = 0f;
                hasWanderTarget = false;
            }
        }

        if (!hasWanderTarget)
            PickWanderTarget();

        return NodeStatus.Running;
    }

    void StartPatrol()
    {
        if (waypoints != null && waypoints.Length > 0)
            GoToWaypoint(0);
        else
            PickWanderTarget();
    }

    void GoToWaypoint(int index)
    {
        currentWaypoint = index;
        navAgent.SetDestination(waypoints[currentWaypoint].position);
    }

    void PickWanderTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        Vector3 candidate    = transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, wanderRadius, NavMesh.AllAreas))
        {
            navAgent.SetDestination(hit.position);
            hasWanderTarget = true;
        }
    }

    void BeginSearch()
    {
        searchElapsed = 0f;
        searchIndex   = 0;
        searchTimer   = 0f;
        searchPoints  = null;
    }

    void EndSearch()
    {
        heardNoise   = false;
        searchPoints = null;

        if (waypoints != null && waypoints.Length > 0)
            GoToWaypoint(currentWaypoint);
        else
        {
            hasWanderTarget = false;
            PickWanderTarget();
        }
    }

    void GenerateSearchPoints()
    {
        searchPoints    = new Vector3[searchPointCount + 1];
        searchPoints[0] = lastKnownPosition;

        for (int i = 1; i <= searchPointCount; i++)
        {
            Vector2 randomCircle = Random.insideUnitCircle * searchRadius;
            Vector3 candidate    = lastKnownPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, searchRadius, NavMesh.AllAreas))
                searchPoints[i] = hit.position;
            else
                searchPoints[i] = lastKnownPosition;
        }
    }

    void CatchPlayer()
    {
        exposureTimer = 0f;
        GameOverUI.Instance?.ShowGameOver();
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(speedParam, navAgent.velocity.magnitude);
    }

    void OnDrawGizmosSelected()
    {
        Vector3 origin = transform.position + Vector3.up * 1.5f;
        float half     = sightAngle * 0.5f;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0,  half, 0) * transform.forward * sightDistance);
        Gizmos.DrawLine(origin, origin + Quaternion.Euler(0, -half, 0) * transform.forward * sightDistance);
        Gizmos.DrawLine(origin, origin + transform.forward * sightDistance);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(origin, sightDistance * crouchSightMod);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, wanderRadius);

        if (heardNoise)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(lastKnownPosition, searchRadius);
        }

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, catchDistance);
    }
}