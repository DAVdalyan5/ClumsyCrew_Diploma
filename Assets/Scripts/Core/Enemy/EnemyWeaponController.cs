using System.Globalization;
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

        [Header("Cowsins visuals")]
        [Tooltip("Muzzle flash spawns here; if empty, uses Aim Camera transform.")]
        [SerializeField] private Transform[] firePoints;
        [Tooltip("Optional weapon model Animator; plays Cowsins \"shooting\" state like the player weapon.")]
        [SerializeField] private Animator weaponShootAnimator;

        private bool _canShoot = true;
        private float _fireCooldown;
        private EnemyShootEffectsNet _shootEffectsNet;

        private void Awake()
        {
            ApplyDefaultHitLayerMaskIfUnset();
            _shootEffectsNet = GetComponent<EnemyShootEffectsNet>();
        }

        private void ApplyDefaultHitLayerMaskIfUnset()
        {
            if (hitLayer.value != 0)
                return;

            int mask = 0;
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0)
                mask |= 1 << playerLayer;
            int defaultLayer = LayerMask.NameToLayer("Default");
            if (defaultLayer >= 0)
                mask |= 1 << defaultLayer;

            hitLayer = mask != 0 ? mask : Physics.DefaultRaycastLayers;
        }

        public Weapon_SO Weapon => weapon;
        public Camera AimCamera => aimCamera;
        public bool CanShoot => _canShoot && weapon != null && aimCamera != null;

        /// <summary>Debug-only reason when <see cref="CanShoot"/> is false.</summary>
        public string DebugShootBlockReason()
        {
            if (weapon == null) return "no_weapon";
            if (aimCamera == null) return "no_aimCamera";
            if (!_canShoot) return "fireCooldown";
            return "ready";
        }

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
            {
                #region agent log
                HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("B", "EnemyWeaponController.Shoot",
                    "early_return",
                    $"{{\"reason\":\"{DebugShootBlockReason()}\",\"hasWeapon\":{(weapon != null).ToString().ToLower()},\"hasAimCam\":{(aimCamera != null).ToString().ToLower()}}}");
                #endregion
                return;
            }

            #region agent log
            HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("B", "EnemyWeaponController.Shoot", "fired",
                $"{{\"fireRate\":{weapon.fireRate}}}");
            #endregion

            _canShoot = false;
            _fireCooldown = Mathf.Max(0.1f, weapon.fireRate);

            PerformHitscanShot();
            SpawnCowinsShotVisualsLocal();
            _shootEffectsNet?.NotifyRemoteClientsShotVisuals();
        }

        /// <summary>
        /// Muzzle VFX, fire SFX, and optional shoot animation (same building blocks as Cowsins <see cref="ShootBehaviour"/>).
        /// Safe to call on server (host) and on clients via <see cref="EnemyShootEffectsNet"/>.
        /// </summary>
        public void SpawnCowinsShotVisualsLocal()
        {
            if (weapon == null || aimCamera == null)
                return;

            Quaternion muzzleRot = aimCamera.transform.rotation;

            if (PoolManager.Instance != null && weapon.muzzleVFX != null)
            {
                foreach (Transform fp in GetFirePointTransforms())
                {
                    if (fp == null)
                        continue;
                    GameObject vfx = PoolManager.Instance.GetFromPool(weapon.muzzleVFX, fp.position, muzzleRot);
                    if (vfx != null)
                        vfx.transform.SetParent(aimCamera.transform);
                }
            }

            if (weaponShootAnimator != null)
                CowsinsUtilities.ForcePlayAnim("shooting", weaponShootAnimator);

            TryPlayCowinsFireSound();
        }

        private Transform[] GetFirePointTransforms()
        {
            if (firePoints != null && firePoints.Length > 0)
                return firePoints;
            return new[] { aimCamera.transform };
        }

        private void TryPlayCowinsFireSound()
        {
            if (SoundManager.Instance == null || weapon.audioSFX == null)
                return;

            AudioClip[] clips = weapon.audioSFX.shooting;
            if (clips == null || clips.Length == 0)
                return;

            AudioClip clip = clips[Random.Range(0, clips.Length)];
            if (clip == null)
                return;

            SoundManager.Instance.PlaySound(clip, 0, weapon.pitchVariationFiringSFX, true);
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
            float maxDist = weapon.bulletRange;

            var hits = Physics.RaycastAll(ray, maxDist, hitLayer, QueryTriggerInteraction.Ignore);
            RaycastHit? chosen = null;
            float bestDist = float.MaxValue;
            Transform myRoot = transform.root;

            foreach (var h in hits)
            {
                if (h.collider.transform.root == myRoot)
                    continue;
                if (h.distance < bestDist)
                {
                    bestDist = h.distance;
                    chosen = h;
                }
            }

            #region agent log
            bool hasChosen = chosen.HasValue;
            float dLog = hasChosen ? chosen.Value.distance : -1f;
            HeistNSeek.Core.Enemy.Diagnostics.EnemyRangedDebugNdjson.Log("F", "EnemyWeaponController.PerformHitscanShot",
                "ray_pick",
                $"{{\"totalHits\":{hits.Length},\"hasChosen\":{hasChosen.ToString().ToLower()},\"chosenDist\":{dLog.ToString(CultureInfo.InvariantCulture)},\"mask\":{hitLayer.value}}}",
                "post-fix");
            #endregion

            if (chosen.HasValue)
                ApplyDamage(chosen.Value, weapon.damagePerBullet, weapon);
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
