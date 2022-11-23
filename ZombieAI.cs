using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieAI : MonoBehaviour
{
    public enum EnemyState { Wander, idle, chase, Attack };
    [Header("Enemy state")]
    [SerializeField] public EnemyState _enemyState;

    [Header("Movement stats")]
    [SerializeField] private float _gravity = 20f;
    [SerializeField] private float _movespeed = 5f;
    [SerializeField] float _RotationSpeed;
    [SerializeField] float _TimeToLose = 20f;

    [Header("Attack stats")]
    [SerializeField] private float _AttackDamage = 15f;
    [SerializeField] private float _attackDistance = 1f;
    [SerializeField] private float _attackCooldown = 1;
    [HideInInspector]
    float OffCooldown;

    [Header("References")]
    [Tooltip("Must be a sphere collider")] [SerializeField] SphereCollider _collider;
    [HideInInspector]
    FieldOfView FOV;
    
    private Transform _Player;
    private Transform _currentTarget;
    CharacterController _controller;
    [HideInInspector]
    public PlayerController _PlayerController;
    private Vector3 _LineOfSight;

    [Header("Wandering stats")]
    [SerializeField] float _MaxDistance;
    [SerializeField] int IdleTimes;
    [HideInInspector]
    public bool IsMoving;
    int PlusOrNegative;
    bool IsCalculated;
    private float nextActionTime = 0.0f;
    [SerializeField] public float WanderingPeriods = 20f;

    [HideInInspector]
    Vector3 waypoint;

    private NavMeshAgent navmeshagent;
    [SerializeField] Transform PlayerPos;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _PlayerController = GameObject.FindObjectOfType<PlayerController>();
        navmeshagent = GetComponent<NavMeshAgent>();
        FOV = GameObject.FindGameObjectWithTag("Enemy").GetComponent<FieldOfView>();
    }

    private void Start()
    {
        IsCalculated = false;
        IsMoving = false;
        _enemyState = EnemyState.idle;
        _Player = GameObject.FindGameObjectWithTag("Player").transform;
        if (_Player == null)
        {
            Debug.LogError("No player found");
        }
        else
        {
            _currentTarget = _Player;
        }

        if (_collider == null)
        {
            Debug.LogError("No trigger collider detected on enemy " + gameObject.name);
        }
        else
        {
            _collider.radius = _attackDistance;
        }
    }

    public void Update()
    {
        if (_PlayerController.Dead())
        {
            _enemyState = EnemyState.Wander;
        }
        else
        {
            if (Time.time > nextActionTime)
            {
                nextActionTime += WanderingPeriods;
                float dist = navmeshagent.remainingDistance;
                if (dist != Mathf.Infinity && navmeshagent.pathStatus == NavMeshPathStatus.PathComplete && navmeshagent.remainingDistance == 0)
                {
                    IsCalculated = false;
                    SetNewWanderLocation();
                }
                else
                {
                    NavMeshPath navMeshPath = new NavMeshPath();
                    if (navmeshagent.CalculatePath(waypoint, navMeshPath) && navMeshPath.status == NavMeshPathStatus.PathComplete)
                    {
                    }
                    else
                    {
                        SetNewWanderLocation();
                    }
                }
            }
            if (FOV.CanSeePlayer == true)
            { 
                _enemyState = EnemyState.chase;
            }
            else if (FOV.CanSeePlayer == false && _enemyState == EnemyState.chase)
            {
                WaitForSeconds wait = new WaitForSeconds(_TimeToLose);
                if (FOV.CanSeePlayer == false)
                {
                    _enemyState = EnemyState.idle;
                }
            }
            if (IsMoving == false)
            {
                switch (_enemyState)
                {
                    case EnemyState.Wander:
                        SetNewWanderLocation();
                        break;

                    case EnemyState.idle:
                        Idle();
                        break;

                    case EnemyState.chase:
                        ChaseMovement();
                        break;

                    case EnemyState.Attack:
                        Attack();
                        break;
                }
            }
        }
    }

    void WanderMovement()
    {
        if (IsMoving == false)
        {
            IsMoving = true;
            navmeshagent.destination = waypoint;
            IsMoving = false;
        }
    }

    void SetNewWanderLocation()
    {
        if (IsCalculated == false)
        {
            float CurrentY = transform.position.y;
            PlusOrNegative = Random.Range(0, 1);
            if (PlusOrNegative == 1)
            {
                float randomX = Random.Range(-_MaxDistance, _MaxDistance);
                float RandomZ = Random.Range(-_MaxDistance, _MaxDistance);
                float CurrentXPos = transform.position.x;
                float CurrentZPos = transform.position.z;
                waypoint = new Vector3(randomX + CurrentXPos, CurrentY, RandomZ + CurrentZPos);
            }
            else
            {
                float randomX = Random.Range(-_MaxDistance, _MaxDistance);
                float RandomZ = Random.Range(-_MaxDistance, _MaxDistance);
                float CurrentXPos = transform.position.x;
                float CurrentZPos = transform.position.z;
                waypoint = new Vector3(randomX - CurrentXPos, CurrentY, RandomZ - CurrentZPos);
            }
            NavMeshPath path = new NavMeshPath();
            if (navmeshagent.CalculatePath(waypoint, path))
            {
                IsCalculated = true;
                WanderMovement();
            }
            else
            {
                SetNewWanderLocation();
            }
        }
    }

    void ChaseMovement()
    {
        navmeshagent.destination = PlayerPos.position;
    }

    void Attack()
    {
        if (Time.time > OffCooldown)
        {
            _PlayerController.TakeDamage(_AttackDamage);
            OffCooldown = Time.time + _attackCooldown;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
            if (other.CompareTag("Player"))
            {
                _enemyState = EnemyState.Attack;
            }
    }

    private void OnTriggerExit(Collider other)
    {
    }

    void Idle()
    {
        StartCoroutine(waiter());
        _enemyState = EnemyState.Wander;
    }

    IEnumerator waiter()
    {
        int wait_time = Random.Range(0, IdleTimes);
        yield return new WaitForSeconds(wait_time);
    }

    public void WalkTo(Vector3 coords)
    {
        if (IsMoving == false)
        {
            navmeshagent.destination = coords;
        }
    }
}
