using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(MonsterVision))]
public class MonsterAI : MonoBehaviour
{
    public enum State { Idle, Patrol, Chase, Search }

    public const float DefaultPatrolSpeed = 3.3f;
    public const float DefaultChaseSpeed = 9.24f;
    public const float DefaultSearchSpeed = 9.9f;

    [SerializeField] private float idleSpeed = 0f;
    [SerializeField] private float patrolSpeed = DefaultPatrolSpeed;
    [SerializeField] private float chaseSpeed = DefaultChaseSpeed;
    [SerializeField] private float searchSpeed = DefaultSearchSpeed;
    [SerializeField] private Transform[] waypoints;
    [SerializeField] private float searchDuration = 6f;
    [SerializeField] private float huntMemoryDuration = 30f;

    private NavMeshAgent _agent;
    private MonsterVision _vision;
    private State _currentState = State.Idle;
    private int _waypointIndex;
    private float _searchTimer;
    private float _huntTimer;
    private Vector3 _lastKnownPosition;
    private bool _lockedOn;
    private bool _catchPaused;
    private bool _wakePaused;

    public State CurrentState => _currentState;
    public bool IsLockedOn => _lockedOn;

    public void Configure(Transform[] patrolPoints) => waypoints = patrolPoints;

    public void ConfigureSpeeds(float patrol, float chase, float search)
    {
        patrolSpeed = patrol;
        chaseSpeed = chase;
        searchSpeed = search;
    }

    public void BeginPatrol()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        _waypointIndex = 0;
        _currentState = State.Patrol;

        if (_agent != null && _agent.isOnNavMesh)
            _agent.SetDestination(waypoints[_waypointIndex].position);
    }

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _vision = GetComponent<MonsterVision>();
        _agent.updateRotation = true;
        _agent.acceleration = 16f;
        _agent.angularSpeed = 420f;
    }

    public static void PauseAllForCatch(MonsterAI attacker, Transform player)
    {
        foreach (var ai in Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude))
        {
            if (ai == attacker)
                ai.BeginCatchFacing(player);
            else
                ai.SetCatchPaused(true);
        }
    }

    public static void PauseAllForWake()
    {
        foreach (var ai in Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude))
            ai.SetWakePaused(true);
    }

    public static void ResumeAllAfterWake()
    {
        foreach (var ai in Object.FindObjectsByType<MonsterAI>(FindObjectsInactive.Exclude))
            ai.SetWakePaused(false);
    }

    public void SetWakePaused(bool paused)
    {
        _wakePaused = paused;
        if (paused)
        {
            SetCatchPaused(true);
            return;
        }

        _catchPaused = false;
        if (_agent != null && _agent.isOnNavMesh)
            _agent.isStopped = false;

        if (waypoints == null || waypoints.Length == 0)
            return;

        if (_currentState == State.Idle || _currentState == State.Patrol)
            BeginPatrol();
    }

    public void BeginCatchFacing(Transform player)
    {
        _catchPaused = true;
        if (_agent.isOnNavMesh)
        {
            _agent.isStopped = true;
            _agent.ResetPath();
        }
        _agent.velocity = Vector3.zero;

        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(dir.normalized);
    }

    public void SetCatchPaused(bool paused)
    {
        _catchPaused = paused;
        if (!paused) return;

        if (_agent.isOnNavMesh)
            _agent.isStopped = true;
        _agent.velocity = Vector3.zero;
    }

    private void Update()
    {
        if (_catchPaused || _wakePaused || DimensionWakeState.IsActive) return;

        if (_vision.CanSeePlayer(out Transform visiblePlayer)
            || _vision.SensePlayerNearby(out visiblePlayer))
            LockOn(visiblePlayer.position);

        if (_lockedOn)
        {
            UpdateChase();
            return;
        }

        switch (_currentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Patrol: UpdatePatrol(); break;
            case State.Chase: UpdateChase(); break;
            case State.Search: UpdateSearch(); break;
        }
    }

    private void SetAgentStopped(bool stopped)
    {
        if (_agent == null || !_agent.isOnNavMesh)
            return;

        _agent.isStopped = stopped;
    }

    private void UpdateIdle()
    {
        SetAgentStopped(false);
        _agent.speed = idleSpeed;
        if (_agent.isOnNavMesh)
            _agent.velocity = Vector3.zero;
    }

    private void UpdatePatrol()
    {
        SetAgentStopped(false);
        _agent.speed = patrolSpeed;

        if (!_agent.isOnNavMesh || waypoints == null || waypoints.Length == 0) return;

        if (!_agent.pathPending && _agent.remainingDistance < 0.6f)
        {
            _waypointIndex = (_waypointIndex + 1) % waypoints.Length;
            _agent.SetDestination(waypoints[_waypointIndex].position);
        }
    }

    private void UpdateChase()
    {
        if (!_agent.isOnNavMesh) return;

        SetAgentStopped(false);
        _agent.speed = chaseSpeed;

        if (_vision.CanSeePlayer(out Transform player))
        {
            _lastKnownPosition = player.position;
            _huntTimer = huntMemoryDuration;
            _agent.SetDestination(player.position);
            SetState(State.Chase);
            return;
        }

        if (_vision.TryGetPlayer(out Transform huntedPlayer))
        {
            _huntTimer = huntMemoryDuration;
            _lastKnownPosition = huntedPlayer.position;
            _agent.SetDestination(huntedPlayer.position);
            SetState(State.Chase);
            return;
        }

        _huntTimer -= Time.deltaTime;
        _agent.SetDestination(_lastKnownPosition);
        if (_huntTimer <= 0f)
        {
            ReleaseLockOn();
            return;
        }

        SetState(_huntTimer > 0f ? State.Search : State.Idle);
    }

    private void UpdateSearch()
    {
        if (!_agent.isOnNavMesh) return;

        SetAgentStopped(false);
        _agent.speed = searchSpeed;

        if (_vision.CanSeePlayer(out Transform player))
        {
            LockOn(player.position);
            return;
        }

        _huntTimer -= Time.deltaTime;
        if (_huntTimer <= 0f)
        {
            ReleaseLockOn();
            return;
        }

        _searchTimer -= Time.deltaTime;
        if (_searchTimer <= 0f)
            _agent.SetDestination(_lastKnownPosition);
    }

    private void LockOn(Vector3 playerPosition)
    {
        _lockedOn = true;
        _huntTimer = huntMemoryDuration;
        _lastKnownPosition = playerPosition;
        _searchTimer = searchDuration;
        if (!_agent.isOnNavMesh) return;

        SetAgentStopped(false);
        SetState(State.Chase);
        _agent.SetDestination(playerPosition);
    }

    private void ReleaseLockOn()
    {
        if (!_lockedOn) return;

        _lockedOn = false;
        BeginPatrol();
    }

    private void SetState(State next)
    {
        if (_currentState == next) return;
        _currentState = next;
        if (next == State.Search)
            _searchTimer = searchDuration;
    }
}
