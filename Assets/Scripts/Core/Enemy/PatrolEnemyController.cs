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

        [Header("Search")]
        [Tooltip("How long the enemy searches for the player before giving up.")]
        [SerializeField] private float searchDuration = 8f;
        [Tooltip("Radius around last known position to check random points.")]
        [SerializeField] private float searchRadius = 6f;
        [Tooltip("How long the enemy pauses at each search point before moving to the next.")]
        [SerializeField] private float searchPointPauseDuration = 1.5f;

        private IdleState _idleState;
        private PatrolState _patrolState;
        private ChaseState _chaseState;
        private AttackState _attackState;
        private SearchState _searchState;

        public Transform[] PatrolPoints => patrolPoints;
        public float IdleDuration => idleDuration;
        public float PatrolArrivalThreshold => patrolArrivalThreshold;
        public float SearchDuration => searchDuration;
        public float SearchRadius => searchRadius;
        public float SearchPointPauseDuration => searchPointPauseDuration;

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
            _searchState = new SearchState(this);
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

        public void TransitionToSearch()
        {
            ChangeState(_searchState);
        }

        protected override void OnPlayerDetectedInternal()
        {
            TransitionToChase();
        }

        protected override void OnAllPlayersLeftInternal()
        {
            if (HasEverSeenPlayer)
                TransitionToSearch();
            else
                TransitionToPatrol();
        }
    }
}
