using UnityEngine;

namespace HeistNSeek.Core.Enemy.States
{
    /// <summary>
    /// Enemy performs attack (placeholder). After cooldown, deals damage and returns to Chase.
    /// </summary>
    public class AttackState : IEnemyState
    {
        private readonly PatrolEnemyController _enemy;
        private float _attackTimer;

        public AttackState(PatrolEnemyController enemy)
        {
            _enemy = enemy;
        }

        public void Enter()
        {
            _enemy.StopEnemy();
            _attackTimer = 0f;
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

            if (_enemy.WeaponShooter != null && _enemy.IsInShootRange() && !_enemy.IsInMeleeAttackRange())
            {
                _enemy.WeaponShooter.TryShoot();
            }

            _attackTimer += Time.deltaTime;

            if (_attackTimer >= _enemy.AttackCooldown)
            {
                PerformAttack();
                _enemy.TransitionToChase();
            }
        }

        public void Exit()
        {
            _enemy.ResumeEnemy();
        }

        private void PerformAttack()
        {
            if (_enemy.IsDead)
            {
                return;
            }

            var player = _enemy.Player;
            if (player != null && _enemy.IsInMeleeAttackRange())
            {
                _enemy.PublishEnemyAttack(player);
                _enemy.KillPlayer();
            }
        }
    }
}
