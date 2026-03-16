using cowsins;
using UnityEngine;

namespace HeistNSeek.Core.Enemy
{
    /// <summary>
    /// Enemy-only weapon controller. No user input — receives shoot commands from the state machine.
    /// Reuses Cowsins weapon logic (hitscan, damage) for AI. Uses Weapon_SO directly (matches WeaponController pattern).
    /// </summary>
    public class EnemyWeaponController : MonoBehaviour
    {
        [Header("Weapon")]
        [SerializeField] private Weapon_SO weapon;
        [Tooltip("Aim camera at weapon fire point; rotated to look at player when shooting.")]
        [SerializeField] private Camera aimCamera;
        [Tooltip("Layer mask for hit detection. Must include Player layer.")]
        [SerializeField] private LayerMask hitLayer;

        private bool _canShoot = true;
        private float _fireCooldown;

        public Weapon_SO Weapon => weapon;
        public Camera AimCamera => aimCamera;
        public bool CanShoot => _canShoot && weapon != null && aimCamera != null;

        private void Update()
        {
            if (_fireCooldown > 0)
            {
                _fireCooldown -= Time.deltaTime;
                if (_fireCooldown <= 0)
                    _canShoot = true;
            }
        }

        /// <summary>
        /// Perform a hitscan shot. Call from EnemyWeaponShooter when state machine requests shoot.
        /// </summary>
        public void Shoot()
        {
            if (!CanShoot || weapon == null || aimCamera == null)
                return;

            _canShoot = false;
            _fireCooldown = Mathf.Max(0.1f, weapon.fireRate);

            PerformHitscanShot();
        }

        /// <summary>
        /// Rotate aim camera to look at target. Call each frame when enemy is aiming.
        /// </summary>
        public void AimAt(Transform target)
        {
            if (aimCamera == null || target == null) return;

            Vector3 direction = (target.position - aimCamera.transform.position).normalized;
            if (direction.sqrMagnitude > 0.001f)
            {
                aimCamera.transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void PerformHitscanShot()
        {
            if (weapon == null) return;

            float spread = weapon.applyBulletSpread ? weapon.spreadAmount : 0f;
            Vector3 dir = CowsinsUtilities.GetSpreadDirection(spread, aimCamera);
            Ray ray = new Ray(aimCamera.transform.position, dir);

            if (Physics.Raycast(ray, out RaycastHit hit, weapon.bulletRange, hitLayer))
            {
                ApplyDamage(hit, weapon.damagePerBullet, weapon);
            }
        }

        private void ApplyDamage(RaycastHit hit, float damage, Weapon_SO weaponData)
        {
            var hitTransform = hit.collider.transform;
            float finalDamage = damage * GetDistanceDamageReduction(hitTransform);

            if (hitTransform.CompareTag("Critical"))
            {
                var damageable = CowsinsUtilities.GatherDamageableParent(hitTransform);
                damageable?.Damage(finalDamage * weaponData.criticalDamageMultiplier, true);
            }
            else if (hitTransform.CompareTag("BodyShot"))
            {
                var damageable = CowsinsUtilities.GatherDamageableParent(hitTransform);
                damageable?.Damage(finalDamage, false);
            }
            else
            {
                var damageable = hit.collider.GetComponent<IDamageable>();
                damageable?.Damage(finalDamage, false);
            }
        }

        private float GetDistanceDamageReduction(Transform target)
        {
            if (weapon == null || !weapon.applyDamageReductionBasedOnDistance)
                return 1f;

            float distance = Vector3.Distance(target.position, transform.position);
            if (distance <= weapon.minimumDistanceToApplyDamageReduction)
                return 1f;

            return (weapon.minimumDistanceToApplyDamageReduction / distance) * weapon.damageReductionMultiplier;
        }
    }
}
