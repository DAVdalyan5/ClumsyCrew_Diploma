using cowsins;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Replicates weapon fire and reload SFX to other clients. Owner already hears Cowsins' local sounds;
    /// observers use <see cref="SoundManager.PlaySoundAtPosition"/> at an approximate weapon origin.
    /// Fire uses <see cref="WeaponControllerEvents.OnShootHitscanProjectile"/> so full-auto emits one RPC per shot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkedCowsinsRemoteFireSfx : NetworkBehaviour
    {
        [SerializeField] private Transform audioOrigin;
        [SerializeField] private WeaponController weaponController;

        private void Awake()
        {
            if (weaponController == null)
                weaponController = GetComponentInChildren<WeaponController>(true);
            if (audioOrigin == null && weaponController != null)
                audioOrigin = weaponController.transform;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner || weaponController == null)
                return;

            weaponController.Events.OnShootHitscanProjectile.AddListener(OnOwnerShootHitscanProjectile);
            weaponController.Events.OnStartReload.AddListener(OnOwnerStartReload);
        }

        public override void OnNetworkDespawn()
        {
            if (weaponController != null)
            {
                weaponController.Events.OnShootHitscanProjectile.RemoveListener(OnOwnerShootHitscanProjectile);
                weaponController.Events.OnStartReload.RemoveListener(OnOwnerStartReload);
            }

            base.OnNetworkDespawn();
        }

        private void OnOwnerShootHitscanProjectile()
        {
            if (!IsSpawned || !IsOwner)
                return;
            RequestFireSoundServerRpc(GetAudioWorldPosition());
        }

        private void OnOwnerStartReload()
        {
            if (!IsSpawned || !IsOwner)
                return;
            RequestReloadSoundServerRpc(GetAudioWorldPosition());
        }

        private Vector3 GetAudioWorldPosition()
        {
            if (audioOrigin != null)
                return audioOrigin.position;
            return transform.position + Vector3.up * 1.5f;
        }

        [ServerRpc]
        private void RequestFireSoundServerRpc(Vector3 worldPosition)
        {
            PlayFireSoundObserversClientRpc(worldPosition);
        }

        [ServerRpc]
        private void RequestReloadSoundServerRpc(Vector3 worldPosition)
        {
            PlayReloadSoundObserversClientRpc(worldPosition);
        }

        [ClientRpc]
        private void PlayFireSoundObserversClientRpc(Vector3 worldPosition)
        {
            if (IsOwner)
                return;
            TryPlayFireSound(worldPosition);
        }

        [ClientRpc]
        private void PlayReloadSoundObserversClientRpc(Vector3 worldPosition)
        {
            if (IsOwner)
                return;
            TryPlayReloadSound(worldPosition);
        }

        private void TryPlayFireSound(Vector3 worldPosition)
        {
            if (SoundManager.Instance == null || weaponController == null)
                return;
            var id = weaponController.Id;
            var weapon = weaponController.Weapon;
            if (id == null || weapon == null)
                return;
            var clip = id.GetFireSFX();
            if (clip == null)
                return;
            SoundManager.Instance.PlaySoundAtPosition(clip, worldPosition, 0f, weapon.pitchVariationFiringSFX, true);
        }

        private void TryPlayReloadSound(Vector3 worldPosition)
        {
            if (SoundManager.Instance == null || weaponController == null)
                return;
            var id = weaponController.Id;
            var weapon = weaponController.Weapon;
            if (id == null || weapon == null || weapon.audioSFX == null)
                return;
            var clip = id.bulletsLeftInMagazine == 0 ? weapon.audioSFX.emptyMagReload : weapon.audioSFX.reload;
            if (clip == null)
                return;
            SoundManager.Instance.PlaySoundAtPosition(clip, worldPosition, 0.1f, 0f, true);
        }
    }
}
