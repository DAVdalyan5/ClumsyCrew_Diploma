using Assets.Scripts.Core.Character;
using Assets.Scripts.UI;
using cowsins;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;

namespace HeistNSeek.Core.Enemy
{
    /// <summary>
    /// Network-synced enemy health. Implements Cowsins IDamageable so player weapons can damage the enemy.
    /// Ragdoll triggers only when health reaches the death threshold (no balance-loss ragdoll).
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedEnemyHealth : NetworkBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [SerializeField] private bool logDamageInConsole;
        [Header("Death")]
        [Tooltip("Enemy is considered dead when health is less than or equal to this threshold.")]
        [SerializeField] private float deathHealthThreshold = 0f;

        [Header("Ragdoll")]
        [Tooltip("Root GameObject of the ragdoll hierarchy (for ToggleRagdoll).")]
        [SerializeField] private GameObject ragdollHierarchyPart;
        [Tooltip("Transform for death position (typically hips/pelvis bone).")]
        [SerializeField] private Transform ragdollPositionRoot;

        private NetworkVariable<float> _health = new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        private NavMeshAgent _agent;
        private EnemyControllerBase _enemyController;
        private EnemyWeaponController _weaponController;
        private EnemyWeaponShooter _weaponShooter;
        private DamageParticleEffect _damageParticleEffect;
        private bool _isDead;
        private bool _hasAppliedDeathStateOnClient;

        public float Health => _health.Value;
        public float MaxHealth => maxHealth;
        public bool IsDead => _isDead;

        private void Awake()
        {
            EnsureRagdollDisabled();
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            _agent = GetComponent<NavMeshAgent>();
            _enemyController = GetComponent<EnemyControllerBase>();
            _weaponController = GetComponent<EnemyWeaponController>();
            _weaponShooter = GetComponent<EnemyWeaponShooter>();
            _damageParticleEffect = GetComponentInChildren<DamageParticleEffect>(true);
            _hasAppliedDeathStateOnClient = false;
            _isDead = false;

            if (IsServer)
                _health.Value = maxHealth;

            EnsureRagdollDisabled();
        }

        private void EnsureRagdollDisabled()
        {
            if (ragdollHierarchyPart == null) return;
            RagdollUtilities.ToggleRagdoll(ragdollHierarchyPart, false);
        }

        public void Damage(float damage, bool isHeadshot, Vector3 pos)
        {
            if (!IsSpawned || _isDead) return;
            RequestTakeDamageServerRpc(damage, isHeadshot, pos);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestTakeDamageServerRpc(float damage, bool isHeadshot, Vector3 attackerPos)
        {
            if (!IsServer || _isDead) return;

            float applied = Mathf.Max(0, damage);
            float previous = _health.Value;
            _health.Value = Mathf.Max(0, previous - applied);

            if (logDamageInConsole && applied > 0)
                Debug.Log($"[NetworkedEnemyHealth] Enemy took {applied} damage{(isHeadshot ? " HEADSHOT" : "")}. Health: {previous:F0} -> {_health.Value:F0}");

            if (applied > 0)
                NotifyDamageTakenClientRpc(attackerPos);

            if (_health.Value <= GetDeathThreshold())
                TriggerDeathServer();
        }

        private float GetDeathThreshold()
        {
            return Mathf.Max(0f, deathHealthThreshold);
        }

        [ClientRpc]
        private void NotifyDamageTakenClientRpc(Vector3 attackerPos)
        {
            _damageParticleEffect?.TriggerEffect(attackerPos);
        }

        private void TriggerDeathServer()
        {
            if (_isDead)
                return;

            _isDead = true;
            // Dedicated server never runs ClientRpc locally; shut down NavMesh/AI here so no stray state transitions hit SetDestination.
            DisableAI();
            DisableBlockingCollidersOutsideRagdollHierarchy();
            TriggerDeathClientRpc();
        }

        [ClientRpc]
        private void TriggerDeathClientRpc()
        {
            if (_hasAppliedDeathStateOnClient)
                return;

            _hasAppliedDeathStateOnClient = true;
            _isDead = true;
            DisableAI();
            EnableRagdoll();
        }

        private void DisableAI()
        {
            if (_enemyController != null)
            {
                _enemyController.StopEnemy();
                _enemyController.ResetAgentPath();
                _enemyController.enabled = false;
            }

            if (_agent != null && _agent.isActiveAndEnabled)
            {
                _agent.enabled = false;
            }

            if (_weaponShooter != null)
            {
                _weaponShooter.enabled = false;
            }

            if (_weaponController != null)
            {
                _weaponController.enabled = false;
            }
        }

        private void EnableRagdoll()
        {
            if (ragdollHierarchyPart == null)
            {
                Debug.LogWarning("[NetworkedEnemyHealth] ragdollHierarchyPart not assigned.");
                return;
            }

            RagdollUtilities.ToggleRagdoll(ragdollHierarchyPart, true);

            if (ragdollPositionRoot != null)
            {
                transform.position = ragdollPositionRoot.position;
            }

            DisableBlockingCollidersOutsideRagdollHierarchy();
        }

        /// <summary>
        /// Turns off hit/detection colliders on the animated proxy (e.g. EnemyBody capsule) so only ragdoll bones
        /// collide after death. Runs on server for headless/dedicated and again on clients after ragdoll enables.
        /// </summary>
        private void DisableBlockingCollidersOutsideRagdollHierarchy()
        {
            Transform ragdollRoot = ragdollHierarchyPart != null ? ragdollHierarchyPart.transform : null;
            var colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                var col = colliders[i];
                if (col == null)
                    continue;

                if (ragdollRoot != null && IsUnderTransform(col.transform, ragdollRoot))
                    continue;

                col.enabled = false;
            }
        }

        private static bool IsUnderTransform(Transform t, Transform ancestor)
        {
            if (ancestor == null || t == null)
                return false;
            return t == ancestor || t.IsChildOf(ancestor);
        }
    }
}
