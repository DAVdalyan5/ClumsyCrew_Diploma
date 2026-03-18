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
            if (enemyController != null && enemyController.IsDead)
                return;

            if (weaponController == null || !weaponController.CanShoot)
                return;

            if (Time.time - _lastShootTime < shootInterval)
                return;

            var target = enemyController?.CurrentTarget;
            if (target == null)
                return;

            weaponController.AimAt(target);
            weaponController.Shoot();
            _lastShootTime = Time.time;
        }
    }
}
