using System.Collections.Generic;
using UnityEngine;

public class EnemyWatcherController : Enemy
{
    [SerializeField] float _detectPlayerRange = 9f;
    [SerializeField] float _losePlayerRange = 15f;
    [SerializeField] float _attackRange = 2f;
    [SerializeField] float _idleDuration = 1f;
    [SerializeField] List<Transform> _patrolPoints;
    

    private int _currentPatrolIndex = -1;
    [SerializeField] float _idleTimeout = 3f;
    [SerializeField] Collider _hitAttackCollider;
    [SerializeField] int[] _attackDamageAmount;

    public int[] AvailableAttacks => _attackDamageAmount;
    public float IdleTimeout => _idleTimeout;
    public bool IsAttacking { get; private set; }


    protected override void Start()
    {
        base.Start();
        DisableAttack();
        SetState(new EnemyWatcherIdleState(this, _animation, _idleDuration, _navigation));
    }

    public bool HasSufficientPatrolPoints() => _patrolPoints != null && _patrolPoints.Count > 1;

    public bool IsPlayerClose() => DistanceToPlayer() < _detectPlayerRange;
    public bool IsPlayerLost() => DistanceToPlayer() >= _losePlayerRange;
    public bool IsPlayerInAttackRange() => DistanceToPlayer() <= _attackRange;
    public bool IsNearPosition(Vector3 targetPosition) => Vector2.Distance(new Vector2(transform.position.x, transform.position.z), new Vector2(targetPosition.x, targetPosition.z)) <= 0.2f;

        
    public Transform GetNextPatrolPoint()
    {
        if (!HasSufficientPatrolPoints())
        {
            Debug.LogWarning("No hay suficientes puntos de patrulla disponibles.");
            return transform;
        }
        int randomIndex;
        do
        {
            randomIndex = Random.Range(0, _patrolPoints.Count);
        } while (randomIndex == _currentPatrolIndex);

        _currentPatrolIndex = randomIndex;
        return _patrolPoints[_currentPatrolIndex];
    }

    public Vector3 MoveTowardsNexttPatrolPoint()
    {
        Vector3 position = GetNextPatrolPoint().position;
        _navigation.MoveTo(position);
        return position;
    }

    public void LootAtPlayer()
    {
        if (PlayerTransform != null)
            LookAtTarget(PlayerTransform.position);
    }

    void LookAtTarget(Vector3 target)
    {
        Vector3 direction = (target - transform.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
    }

    public Transform GetPlayer() => PlayerTransform;

    public void EnableAttack(int attackNumber)
    {
        _currentDamageAmount = _attackDamageAmount[attackNumber - 1];
        if (_hitAttackCollider != null)
            _hitAttackCollider.enabled = true;
        IsAttacking = true;
        TryMeleeDamagePlayer(_attackRange + 0.75f);
    }

    public void DisableAttack()
    {
        if (_hitAttackCollider != null)
            _hitAttackCollider.enabled = false;
        IsAttacking = false;
    }

    public void ConfigureDetection(float detectRange, float loseRange)
    {
        _detectPlayerRange = detectRange;
        _losePlayerRange = loseRange;
    }

    public void ConfigureWaveSpawn()
    {
        _patrolPoints?.Clear();
    }
}
