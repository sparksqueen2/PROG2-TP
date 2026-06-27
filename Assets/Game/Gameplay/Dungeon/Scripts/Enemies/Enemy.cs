using UnityEngine;

public class Enemy : MonoBehaviour
{
    protected EnemyAnimation _animation;
    protected EnemyNavigation _navigation;

    protected IEnemyState _currentState;
    protected IEnemyState _previousState = null;
    protected int _previousAnimStateHash;
    protected Transform _player;

    [SerializeField] float _damageCooldownTime = 0.8f;
    [SerializeField] float _attackCooldownTime = 2.2f;
    [SerializeField] protected LayerMask _targetLayer;
    protected int _currentDamageAmount = 0;

    float _lastDamageTime;
    float _lastAttackTime;

    [SerializeField] float _moveSpeed = 3f;
    [SerializeField] float _runSpeed = 5.5f;

    public float MoveSpeed => _moveSpeed;
    public float RunSpeed => _runSpeed;

    public void ConfigureMovement(float moveSpeed, float runSpeed)
    {
        _moveSpeed = moveSpeed;
        _runSpeed = runSpeed;
        _navigation?.RefreshSpeeds();
    }

    public void RefreshPlayerTarget()
    {
        _player = PlayerTarget.Transform;
    }

    protected virtual void Awake()
    {
        _animation = GetComponent<EnemyAnimation>();
        _navigation = GetComponent<EnemyNavigation>();
    }

    protected Transform PlayerTransform
    {
        get
        {
            if (_player == null)
                _player = PlayerTarget.Transform;

            return _player;
        }
    }

    private void OnEnable()
    {
        if (_player == null)
            _player = PlayerTarget.Transform;
    }
    private void OnDisable()
    {
        EnemyManager.Instance.UnregisterEnemy(this);
    }
    protected virtual void Start()
    {
        EnemyManager.Instance.RegisterEnemy(this);
        _ = PlayerTransform;
    }

    protected float DistanceToPlayer()
    {
        if (PlayerTransform == null)
            return float.MaxValue;

        return Vector3.Distance(transform.position, PlayerTransform.position);
    }

    protected bool TryMeleeDamagePlayer(float range)
    {
        if (PlayerTransform == null || !CanDamage() || DistanceToPlayer() > range)
            return false;

        PlayerTransform.GetComponent<IDamagable>()?.Damage(_currentDamageAmount);
        DidDamage();
        return true;
    }

    public bool CanAttack() => Time.time >= _lastAttackTime + _attackCooldownTime;
    public void DidAttack() 
    {
        _lastAttackTime = Time.time;
    }

    bool CanDamage() => Time.time >= _lastDamageTime + _damageCooldownTime;

    void DidDamage()
    {
        _lastDamageTime = Time.time;
    }

    private void Update()
    {
        if (_currentState == null)
            return;

        _currentState.Execute();
    }

    public void SetState(IEnemyState newState)
    {
        _currentState = newState;
        _currentState.EnterState();
    }

    
   
    private void OnTriggerEnter(Collider other)
    {
        if (Utils.CheckLayerInMask(_targetLayer, other.gameObject.layer) && CanDamage())
        {
            IDamagable recieveDamage = other.gameObject.GetComponent<IDamagable>();
            recieveDamage?.Damage(_currentDamageAmount);
            //Debug.Log($"{gameObject.name} le hace da�o de {_currentDamageAmount} a {other.name}");
            DidDamage();
        }
    }

    public void VictoryAgainstPlayer()
    {
        SetState(new EnemyVictoryState(this, _animation));
    }

    public void Die()
    {
        SetState(new EnemyDeathState(this, _animation, _navigation));
    }

    public void TogglePause()
    {
        if(_previousState == null)
        {
            _previousState = _currentState;
            _previousAnimStateHash = _animation.GetCurrentAnimationStateHash();
            SetState(new EnemyPauseState(this, _animation, _navigation));
        }
        else
        {
            _animation.Play(_previousAnimStateHash);
            SetState(_previousState);
            _previousState = null;
        }
    }
}
