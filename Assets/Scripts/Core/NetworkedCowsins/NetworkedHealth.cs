using Assets.Scripts.Events;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using cowsins;
using Easy.MessageHub;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

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

        private IMessageHub _messageHub;

        /// <summary>Current health. Only server writes; all clients read.</summary>
        public float Health => _health.Value;

        /// <summary>Max health used at spawn (server).</summary>
        public float MaxHealth => maxHealth;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();
            if (IsServer)
                _health.Value = maxHealth;

            EnsureMessageHubResolved();
        }

        private void EnsureMessageHubResolved()
        {
            if (_messageHub != null) return;

            LifetimeScope scope = FindAnyObjectByType<global::BootstrapLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();
            if (scope != null)
                _messageHub = scope.Container.Resolve<IMessageHub>();
        }

        /// <summary>
        /// IDamageable entry point. If <paramref name="p"/> is non-zero the caller
        /// is providing the attacker position directly (e.g. forwarded from PlayerStats
        /// after an NPC hit). If it is zero we fall back to resolving the shooter from
        /// SenderClientId, which is correct for the player-vs-player path where Cowsins
        /// weapon code does not supply a position.
        /// </summary>
        public void Damage(float damage, bool isHeadshot, Vector3 p)
        {
            if (!IsSpawned) return;
            bool useProvided = p != Vector3.zero;
            RequestTakeDamageServerRpc(damage, isHeadshot, p, useProvided);
        }

        /// <summary>
        /// Server-side / NPC damage path. Caller explicitly provides the attacker's
        /// world position so the damage indicator points at the actual enemy, not at
        /// a connected player derived from SenderClientId (which is wrong for NPCs).
        /// </summary>
        public void DamageFromAttacker(float damage, bool isHeadshot, Vector3 attackerWorldPosition)
        {
            if (!IsSpawned) return;
            RequestTakeDamageServerRpc(damage, isHeadshot, attackerWorldPosition, true);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
        private void RequestTakeDamageServerRpc(
            float damage,
            bool isHeadshot,
            Vector3 providedAttackerPos,
            bool useProvidedPos,
            ServerRpcParams serverRpcParams = default)
        {
            if (!IsServer) return;

            float applied = Mathf.Max(0, damage);
            float previous = _health.Value;
            _health.Value = Mathf.Max(0, previous - applied);

            if (logDamageInConsole && applied > 0)
                Debug.Log($"[NetworkedHealth] Player (owner {OwnerClientId}) took {applied} damage{(isHeadshot ? " HEADSHOT" : "")}. Health: {previous:F0} -> {_health.Value:F0}");

            if (applied <= 0) return;

            Vector3 attackerPosition = Vector3.zero;
            if (useProvidedPos)
            {
                // NPC / server-side attacker: position was passed explicitly by the caller.
                attackerPosition = providedAttackerPos;
            }
            else
            {
                // Player-vs-player: resolve shooter position from SenderClientId.
                ulong shooterClientId = serverRpcParams.Receive.SenderClientId;
                if (NetworkManager.Singleton.ConnectedClients.TryGetValue(shooterClientId, out var shooterClient)
                    && shooterClient.PlayerObject != null)
                {
                    attackerPosition = shooterClient.PlayerObject.transform.position;
                }
            }

            // Notify only the victim's client so they can show the damage indicator.
            var targetParams = new ClientRpcParams
            {
                Send = new ClientRpcSendParams { TargetClientIds = new ulong[] { OwnerClientId } }
            };
            NotifyDamageTakenClientRpc(attackerPosition, applied, targetParams);
        }

        [ClientRpc]
        private void NotifyDamageTakenClientRpc(Vector3 attackerPosition, float damage, ClientRpcParams clientRpcParams = default)
        {
            if (!IsOwner) return;
            _messageHub?.Publish(new DamageTakenEvent(attackerPosition, damage));
        }
    }
}
