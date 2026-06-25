using UnityEngine;
using UnityEngine.AI;

public class EnemyChestController : Enemy
{
    private const float MinFootGroundOffset = 0.52f;
    private static readonly int IdleBattleHash = Animator.StringToHash("IdleBattle");

    [SerializeField] float _detectItemRange = 4f;
    [SerializeField] float _detectPlayerRange = 8f;
    [SerializeField] float _idleDistance = 14f;
    [SerializeField] float _attackRange = 2f;
    [SerializeField] float _idleTimeout = 3f;
    [SerializeField] Collider _hitAttackCollider;
    [SerializeField] int[] _attackDamageAmount;
    public int[] AvailableAttacks => _attackDamageAmount;
    public float IdleTimeout => _idleTimeout;
    public bool IsAttacking { get; private set; }
    [SerializeField] Transform _itemToProtect;

    private bool _isWaveCombat;
    private float _spawnGroundY;
    private NavMeshAgent _agent;

    public Transform ItemToProtect => _itemToProtect;
    public bool IsWaveCombat => _isWaveCombat;

    protected override void Awake()
    {
        base.Awake();
        _agent = GetComponent<NavMeshAgent>();
        if (_itemToProtect == null)
            _itemToProtect = transform;
    }

    protected override void Start()
    {
        base.Start();
        DisableAttack();

        if (_isWaveCombat)
            ForceAlertPose();

        SetState(new EnemyChestIdleState(this, _animation, _navigation));
    }

    private void LateUpdate()
    {
        if (!_isWaveCombat || _agent == null || _agent.updatePosition)
            return;

        var position = transform.position;
        if (Mathf.Abs(position.y - _spawnGroundY) > 0.01f)
            transform.position = new Vector3(position.x, _spawnGroundY, position.z);
    }

    public bool IsPlayerClose() => DistanceToPlayer() < _detectPlayerRange;

    public bool IsPlayerFar() => DistanceToPlayer() > _idleDistance;

    public bool IsNearItemToProtect() => Vector3.Distance(transform.position, _itemToProtect.position) < _detectItemRange;

    public bool IsPlayerInAttackRange() => DistanceToPlayer() <= _attackRange;

    public void LookAtCollectible()
    {
        LookAtTarget(_itemToProtect.position);
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

    public void ConfigureDetection(float detectRange, float idleDistance)
    {
        _detectPlayerRange = detectRange;
        _idleDistance = idleDistance;
    }

    public void ConfigureWaveSpawn(bool placedOnNavMesh = true)
    {
        _isWaveCombat = true;
        _itemToProtect = transform;
        AlignFeetToGround();
        _spawnGroundY = transform.position.y;
        SetWaveAgentLocked(true, placedOnNavMesh);
        ForceAlertPose();
    }

    public void ForceAlertPose()
    {
        var animator = GetComponent<Animator>();
        if (animator == null)
            return;

        animator.Play(IdleBattleHash, 0, 0f);
        animator.Update(0f);
    }

    public void MaintainWaveIdlePose()
    {
        if (!_isWaveCombat)
            return;

        var animator = GetComponent<Animator>();
        if (animator == null)
            return;

        var state = animator.GetCurrentAnimatorStateInfo(0);
        if (state.shortNameHash == IdleBattleHash)
            return;

        animator.Play(IdleBattleHash, 0, 0f);
    }

    public void SetWaveAgentLocked(bool locked, bool agentOnNavMesh = true)
    {
        if (_agent == null)
            return;

        _agent.updatePosition = !locked;
        _agent.updateRotation = !locked;

        if (locked && agentOnNavMesh && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            _agent.ResetPath();
    }

    private void AlignFeetToGround()
    {
        GetComponent<Animator>()?.Update(0f);

        var groundY = DungeonGroundSnap.GetGroundY(transform.position);
        var correction = groundY - GetVisualBottomY();
        if (correction <= 0.02f)
            correction = MinFootGroundOffset;

        if (_agent != null)
            _agent.baseOffset += correction;
        else
            transform.position += Vector3.up * correction;
    }

    private float GetVisualBottomY()
    {
        var hasBounds = false;
        var bounds = new Bounds(transform.position, Vector3.zero);

        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
        {
            if (!hasBounds)
            {
                bounds = renderer.bounds;
                hasBounds = true;
                continue;
            }

            bounds.Encapsulate(renderer.bounds);
        }

        return hasBounds ? bounds.min.y : transform.position.y;
    }
}
