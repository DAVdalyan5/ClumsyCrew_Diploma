using HeistNSeek.Core.Enemy.States;
using UnityEngine;

namespace HeistNSeek.Core.Enemy
{
    /// <summary>
    /// Enemy controller with Idle, Patrol, Chase, and Attack states.
    /// Uses waypoint-based patrol loop.
    /// Requires: NavMesh baked in scene, GameObject with "Player" tag for targeting.
    /// Place under GameplayLifetimeScope hierarchy for DI injection.
    /// </summary>
    public class PatrolEnemyController : EnemyControllerBase
    {
        [Header("Patrol")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField] private float idleDuration;
        [SerializeField] private float patrolArrivalThreshold = 0.5f;

        private IdleState _idleState;
        private PatrolState _patrolState;
        private ChaseState _chaseState;
        private AttackState _attackState;

        public Transform[] PatrolPoints => patrolPoints;
        public float IdleDuration => idleDuration;
        public float PatrolArrivalThreshold => patrolArrivalThreshold;

        protected override void Start()
        {
            base.Start();
            StartActivity();
        }

        protected override void CreateStates()
        {
            _idleState = new IdleState(this);
            _patrolState = new PatrolState(this);
            _chaseState = new ChaseState(this);
            _attackState = new AttackState(this);
        }

        public void StartActivity()
        {
            ChangeState(_idleState);
        }

        public void TransitionToPatrol()
        {
            ChangeState(_patrolState);
        }

        public void TransitionToChase()
        {
            ChangeState(_chaseState);
        }

        public void TransitionToAttack()
        {
            ChangeState(_attackState);
        }

        protected override void OnPlayerDetectedInternal()
        {
            TransitionToChase();
        }

        protected override void OnAllPlayersLeftInternal()
        {
            TransitionToPatrol();
        }
    }
}
