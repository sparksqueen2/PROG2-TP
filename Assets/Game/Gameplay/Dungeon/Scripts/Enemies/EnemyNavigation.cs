using UnityEngine;
using UnityEngine.AI;

public class EnemyNavigation : MonoBehaviour
{
    private NavMeshAgent agent;
    private Transform mover;
    private float moveSpeed;
    private float runSpeed;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        mover = transform;
        RefreshSpeeds();
    }

    public void RefreshSpeeds()
    {
        var enemy = GetComponent<Enemy>();
        if (enemy == null)
            return;

        moveSpeed = enemy.MoveSpeed;
        runSpeed = enemy.RunSpeed;
    }

    public void SetAgentDestination(Vector3 destination)
    {
        if (!TryMoveWithAgent(destination, moveSpeed))
            MoveTransformTowards(destination, moveSpeed);
    }

    public void ResetAgentDestination()
    {
        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.ResetPath();
    }

    public void MoveTowardsPlayer()
    {
        var player = PlayerTarget.Transform;
        if (player == null)
            return;

        if (!TryMoveWithAgent(player.position, runSpeed))
            MoveTransformTowards(player.position, runSpeed);
    }

    public void MoveTo(Vector3 position)
    {
        if (!TryMoveWithAgent(position, moveSpeed))
            MoveTransformTowards(position, moveSpeed);
    }

    public void EnsureReady()
    {
        DungeonNavMeshSetup.WarpAgent(agent);
        RefreshSpeeds();

        if (agent != null && agent.isActiveAndEnabled && agent.isOnNavMesh)
            agent.isStopped = false;
    }

    private bool TryMoveWithAgent(Vector3 destination, float speed)
    {
        if (agent == null || !agent.isActiveAndEnabled)
            return false;

        if (!EnsureOnNavMesh())
            return false;

        agent.isStopped = false;
        agent.updatePosition = true;
        agent.updateRotation = true;
        agent.speed = Mathf.Max(speed, 1f);
        agent.SetDestination(destination);

        if (agent.pathPending)
            return true;

        return agent.pathStatus != NavMeshPathStatus.PathInvalid;
    }

    private void MoveTransformTowards(Vector3 destination, float speed)
    {
        if (mover == null)
            return;

        var current = mover.position;
        var delta = destination - current;
        delta.y = 0f;

        var distance = delta.magnitude;
        if (distance < 0.1f)
            return;

        var step = Mathf.Max(speed, 1f) * Time.deltaTime;
        mover.position = current + delta.normalized * Mathf.Min(step, distance);

        var lookRotation = Quaternion.LookRotation(delta.normalized);
        mover.rotation = Quaternion.Slerp(mover.rotation, lookRotation, Time.deltaTime * 10f);
    }

    private bool EnsureOnNavMesh()
    {
        if (agent == null || !agent.isActiveAndEnabled)
            return false;

        if (agent.isOnNavMesh)
            return true;

        return DungeonNavMeshSetup.WarpAgent(agent);
    }
}
