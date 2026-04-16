using System.Collections.Generic;
using cowsins;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Replicates weapon fire and reload SFX to other clients. Owner hears Cowsins' local sounds;
    /// observers resolve clips from <see cref="weaponLookup"/> using a payload chosen on the owner.
    /// Subscribes to <see cref="WeaponControllerSettings.Events.OnShoot"/> so hitscan, projectile, melee, and custom styles all emit one RPC per shot.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkedCowsinsRemoteFireSfx : NetworkBehaviour
    {
        private static int _dbgFireObserverClientRxCount;
        private static int _dbgFirePlayAtPosLogCount;
        private static int _dbgFireServerRpcLogCount;

        private const byte ReloadKindNormal = 1;
        private const byte ReloadKindEmptyMag = 2;

        [Header("References")]
        [SerializeField] private Transform audioOrigin;
        [SerializeField] private WeaponController weaponController;

        [Tooltip("All Weapon_SO assets used in multiplayer so observers can resolve fire/reload clips by name.")]
        [SerializeField] private Weapon_SO[] weaponLookup;

        private Dictionary<string, Weapon_SO> _weaponByName;

        private void Awake()
        {
            BuildWeaponLookup();
            if (weaponController == null)
                weaponController = GetComponentInChildren<WeaponController>(true);
            if (audioOrigin == null && weaponController != null)
                audioOrigin = weaponController.transform;
        }

        private void BuildWeaponLookup()
        {
            _weaponByName = new Dictionary<string, Weapon_SO>();
            if (weaponLookup == null)
                return;
            for (var i = 0; i < weaponLookup.Length; i++)
            {
                var w = weaponLookup[i];
                if (w == null)
                    continue;
                _weaponByName[w.name] = w;
            }
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // #region agent log
            AgentDebugSessionLog.Write(
                "H1",
                "NetworkedCowsinsRemoteFireSfx.OnNetworkSpawn",
                "spawn",
                $"\"isOwner\":{IsOwner.ToString().ToLowerInvariant()},\"isServer\":{IsServer.ToString().ToLowerInvariant()},\"wcNull\":{(weaponController == null).ToString().ToLowerInvariant()},\"lookupLen\":{((weaponLookup != null) ? weaponLookup.Length : 0)},\"clientId\":{((NetworkManager.Singleton != null) ? NetworkManager.Singleton.LocalClientId : -1)}");
            // #endregion

            if (!IsOwner || weaponController == null)
                return;

            if (weaponController.settings.userEvents != null)
                weaponController.settings.userEvents.OnShoot.AddListener(OnOwnerUserShoot);
            weaponController.Events.OnStartReload.AddListener(OnOwnerStartReload);
        }

        public override void OnNetworkDespawn()
        {
            if (weaponController != null)
            {
                if (weaponController.settings.userEvents != null)
                    weaponController.settings.userEvents.OnShoot.RemoveListener(OnOwnerUserShoot);
                weaponController.Events.OnStartReload.RemoveListener(OnOwnerStartReload);
            }

            base.OnNetworkDespawn();
        }

        private void OnOwnerUserShoot()
        {
            if (!IsSpawned || !IsOwner)
                return;
            if (!TryBuildFirePayload(out var weaponName, out var clipIndex, out var pitchVariation))
            {
                // #region agent log
                AgentDebugSessionLog.Write(
                    "H1",
                    "NetworkedCowsinsRemoteFireSfx.OnOwnerUserShoot",
                    "TryBuildFirePayload_failed");
                // #endregion
                return;
            }

            // #region agent log
            AgentDebugSessionLog.Write(
                "H1",
                "NetworkedCowsinsRemoteFireSfx.OnOwnerUserShoot",
                "RequestFireSoundServerRpc",
                $"\"weapon\":\"{AgentDebugSessionLog.EscapeForJson(weaponName.ToString())}\",\"clipIndex\":{clipIndex},\"pitch\":{pitchVariation.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            // #endregion

            RequestFireSoundServerRpc(GetAudioWorldPosition(), weaponName, clipIndex, pitchVariation);
        }

        private void OnOwnerStartReload()
        {
            if (!IsSpawned || !IsOwner)
                return;
            if (!TryBuildReloadPayload(out var weaponName, out var reloadKind))
                return;
            RequestReloadSoundServerRpc(GetAudioWorldPosition(), weaponName, reloadKind);
        }

        private bool TryBuildFirePayload(out FixedString128Bytes weaponSoName, out byte clipIndex, out float pitchVariation)
        {
            weaponSoName = default;
            clipIndex = 0;
            pitchVariation = 0f;

            var weapon = weaponController.Weapon;
            var id = weaponController.Id;
            if (weapon == null || id == null)
                return false;

            var sfxs = id.fireSFXs;
            if (sfxs == null || sfxs.Length == 0)
                return false;

            var picked = sfxs[Random.Range(0, sfxs.Length)];
            if (picked == null)
                return false;

            var shooting = weapon.audioSFX != null ? weapon.audioSFX.shooting : null;
            if (shooting != null && shooting.Length > 0)
            {
                var mapped = System.Array.IndexOf(shooting, picked);
                clipIndex = (byte)(mapped >= 0 ? mapped : 0);
            }
            else
                clipIndex = 0;

            weaponSoName = new FixedString128Bytes(weapon.name);
            pitchVariation = weapon.pitchVariationFiringSFX;
            return true;
        }

        private bool TryBuildReloadPayload(out FixedString128Bytes weaponSoName, out byte reloadKind)
        {
            weaponSoName = default;
            reloadKind = ReloadKindNormal;
            var weapon = weaponController.Weapon;
            var id = weaponController.Id;
            if (weapon == null || id == null || weapon.audioSFX == null)
                return false;

            reloadKind = id.bulletsLeftInMagazine == 0 ? ReloadKindEmptyMag : ReloadKindNormal;
            weaponSoName = new FixedString128Bytes(weapon.name);
            return true;
        }

        private Vector3 GetAudioWorldPosition()
        {
            if (audioOrigin != null)
                return audioOrigin.position;
            return transform.position + Vector3.up * 1.5f;
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestFireSoundServerRpc(Vector3 worldPosition, FixedString128Bytes weaponSoName, byte clipIndex, float pitchVariation)
        {
            // #region agent log
            if (_dbgFireServerRpcLogCount < 40)
            {
                _dbgFireServerRpcLogCount++;
                AgentDebugSessionLog.Write(
                    "H1",
                    "NetworkedCowsinsRemoteFireSfx.RequestFireSoundServerRpc",
                    "server_rx",
                    $"\"isServer\":{IsServer.ToString().ToLowerInvariant()},\"weapon\":\"{AgentDebugSessionLog.EscapeForJson(weaponSoName.ToString())}\",\"clipIndex\":{clipIndex}");
            }
            // #endregion

            PlayFireSoundObserversClientRpc(worldPosition, weaponSoName, clipIndex, pitchVariation);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestReloadSoundServerRpc(Vector3 worldPosition, FixedString128Bytes weaponSoName, byte reloadKind)
        {
            PlayReloadSoundObserversClientRpc(worldPosition, weaponSoName, reloadKind);
        }

        [ClientRpc]
        private void PlayFireSoundObserversClientRpc(Vector3 worldPosition, FixedString128Bytes weaponSoName, byte clipIndex, float pitchVariation)
        {
            // #region agent log
            if (_dbgFireObserverClientRxCount < 48)
            {
                _dbgFireObserverClientRxCount++;
                AgentDebugSessionLog.Write(
                    "H2",
                    "NetworkedCowsinsRemoteFireSfx.PlayFireSoundObserversClientRpc",
                    "client_rx",
                    $"\"isOwner\":{IsOwner.ToString().ToLowerInvariant()},\"willPlayRemote\":{(!IsOwner).ToString().ToLowerInvariant()},\"weapon\":\"{AgentDebugSessionLog.EscapeForJson(weaponSoName.ToString())}\",\"clipIndex\":{clipIndex}");
            }
            // #endregion

            if (IsOwner)
                return;
            TryPlayFireSoundForObservers(worldPosition, weaponSoName, clipIndex, pitchVariation);
        }

        [ClientRpc]
        private void PlayReloadSoundObserversClientRpc(Vector3 worldPosition, FixedString128Bytes weaponSoName, byte reloadKind)
        {
            if (IsOwner)
                return;
            TryPlayReloadSoundForObservers(worldPosition, weaponSoName, reloadKind);
        }

        private void TryPlayFireSoundForObservers(Vector3 worldPosition, FixedString128Bytes weaponSoName, byte clipIndex, float pitchVariation)
        {
            if (!TryResolveWeapon(weaponSoName, out var weaponSo))
            {
                // #region agent log
                AgentDebugSessionLog.Write(
                    "H5",
                    "NetworkedCowsinsRemoteFireSfx.TryPlayFireSoundForObservers",
                    "TryResolveWeapon_failed",
                    $"\"key\":\"{AgentDebugSessionLog.EscapeForJson(weaponSoName.ToString())}\",\"lookupCount\":{(_weaponByName != null ? _weaponByName.Count : 0)}");
                // #endregion
                return;
            }
            var shooting = weaponSo.audioSFX != null ? weaponSo.audioSFX.shooting : null;
            if (shooting == null || shooting.Length == 0)
                return;
            var i = Mathf.Clamp(clipIndex, 0, shooting.Length - 1);
            var clip = shooting[i];
            TryPlaySoundAtPosition(clip, worldPosition, 0f, pitchVariation, true);
        }

        private void TryPlayReloadSoundForObservers(Vector3 worldPosition, FixedString128Bytes weaponSoName, byte reloadKind)
        {
            if (!TryResolveWeapon(weaponSoName, out var weaponSo) || weaponSo.audioSFX == null)
                return;

            AudioClip clip = null;
            if (reloadKind == ReloadKindEmptyMag)
                clip = weaponSo.audioSFX.emptyMagReload;
            else if (reloadKind == ReloadKindNormal)
                clip = weaponSo.audioSFX.reload;

            if (clip == null)
                return;
            TryPlaySoundAtPosition(clip, worldPosition, 0.1f, 0f, true);
        }

        private bool TryResolveWeapon(FixedString128Bytes weaponSoName, out Weapon_SO weaponSo)
        {
            weaponSo = null;
            if (_weaponByName == null || _weaponByName.Count == 0)
                BuildWeaponLookup();
            var key = weaponSoName.ToString();
            return _weaponByName != null && _weaponByName.TryGetValue(key, out weaponSo) && weaponSo != null;
        }

        private static void TryPlaySoundAtPosition(AudioClip clip, Vector3 worldPosition, float delay, float pitchAdded, bool randomPitch)
        {
            if (clip == null || SoundManager.Instance == null || PoolManager.Instance == null)
            {
                // #region agent log
                AgentDebugSessionLog.Write(
                    "H4",
                    "NetworkedCowsinsRemoteFireSfx.TryPlaySoundAtPosition",
                    "skip_play",
                    $"\"clipNull\":{(clip == null).ToString().ToLowerInvariant()},\"smNull\":{(SoundManager.Instance == null).ToString().ToLowerInvariant()},\"poolNull\":{(PoolManager.Instance == null).ToString().ToLowerInvariant()}");
                // #endregion
                return;
            }

            // #region agent log
            if (_dbgFirePlayAtPosLogCount < 40)
            {
                _dbgFirePlayAtPosLogCount++;
                AgentDebugSessionLog.Write(
                    "H4",
                    "NetworkedCowsinsRemoteFireSfx.TryPlaySoundAtPosition",
                    "play",
                    $"\"clip\":\"{AgentDebugSessionLog.EscapeForJson(clip != null ? clip.name : string.Empty)}\",\"delay\":{delay.ToString(System.Globalization.CultureInfo.InvariantCulture)}");
            }
            // #endregion

            SoundManager.Instance.PlaySoundAtPosition(clip, worldPosition, delay, pitchAdded, randomPitch);
        }
    }
}
