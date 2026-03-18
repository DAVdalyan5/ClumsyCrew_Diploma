using UnityEngine;

namespace HeistNSeek.Core.Enemy.States
{
    /// <summary>
    /// Enemy moves between waypoints in a loop. Detects player and transitions to Chase.
    /// </summary>
    public class PatrolState : IEnemyState
    {
        private readonly PatrolEnemyController _enemy;
        private int _currentPatrolPointIndex;

        public PatrolState(PatrolEnemyController enemy)
        {
            _enemy = enemy;
        }

        public void Enter()
        {
            _enemy.ResumeEnemy();
            _currentPatrolPointIndex = 0;

            if (HasValidPatrolPoints())
            {
                SetDestinationToCurrentWaypoint();
            }
        }

        public void Execute()
        {
            if (_enemy.IsDead)
            {
                return;
            }

            if (!HasValidPatrolPoints()) return;

            if (HasReachedDestination())
            {
                AdvanceToNextWaypoint();
                SetDestinationToCurrentWaypoint();
            }
        }

        public void Exit()
        {
            _enemy.ResetAgentPath();
        }

        private bool HasValidPatrolPoints()
        {
            var points = _enemy.PatrolPoints;
            return points != null && points.Length > 0;
        }

        private bool HasReachedDestination()
        {
            if (_enemy.Agent.pathPending) return false;
            return _enemy.Agent.remainingDistance <= _enemy.PatrolArrivalThreshold;
        }

        private void AdvanceToNextWaypoint()
        {
            var points = _enemy.PatrolPoints;
            _currentPatrolPointIndex = (_currentPatrolPointIndex + 1) % points.Length;
        }

        private void SetDestinationToCurrentWaypoint()
        {
            var points = _enemy.PatrolPoints;
            var target = points[_currentPatrolPointIndex];
            if (target != null)
            {
                _enemy.Agent.SetDestination(target.position);
            }
        }
    }
}
