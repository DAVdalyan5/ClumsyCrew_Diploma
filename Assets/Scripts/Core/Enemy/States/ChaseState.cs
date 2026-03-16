using UnityEngine;

namespace HeistNSeek.Core.Enemy.States
{
    /// <summary>
    /// Enemy follows the player. In melee range → Attack. Lost player → Patrol.
    /// </summary>
    public class ChaseState : IEnemyState
    {
        private readonly PatrolEnemyController _enemy;

        public ChaseState(PatrolEnemyController enemy)
        {
            _enemy = enemy;
        }

        public void Enter()
        {
            _enemy.ResumeEnemy();
        }

        public void Execute()
        {
            if (_enemy.IsDead)
            {
                return;
            }

            var player = _enemy.Player;
            if (player == null)
            {
                _enemy.TransitionToPatrol();
                return;
            }

            if (_enemy.IsInMeleeAttackRange())
            {
                _enemy.TransitionToAttack();
                return;
            }

            if (!_enemy.CanDetectPlayer())
            {
                _enemy.TransitionToPatrol();
                return;
            }

            if (_enemy.WeaponShooter != null && _enemy.IsInShootRange())
            {
                _enemy.WeaponShooter.TryShoot();
            }

            _enemy.Agent.SetDestination(player.position);
        }

        public void Exit()
        {
            _enemy.ResetAgentPath();
        }
    }
}
