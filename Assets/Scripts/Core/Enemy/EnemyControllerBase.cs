using Easy.MessageHub;
using HeistNSeek.Core.Enemy.Events;
using HeistNSeek.Core.Enemy.States;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace HeistNSeek.Core.Enemy
{
    /// <summary>
    /// Base enemy controller with NavMeshAgent, detection, and state machine.
    /// Adapted from ah-light EnemyControllerBase.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public abstract class EnemyControllerBase : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] protected float detectionRange = 10f;
        [SerializeField] protected float meleeAttackRange = 1.5f;
        [SerializeField] protected float attackRange = 1.5f;
        [SerializeField] protected float shootRange = 15f;
        [SerializeField] protected float attackCooldown = 1f;

        [Header("Combat")]
        [SerializeField] protected EnemyCombatMode combatMode = EnemyCombatMode.Melee;
        [Tooltip("NavMesh stopping distance while chasing in Ranged mode. 0 = use 65% of Shoot Range.")]
        [SerializeField] protected float rangedCombatStoppingDistance;

        [Header("Ranged Combat")]
        [SerializeField] protected EnemyWeaponShooter weaponShooter;

        protected NavMeshAgent _agent;
        protected NetworkedEnemyHealth _health;
        protected IEnemyState _currentState;
        protected IMessageHub _messageHub;
        private bool _didHandleDeath;
        private float _cachedDefaultStoppingDistance;

        public NavMeshAgent Agent => _agent;
        public Transform Transform => transform;
        public Transform CurrentTarget { get; set; }
        public float DetectionRange => detectionRange;
        public float MeleeAttackRange => meleeAttackRange;
        public float AttackRange => attackRange;
        public float ShootRange => shootRange;
        public float AttackCooldown => attackCooldown;
        public bool IsDead => _health != null && _health.IsDead;
        public bool IsAlive => !IsDead;
        public EnemyWeaponShooter WeaponShooter => weaponShooter;
        public EnemyCombatMode CombatMode => combatMode;
        public bool UsesMeleeContactAttack => combatMode == EnemyCombatMode.Melee;

        [Inject]
        protected void Construct(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        /// <summary>Called by EnemyDetectionTrigger when a player enters the detection zone.</summary>
        public void OnPlayerDetected(Transform target)
        {
            CurrentTarget = target;
            PublishPlayerSpotted(target);
            OnPlayerDetectedInternal();
        }

        /// <summary>Called by EnemyDetectionTrigger when all players leave the detection zone.</summary>
        public void OnAllPlayersLeft()
        {
            CurrentTarget = null;
            OnAllPlayersLeftInternal();
        }

        protected virtual void OnPlayerDetectedInternal() { }
        protected virtual void OnAllPlayersLeftInternal() { }

        protected virtual void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _health = GetComponent<NetworkedEnemyHealth>();
            if (_agent != null)
                _cachedDefaultStoppingDistance = _agent.stoppingDistance;
        }

        public void ApplyChaseEngagementStoppingDistance()
        {
            if (_agent == null || combatMode != EnemyCombatMode.Ranged)
                return;

            float stopDist = rangedCombatStoppingDistance > 0f
                ? rangedCombatStoppingDistance
                : shootRange * 0.65f;
            _agent.stoppingDistance = Mathf.Max(0.5f, stopDist);
        }

        public void RestoreDefaultStoppingDistance()
        {
            if (_agent == null)
                return;
            _agent.stoppingDistance = _cachedDefaultStoppingDistance;
        }

        protected virtual void Start()
        {
            CreateStates();
        }

        protected virtual void Update()
        {
            if (!IsServerOrStandalone())
                return;

            if (IsDead)
            {
                HandleDeathState();
                return;
            }

            _currentState?.Execute();
        }

        private void HandleDeathState()
        {
            if (_didHandleDeath)
                return;

            _didHandleDeath = true;
            _currentState?.Exit();
            _currentState = null;

            ResetAgentPath();
            StopEnemy();

            if (weaponShooter != null)
            {
                weaponShooter.enabled = false;
            }
        }

        private bool IsServerOrStandalone()
        {
            var networkManager = NetworkManager.Singleton;
            if (networkManager == null || !networkManager.IsListening)
                return true;
            return networkManager.IsServer;
        }

        public bool IsInMeleeAttackRange()
        {
            var target = CurrentTarget;
            if (target == null) return false;

            float distance = Vector3.Distance(transform.position, target.position);
            return distance <= meleeAttackRange;
        }

        public bool IsInAttackRange()
        {
            var target = CurrentTarget;
            if (target == null) return false;

            float distance = Vector3.Distance(transform.position, target.position);
            return distance <= attackRange;
        }

        public bool IsInShootRange()
        {
            var target = CurrentTarget;
            if (target == null) return false;

            float distance = Vector3.Distance(transform.position, target.position);
            return distance <= shootRange;
        }

        public void KillPlayer()
        {
            Debug.Log("[Enemy] Player killed!");
            _messageHub?.Publish(new PlayerKilledEvent(this));
        }

        public void PublishPlayerSpotted(Transform player)
        {
            _messageHub?.Publish(new PlayerSpottedEvent(this, player));
        }

        public void PublishEnemyAttack(Transform target)
        {
            _messageHub?.Publish(new EnemyAttackEvent(this, target));
        }

        public void StopEnemy()
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = true;
            }
        }

        public void ResumeEnemy()
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.isStopped = false;
            }
        }

        public void ResetAgentPath()
        {
            if (_agent != null && _agent.isActiveAndEnabled && _agent.isOnNavMesh)
            {
                _agent.ResetPath();
            }
        }

        public void SetInitialPosition(Vector3 position)
        {
            if (_agent != null && _agent.isActiveAndEnabled)
            {
                _agent.Warp(position);
            }
            else
            {
                transform.position = position;
            }
        }

        protected abstract void CreateStates();

        protected void ChangeState(IEnemyState newState)
        {
            _currentState?.Exit();
            _currentState = newState;
            _currentState?.Enter();
        }

        protected virtual void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, meleeAttackRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, shootRange);
        }
    }
}
