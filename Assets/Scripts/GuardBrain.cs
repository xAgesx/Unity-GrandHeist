using UnityEngine;
using UnityEngine.AI;

public class GuardBrain : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform[] waypoints;
    private int destPoint = 0;

    private enum State { Patrol, Investigate, Chase }
    private State currentState = State.Patrol;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        GoToNextWaypoint();
    }

    void Update()
    {
        if (currentState == State.Patrol && !agent.pathPending && agent.remainingDistance < 0.5f)
            GoToNextWaypoint();
    }

    void GoToNextWaypoint()
    {
        if (waypoints.Length == 0) return;
        agent.destination = waypoints[destPoint].position;
        destPoint = (destPoint + 1) % waypoints.Length;
    }

    public void OnHearNoise(Vector3 noisePosition)
    {
        if (currentState == State.Chase) return; // Don't distract if already chasing
        
        currentState = State.Investigate;
        agent.destination = noisePosition;
        Invoke(nameof(ResumePatrol), 5f); // Investigate for 5 seconds, then return
    }

    public void OnGlobalAlarm(Vector3 playerPosition)
    {
        CancelInvoke(nameof(ResumePatrol));
        currentState = State.Chase;
        agent.speed = 8f; // Run
        agent.destination = playerPosition;
    }

    void ResumePatrol()
    {
        currentState = State.Patrol;
        GoToNextWaypoint();
    }
}