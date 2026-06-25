using UnityEngine;

public class EnemyBeholderController : Enemy
{
    [SerializeField] float _startChaseDistance = 9f;
    [SerializeField] float _stopChaseDistance = 15f;
    [SerializeField] float _attackDistance = 2f;
    [SerializeField] float _idleTimeout = 3f;
    [SerializeField] Collider _hitAttackCollider;
    [SerializeField] Collider _shockAttackCollider;
    [SerializeField] int[] _attackDamageAmount;
    public Vector3 HomePosition { get; private set; }
    public int[] AvailableAttacks => _attackDamageAmount;

    public float IdleTimeout => _idleTimeout;
    public bool IsAttacking { get; private set; }

    

    protected override void Awake()
    {
        base.Awake();
        RefreshHomePosition();
    }

    public void RefreshHomePosition()
    {
        HomePosition = transform.position;
    }

    protected override void Start()
    {
        base.Start();
        DisableAttack();
        SetState(new EnemyBeholderIdleState(this, _animation, _navigation));
    }


    public Transform GetPlayer() => _player;

    public bool IsPlayerClose() => DistanceToPlayer() < _startChaseDistance;

    public bool IsPlayerFar() => DistanceToPlayer() > _stopChaseDistance;
        
    public bool IsPlayerInAttackRange() => DistanceToPlayer() <= _attackDistance;

    public bool IsAtHome() => Vector3.Distance(transform.position, HomePosition) < 0.2f;

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
   

    public void EnableAttack(int attackNumber)
    {
        _currentDamageAmount = _attackDamageAmount[attackNumber - 1];
        if (attackNumber % 2 == 0)
        {
            if (_shockAttackCollider != null)
                _shockAttackCollider.enabled = true;
        }
        else if (_hitAttackCollider != null)
        {
            _hitAttackCollider.enabled = true;
        }

        IsAttacking = true;
        TryMeleeDamagePlayer(_attackDistance + 0.75f);
    }

    public void DisableAttack()
    {
        if (_hitAttackCollider != null)
            _hitAttackCollider.enabled = false;
        if (_shockAttackCollider != null)
            _shockAttackCollider.enabled = false;
        IsAttacking = false;
    }

    public void ConfigureDetection(float startChaseDistance, float stopChaseDistance)
    {
        _startChaseDistance = startChaseDistance;
        _stopChaseDistance = stopChaseDistance;
    }
}
