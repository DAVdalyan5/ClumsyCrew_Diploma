using UnityEngine;

namespace HeistNSeek.Core.Enemy.States
{
    /// <summary>
    /// Enemy waits (optional delay) before starting patrol.
    /// </summary>
    public class IdleState : IEnemyState
    {
        private readonly PatrolEnemyController _enemy;
        private float _idleTimer;

        public IdleState(PatrolEnemyController enemy)
        {
            _enemy = enemy;
        }

        public void Enter()
        {
            _idleTimer = 0f;
            _enemy.StopEnemy();
        }

        public void Execute()
        {
            if (_enemy.IsDead)
            {
                return;
            }

            _idleTimer += Time.deltaTime;

            if (_idleTimer >= _enemy.IdleDuration)
            {
                _enemy.TransitionToPatrol();
            }
        }

        public void Exit()
        {
            // No cleanup needed
        }
    }
}
