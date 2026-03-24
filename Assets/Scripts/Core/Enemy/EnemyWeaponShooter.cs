using UnityEngine;

namespace HeistNSeek.Core.Enemy
{
    /// <summary>
    /// Bridges the enemy state machine to the weapon. Receives shoot commands from ChaseState/AttackState.
    /// No real user input — state machine drives when to shoot.
    /// </summary>
    public class EnemyWeaponShooter : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private EnemyWeaponController weaponController;
        [SerializeField] private EnemyControllerBase enemyController;

        [Header("Shooting")]
        [Tooltip("Minimum seconds between shots.")]
        [SerializeField] private float shootInterval = 0.5f;

        private float _lastShootTime;
        private float _lastDebugLogTime;
        private float _lastIntervalLogTime;
        private float _lastCannotShootLogTime;

        public EnemyWeaponController WeaponController => weaponController;
        public float ShootInterval => shootInterval;

        private void Awake()
        {
            if (weaponController == null)
                weaponController = GetComponent<EnemyWeaponController>();
            if (enemyController == null)
                enemyController = GetComponent<EnemyControllerBase>();
        }

        /// <summary>
        /// Called by ChaseState/AttackState when player is detected. Shoots if cooldown elapsed.
        /// </summary>
        public void TryShoot()
        {
            #region agent log
            if (Time.time - _lastDebugLogTime >= 0.35f)
            {
                _lastDebugLogTime = Time.time;
                var ec = enemyController != null;
                var wc = weaponController != null;
                var can = wc && weaponController.CanShoot;
                var reason = wc ? weaponController.DebugShootBlockReason() : "no_weaponController";
                HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("A", "EnemyWeaponShooter.TryShoot",
                    "sample",
                    $"{{\"enemyControllerNull\":{(!ec).ToString().ToLower()},\"weaponControllerNull\":{(!wc).ToString().ToLower()},\"canShoot\":{can.ToString().ToLower()},\"blockReason\":\"{reason}\",\"go\":\"{gameObject.name}\"}}");
            }
            #endregion

            if (enemyController != null && enemyController.IsDead)
                return;

            if (weaponController == null)
            {
                #region agent log
                HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("A", "EnemyWeaponShooter.TryShoot", "exit_no_weaponController", "{}");
                #endregion
                return;
            }

            if (!weaponController.CanShoot)
            {
                #region agent log
                if (Time.time - _lastCannotShootLogTime >= 0.4f)
                {
                    _lastCannotShootLogTime = Time.time;
                    HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("B", "EnemyWeaponShooter.TryShoot", "exit_cannot_shoot",
                        $"{{\"reason\":\"{weaponController.DebugShootBlockReason()}\"}}");
                }
                #endregion
                return;
            }

            if (Time.time - _lastShootTime < shootInterval)
            {
                #region agent log
                if (Time.time - _lastIntervalLogTime >= 0.5f)
                {
                    _lastIntervalLogTime = Time.time;
                    HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("D", "EnemyWeaponShooter.TryShoot", "exit_interval",
                        $"{{\"elapsed\":{Time.time - _lastShootTime},\"need\":{shootInterval}}}");
                }
                #endregion
                return;
            }

            var target = enemyController?.CurrentTarget;
            if (target == null)
            {
                #region agent log
                HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("A", "EnemyWeaponShooter.TryShoot", "exit_no_target", "{}");
                #endregion
                return;
            }

            weaponController.AimAt(target);
            weaponController.Shoot();
            _lastShootTime = Time.time;
        }
    }
}
