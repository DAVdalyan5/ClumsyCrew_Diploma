using System.Collections;
using cowsins;
using Unity.Netcode;
using UnityEngine;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Replicates footstep SFX to other clients. Non-owners have <see cref="PlayerMovement"/> disabled, so Cowsins never plays steps there.
    /// Owner mutes built-in Cowsins footstep volume and drives both local pooled 3D audio and RPCs so volume matches <see cref="PlayerMovementSettings.footstepVolume"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class NetworkedCowsinsRemoteFootsteps : NetworkBehaviour
    {
        private static bool _loggedVolumeFallbackWarning;

        [Header("References")]
        [SerializeField] private PlayerMovement playerMovement;

        [Tooltip("Same reference as session Cowsins SoundManager.source3D: drag the AudioSource component from AudioSource_3D.prefab (not the root Transform alone). PoolManager needs the prefab root GameObject.")]
        [SerializeField] private AudioSource pooledAudio3DSourcePrefab;

        private float _stepTimer;
        private float _cachedFootstepVolume = -1f;
        private bool _layerIndicesCached;

        private void Awake()
        {
            if (playerMovement == null)
                playerMovement = GetComponentInChildren<PlayerMovement>(true);
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (!IsOwner || playerMovement == null)
                return;

            if (_cachedFootstepVolume < 0f)
                _cachedFootstepVolume = playerMovement.playerSettings.footstepVolume;
            playerMovement.playerSettings.footstepVolume = 0f;

            playerMovement.Events.OnWallRunStart.AddListener(ResetStepTimer);
        }

        public override void OnNetworkDespawn()
        {
            if (playerMovement != null)
                playerMovement.Events.OnWallRunStart.RemoveListener(ResetStepTimer);

            if (IsOwner && playerMovement != null && _cachedFootstepVolume >= 0f)
                playerMovement.playerSettings.footstepVolume = _cachedFootstepVolume;

            base.OnNetworkDespawn();
        }

        private void ResetStepTimer()
        {
            _stepTimer = 0f;
        }

        private void Update()
        {
            if (!IsSpawned || !IsOwner || playerMovement == null)
                return;

            var ctx = playerMovement.movementContext;
            if (ctx == null || ctx.Camera == null)
                return;

            EnsureLayerIndicesCached();
            TickFootsteps(ctx);
        }

        private void EnsureLayerIndicesCached()
        {
            if (_layerIndicesCached || playerMovement == null)
                return;

            var surface = playerMovement.playerSettings.footstepSounds.surfaceSounds;
            if (surface == null)
            {
                _layerIndicesCached = true;
                return;
            }

            for (var i = 0; i < surface.Count; i++)
            {
                var entry = surface[i];
                if (entry.cachedLayerIndex == -1)
                    entry.cachedLayerIndex = LayerMask.NameToLayer(entry.layerName);
            }

            _layerIndicesCached = true;
        }

        private void TickFootsteps(MovementContext movementContext)
        {
            if (!CanExecuteFootstep())
            {
                _stepTimer = 1f - playerMovement.playerSettings.footstepSpeed;
                return;
            }

            _stepTimer -= Time.deltaTime * playerMovement.CurrentSpeed / 15f;

            if (_stepTimer > 0f)
                return;

            _stepTimer = 1f - playerMovement.playerSettings.footstepSpeed;

            var orientation = playerMovement.Orientation;
            var footstepDirection = !playerMovement.IsWallRunning
                ? Vector3.down
                : (movementContext.WallLeft ? -orientation.Right : orientation.Right) * 2f;

            if (!Physics.Raycast(movementContext.Camera.position, footstepDirection, out var hit, 2.5f, movementContext.WhatIsGround))
                return;

            var layer = hit.transform.gameObject.layer;
            var sounds = playerMovement.playerSettings.footstepSounds.GetSoundsForLayer(layer);
            if (sounds == null || sounds.Length == 0)
                return;

            var clipIndex = (byte)Random.Range(0, sounds.Length);
            var clip = sounds[clipIndex];
            if (clip == null)
                return;

            var vol = _cachedFootstepVolume >= 0f ? _cachedFootstepVolume : 1f;
            PlayFootstepForLocalClient(clip, hit.point, vol);

            RequestFootstepServerRpc(hit.point, layer, clipIndex, vol);
        }

        private bool CanExecuteFootstep()
        {
            if ((!playerMovement.Grounded && !playerMovement.IsWallRunning) || playerMovement.IsIdle)
                return false;
            return true;
        }

        private GameObject GetPooledAudioPrefabRoot()
        {
            return pooledAudio3DSourcePrefab != null ? pooledAudio3DSourcePrefab.gameObject : null;
        }

        private void PlayFootstepForLocalClient(AudioClip clip, Vector3 worldPosition, float volume)
        {
            var poolRoot = GetPooledAudioPrefabRoot();
            if (poolRoot != null && PoolManager.Instance != null)
                StartCoroutine(PlayPooledFootstepCoroutine(clip, worldPosition, volume, poolRoot));
            else
                TryPlaySoundManagerFallback(clip, worldPosition, volume);
        }

        private static void TryPlaySoundManagerFallback(AudioClip clip, Vector3 worldPosition, float volume)
        {
            if (clip == null)
                return;

            if (SoundManager.Instance != null && PoolManager.Instance != null)
            {
                SoundManager.Instance.PlaySoundAtPosition(clip, worldPosition, 0f, 0.3f, true);
                if (volume < 0.99f && !_loggedVolumeFallbackWarning)
                {
                    _loggedVolumeFallbackWarning = true;
                    Debug.LogWarning("[NetworkedCowsinsRemoteFootsteps] Assign pooledAudio3DSourcePrefab (AudioSource on AudioSource_3D, same as SoundManager.source3D) for correct footstep volume when volume is below 1.");
                }
                return;
            }

            PlayWorldClipOneShot(clip, worldPosition, volume);
        }

        private static void PlayWorldClipOneShot(AudioClip clip, Vector3 worldPosition, float volume)
        {
            if (clip == null)
                return;
            AudioSource.PlayClipAtPoint(clip, worldPosition, Mathf.Clamp01(volume));
        }

        private IEnumerator PlayPooledFootstepCoroutine(AudioClip clip, Vector3 position, float volume, GameObject prefab)
        {
            if (clip == null || prefab == null || PoolManager.Instance == null)
                yield break;

            var go = PoolManager.Instance.GetFromPool(prefab, position, Quaternion.identity);
            if (go == null)
            {
                PlayWorldClipOneShot(clip, position, volume);
                yield break;
            }

            var src = go.GetComponent<AudioSource>();
            if (src == null)
            {
                PlayWorldClipOneShot(clip, position, volume);
                yield break;
            }

            src.spatialBlend = 1f;
            src.volume = volume;
            src.pitch = 1f + Random.Range(-0.3f, 0.3f);
            src.clip = clip;
            src.Play();

            var wait = clip.length / Mathf.Max(0.01f, src.pitch);
            yield return new WaitForSeconds(wait);
            PoolManager.Instance.ReturnToPool(go, prefab);
        }

        [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
        private void RequestFootstepServerRpc(Vector3 worldPosition, int groundLayer, byte clipIndex, float volume)
        {
            PlayFootstepObserversClientRpc(worldPosition, groundLayer, clipIndex, volume);
        }

        [ClientRpc]
        private void PlayFootstepObserversClientRpc(Vector3 worldPosition, int groundLayer, byte clipIndex, float volume)
        {
            if (IsOwner)
                return;

            if (playerMovement == null)
                playerMovement = GetComponentInChildren<PlayerMovement>(true);
            if (playerMovement == null)
                return;

            var sounds = playerMovement.playerSettings.footstepSounds.GetSoundsForLayer(groundLayer);
            if (sounds == null || sounds.Length == 0)
                return;

            var i = Mathf.Clamp(clipIndex, 0, sounds.Length - 1);
            var clip = sounds[i];
            if (clip == null)
                return;

            var poolRoot = GetPooledAudioPrefabRoot();
            if (poolRoot != null && PoolManager.Instance != null)
                StartCoroutine(PlayPooledFootstepCoroutine(clip, worldPosition, volume, poolRoot));
            else
                TryPlaySoundManagerFallback(clip, worldPosition, volume);
        }
    }
}
