using UnityEngine;

namespace HeistNSeek.Core.Enemy.States
{
    /// <summary>
    /// Enemy follows the player. Melee: in melee range → Attack. Ranged: follow + shoot, no contact finisher. Lost player → Patrol.
    /// </summary>
    public class ChaseState : IEnemyState
    {
        private readonly PatrolEnemyController _enemy;
        private float _lastChaseDebugTime;

        public ChaseState(PatrolEnemyController enemy)
        {
            _enemy = enemy;
        }

        public void Enter()
        {
            _enemy.ResumeEnemy();
            _enemy.ApplyChaseEngagementStoppingDistance();
        }

        public void Execute()
        {
            if (_enemy.IsDead)
            {
                return;
            }

            var target = _enemy.CurrentTarget;
            if (target == null)
            {
                _enemy.TransitionToPatrol();
                return;
            }

            if (_enemy.UsesMeleeContactAttack && _enemy.IsInMeleeAttackRange())
            {
                _enemy.TransitionToAttack();
                return;
            }

            #region agent log
            if (UnityEngine.Time.time - _lastChaseDebugTime >= 0.4f)
            {
                _lastChaseDebugTime = UnityEngine.Time.time;
                float d = UnityEngine.Vector3.Distance(_enemy.transform.position, target.position);
                bool inShoot = _enemy.IsInShootRange();
                bool hasShooter = _enemy.WeaponShooter != null;
                HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("C", "ChaseState.Execute", "chase_snapshot",
                    $"{{\"dist\":{d},\"inShootRange\":{inShoot.ToString().ToLower()},\"hasWeaponShooter\":{hasShooter.ToString().ToLower()},\"combatMode\":\"{_enemy.CombatMode}\",\"shootRange\":{_enemy.ShootRange},\"stoppingDist\":{_enemy.Agent.stoppingDistance}}}");
            }
            #endregion

            if (_enemy.WeaponShooter != null && _enemy.IsInShootRange())
            {
                _enemy.WeaponShooter.TryShoot();
            }

            _enemy.Agent.SetDestination(target.position);
        }

        public void Exit()
        {
            _enemy.RestoreDefaultStoppingDistance();
            _enemy.ResetAgentPath();
        }
    }
}
