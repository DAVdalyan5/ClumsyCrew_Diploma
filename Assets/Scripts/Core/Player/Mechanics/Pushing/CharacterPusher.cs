using Assets.Scripts.Core.Player;
using Assets.Scripts.Events.Actions;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

namespace HeistNSeek.Core
{
    public class CharacterPusher : NetworkBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] public Transform CameraTransform;

        [Header("Push Settings")]
        [SerializeField] private float pushSpeed = 10f;
        [SerializeField] private float pushDistance = 3f;
        [SerializeField] private float returnSpeed = 5f;

        private IMessageHub _messageHub;

        private new Collider collider;
        private Vector3 initialLocalPosition;
        private bool isPushing = false;

        private Transform pusherPositionTransform;
        private PlayerController ownerPlayerController;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Awake()
        {
            collider = GetComponent<Collider>();
            pusherPositionTransform = GetComponentInChildren<InteractionPositionMarker>().transform;
            ownerPlayerController = GetComponentInParent<PlayerController>();

            initialLocalPosition = transform.localPosition;
        }

        private void Start()
        {
            if (!IsOwner) return;

            this._messageHub.SubscribeSafe<PushEvent>(this, _ => PerformPush());
        }

        private void Update()
        {
            // Always track camera rotation to stay synced
            FollowCameraRotation();
        }

        /// <summary>
        /// Syncs the collider's rotation with the camera's rotation to avoid desyncing.
        /// </summary>
        private void FollowCameraRotation()
        {
            if (CameraTransform == null) return;

            // Match the camera's rotation exactly
            transform.rotation = CameraTransform.rotation;
        }

        /// <summary>
        /// Performs a push action: moves the collider forward at set speed,
        /// then returns it to initial position and resumes camera tracking.
        /// </summary>
        private void PerformPush()
        {
            if (!IsOwner) return;

            StartCoroutine(PerformRaycastPush());
        }

        public IEnumerator PerformRaycastPush()
        {
            //add cooldown
            //maybe some bug here hoenslty with position but will fix it later
            //TODO:
            Vector3 pushDirection = CameraTransform != null ? CameraTransform.forward : transform.forward;

            if (CameraTransform != null && Physics.Raycast(pusherPositionTransform.position, pushDirection, out RaycastHit hit, pushDistance))
            {
                var hitPlayerController = hit.collider.GetComponentInParent<PlayerController>();

                // Prevent self-pushing: check if we hit our own player
                if (hitPlayerController != null && hitPlayerController == ownerPlayerController)
                {
                    Debug.DrawRay(pusherPositionTransform.position, pushDirection * pushDistance, Color.yellow, 2000);
                    yield return null;
                    yield break;
                }

                // If we hit another player, trigger their ragdoll via RPC
                if (hitPlayerController != null)
                {
                    Debug.Log($"[CharacterPusher] Hit player: {hitPlayerController.name}, triggering push with force {pushSpeed}");
                    RequestPushPlayerServerRpc(hitPlayerController.NetworkObjectId, pushSpeed);
                    Debug.DrawRay(pusherPositionTransform.position, pushDirection * pushDistance, Color.red, 2000);
                    yield return null;
                    yield break;
                }

                // For non-player pushables (objects), handle locally
                var pushable = hit.collider.GetComponent<Assets.Scripts.Core.Player.Mechanics.Pushing.IPushable>();
                pushable?.OnPushed(pushSpeed);
            }
            Debug.DrawRay(pusherPositionTransform.position, pushDirection * pushDistance, Color.green, 2000);

            yield return null;
        }

        [Rpc(SendTo.Server)]
        private void RequestPushPlayerServerRpc(ulong targetNetworkObjectId, float force)
        {
            // Server finds the target player and tells all clients to enable ragdoll
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetNetworkObject))
            {
                var targetPlayerController = targetNetworkObject.GetComponent<PlayerController>();
                if (targetPlayerController != null)
                {
                    targetPlayerController.TriggerPushRagdoll(force);
                }
            }
        }
    }
}
