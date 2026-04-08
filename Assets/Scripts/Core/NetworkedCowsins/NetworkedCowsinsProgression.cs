using cowsins;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Server-authoritative coins and total experience. On the owning client, mirrors values into session
    /// <see cref="CoinManager"/> / <see cref="ExperienceManager"/> so Cowsins UIController keeps working without a global shared wallet across players.
    /// </summary>
    public sealed class NetworkedCowsinsProgression : NetworkBehaviour
    {
        private readonly NetworkVariable<int> _coins = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
        private readonly NetworkVariable<float> _totalExperience = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

        public int Coins => _coins.Value;
        public float TotalExperience => _totalExperience.Value;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            _coins.OnValueChanged += OnCoinsChangedHandler;
            _totalExperience.OnValueChanged += OnExperienceChangedHandler;

            if (IsOwner)
            {
                ApplyCoinsToSessionManagers(_coins.Value);
                ApplyExperienceToSessionManagers(_totalExperience.Value);
            }
        }

        public override void OnNetworkDespawn()
        {
            _coins.OnValueChanged -= OnCoinsChangedHandler;
            _totalExperience.OnValueChanged -= OnExperienceChangedHandler;
            base.OnNetworkDespawn();
        }

        private void OnCoinsChangedHandler(int previous, int current)
        {
            if (IsOwner)
                ApplyCoinsToSessionManagers(current);
        }

        private void OnExperienceChangedHandler(float previous, float current)
        {
            if (IsOwner)
                ApplyExperienceToSessionManagers(current);
        }

        private static void ApplyCoinsToSessionManagers(int targetCoins)
        {
            var cm = CoinManager.Instance;
            if (cm == null)
                return;

            int current = cm.coins;
            int delta = targetCoins - current;
            if (delta > 0)
                cm.AddCoins(delta, true);
            else if (delta < 0)
                cm.RemoveCoins(-delta, true);
        }

        private static void ApplyExperienceToSessionManagers(float targetTotal)
        {
            var em = ExperienceManager.Instance;
            if (em == null)
                return;

            em.ResetExperience();
            em.playerLevel = 0;
            em.AddExperience(targetTotal);
            UIEvents.onExperienceCollected?.Invoke(true);
        }

        /// <summary>Server-only: set absolute coin count.</summary>
        public void ServerSetCoins(int value)
        {
            if (!IsServer)
                return;
            _coins.Value = Mathf.Max(0, value);
        }

        /// <summary>Server-only: add coins.</summary>
        public void ServerAddCoins(int amount)
        {
            if (!IsServer || amount == 0)
                return;
            _coins.Value = Mathf.Max(0, _coins.Value + amount);
        }

        /// <summary>Server-only: spend coins if possible; returns true if deducted.</summary>
        public bool ServerTrySpendCoins(int amount)
        {
            if (!IsServer || amount <= 0)
                return false;
            if (_coins.Value < amount)
                return false;
            _coins.Value -= amount;
            return true;
        }

        /// <summary>Server-only: set absolute total experience.</summary>
        public void ServerSetTotalExperience(float total)
        {
            if (!IsServer)
                return;
            _totalExperience.Value = Mathf.Max(0f, total);
        }

        /// <summary>Server-only: add experience.</summary>
        public void ServerAddExperience(float amount)
        {
            if (!IsServer || amount <= 0f)
                return;
            _totalExperience.Value += amount;
        }
    }
}
