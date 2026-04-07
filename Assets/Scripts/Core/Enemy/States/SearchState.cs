using UnityEngine;
using UnityEngine.AI;

namespace HeistNSeek.Core.Enemy.States
{
    /// <summary>
    /// Enemy actively searches for the player after losing sight.
    /// Goes to last known position, then checks random nearby points.
    /// If the player is not found within the search duration, returns to patrol.
    /// </summary>
    public class SearchState : IEnemyState
    {
        private readonly PatrolEnemyController _enemy;

        private Vector3 _lastKnownPosition;
        private float _searchTimer;
        private bool _reachedLastKnownPosition;
        private float _lookAroundTimer;
        private Vector3 _currentSearchPoint;
        private bool _movingToSearchPoint;

        public SearchState(PatrolEnemyController enemy)
        {
            _enemy = enemy;
        }

        public void Enter()
        {
            _lastKnownPosition = _enemy.LastKnownPlayerPosition;
            _searchTimer = 0f;
            _reachedLastKnownPosition = false;
            _lookAroundTimer = 0f;
            _movingToSearchPoint = false;

            _enemy.ResumeEnemy();
            _enemy.Agent.SetDestination(_lastKnownPosition);
        }

        public void Execute()
        {
            if (_enemy.IsDead)
                return;

            _searchTimer += Time.deltaTime;

            if (_searchTimer >= _enemy.SearchDuration)
            {
                _enemy.TransitionToPatrol();
                return;
            }

            if (!_reachedLastKnownPosition)
            {
                if (HasReachedDestination())
                {
                    _reachedLastKnownPosition = true;
                    _lookAroundTimer = 0f;
                }
                return;
            }

            SearchNearbyPoints();
        }

        public void Exit()
        {
            _enemy.ResetAgentPath();
        }

        private void SearchNearbyPoints()
        {
            if (_movingToSearchPoint)
            {
                if (HasReachedDestination())
                {
                    _movingToSearchPoint = false;
                    _lookAroundTimer = 0f;
                }
                return;
            }

            _lookAroundTimer += Time.deltaTime;

            if (_lookAroundTimer >= _enemy.SearchPointPauseDuration)
            {
                if (TryPickRandomSearchPoint(out var point))
                {
                    _currentSearchPoint = point;
                    _enemy.Agent.SetDestination(_currentSearchPoint);
                    _movingToSearchPoint = true;
                }
            }
        }

        private bool TryPickRandomSearchPoint(out Vector3 result)
        {
            for (int i = 0; i < 10; i++)
            {
                var randomDir = Random.insideUnitSphere * _enemy.SearchRadius;
                randomDir += _lastKnownPosition;

                if (NavMesh.SamplePosition(randomDir, out NavMeshHit hit, _enemy.SearchRadius, NavMesh.AllAreas))
                {
                    result = hit.position;
                    return true;
                }
            }

            result = Vector3.zero;
            return false;
        }

        private bool HasReachedDestination()
        {
            if (_enemy.Agent.pathPending)
                return false;
            return _enemy.Agent.remainingDistance <= _enemy.Agent.stoppingDistance + 0.3f;
        }
    }
}
