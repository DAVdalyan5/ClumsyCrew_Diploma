using cowsins;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Networked health that implements Cowsins IDamageable so the FPS engine's
    /// weapon hit detection applies damage over the network. Place on the same root as NetworkObject.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkObject))]
    public class NetworkedHealth : NetworkBehaviour, IDamageable
    {
        [Header("Health")]
        [SerializeField] private float maxHealth = 100f;
        [Tooltip("When enabled, server logs damage so you can verify PvP in the console.")]
        [SerializeField] private bool logDamageInConsole;

        private NetworkVariable<float> _health = new NetworkVariable<float>(
            100f,
            NetworkVariableReadPermission.Everyone,
            NetworkVariableWritePermission.Server
        );

        /// <summary>Current health. Only server writes; all clients read.</summary>
        public float Health => _health.Value;

        /// <summary>Max health used at spawn (server).</summary>
        public float MaxHealth => maxHealth;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
                _health.Value = maxHealth;
        }

        /// <summary>Cowsins weapon hit detection calls this on the shooter's client. We forward to server.</summary>
        public void Damage(float damage, bool isHeadshot)
        {
            if (!IsSpawned) return;
            RequestTakeDamageServerRpc(damage, isHeadshot);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestTakeDamageServerRpc(float damage, bool isHeadshot)
        {
            if (!IsServer) return;
            float applied = Mathf.Max(0, damage);
            float previous = _health.Value;
            _health.Value = Mathf.Max(0, previous - applied);
            if (logDamageInConsole && applied > 0)
                Debug.Log($"[NetworkedHealth] Player (owner {OwnerClientId}) took {applied} damage{(isHeadshot ? " HEADSHOT" : "")}. Health: {previous:F0} -> {_health.Value:F0}");
        }
    }
}
