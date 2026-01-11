using UnityEngine;

namespace cowsins
{
    public class Turret : MonoBehaviour
    {
        public enum TargetType
        {
            Player,
            Enemies
        }

        [SerializeField, Title("Target Settings"), Tooltip("Choose whether this turret targets the Player or Enemies.")]
        private TargetType targetType = TargetType.Player;

        [SerializeField, Title("References")] private bool displayGizmos = true;
        [SerializeField] private Animator animator;
        [SerializeField, Tooltip("The part of the turret that rotates.")] private Transform turretHead;

        [SerializeField, Tooltip("Distance within the player is detectable. If displayGizmos is true this will be visible in the Editor."), Title("Basic Settings")] private float detectionRange = 10f;
        [SerializeField, Tooltip("Enable vertical movement.")] private bool allowVerticalMovement = false;
        [SerializeField, Tooltip("Speed of rotation interpolation.")] private float lerpSpeed = 5f;

        [SerializeField, Title("Projectile Settings")] private GameObject projectilePrefab;
        [SerializeField, Min(0)] private float projectileSpeed, projectileDamage, projectileDuration;
        [SerializeField] private Transform firePoint;
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField, Tooltip("Shots per second."), Title("Shooting")] private float fireRate = 2f;

        [SerializeField, Title("Pool Settings")] private int projectilePoolSize;

        private bool canShoot = false;
        private float fireCooldown = 0f;
        private Transform target;

        Vector3 targetDirection;
        Quaternion targetRotation;

        private void Start()
        {
            InitializeTarget();
            PoolManager.Instance.RegisterPool(projectilePrefab, projectilePoolSize);
            PoolManager.Instance.RegisterPool(muzzleFlash, projectilePoolSize);
        }

        private void Update()
        {
            UpdateTarget();
            if (target == null) return;

            Vector3 targetDir = target.position - transform.position;
            if (!allowVerticalMovement) targetDir.y = 0f;

            if (targetDir.magnitude <= detectionRange)
            {
                AimAtTarget(targetDir);
                Shoot(targetDir);
            }
            else
            {
                canShoot = false;
                fireCooldown = fireRate;
            }
        }

        private void InitializeTarget()
        {
            if (targetType == TargetType.Player)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null) target = player.transform;
            }
        }

        private void UpdateTarget()
        {
            if (targetType == TargetType.Enemies)
            {
                FindClosestEnemy();
            }
        }

        private void AimAtTarget(Vector3 direction)
        {
            canShoot = true;
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            turretHead.rotation = Quaternion.RotateTowards(turretHead.rotation, targetRotation, lerpSpeed * 100f * Time.deltaTime);
        }

        private void Shoot(Vector3 direction)
        {
            fireCooldown -= Time.deltaTime;
            if (!canShoot || fireCooldown > 0) return;

            fireCooldown = fireRate;
            animator?.SetTrigger("Fire");
            PoolManager.Instance.GetFromPool(muzzleFlash, firePoint.position, Quaternion.identity);

            var projGO = PoolManager.Instance.GetFromPool(projectilePrefab, firePoint.position, Quaternion.identity);
            var projectile = projGO.GetComponent<TurretProjectile>();
            projectile.Initialize(targetType, direction.normalized, projectileDamage, projectileSpeed, projectileDuration, ReturnProjectileToPool);
        }

        private void ReturnProjectileToPool(TurretProjectile proj)
        {
            proj.destroyEvent.RemoveAllListeners();
            PoolManager.Instance?.ReturnToPool(proj.gameObject, projectilePrefab);
        }

        private void FindClosestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            float minDistance = Mathf.Infinity;
            target = null;

            foreach (var e in enemies)
            {
                if (e == this.gameObject) continue;

                EnemyHealth enemyHealth = e.GetComponent<EnemyHealth>();
                if (enemyHealth == null) continue;

                // Skip dead enemies
                if (enemyHealth.IsDead) continue;
                float dist = Vector3.Distance(transform.position, e.transform.position);
                if (dist < minDistance)
                {
                    minDistance = dist;
                    target = e.transform;
                }
            }
        }

        // Draw Gizmos
        private void OnDrawGizmosSelected()
        {
            if (!displayGizmos) return;
            Gizmos.matrix = Matrix4x4.TRS(transform.position, Quaternion.identity, Vector3.one);
            Gizmos.DrawWireSphere(Vector3.zero, detectionRange);
        }
    }
}