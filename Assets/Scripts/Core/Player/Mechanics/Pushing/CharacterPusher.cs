using Assets.Scripts.Core.Player;
using Assets.Scripts.Events.Actions;
using Assets.Scripts.Infrastructure.EasyMessageHub;
using Easy.MessageHub;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using HeistNSeek.Core.NetworkedCowsins;

namespace HeistNSeek.Core
{
    public class CharacterPusher : NetworkBehaviour
    {
        [Header("Camera Reference")]
        [SerializeField] public Transform CameraTransform;

        [Header("Push Settings")]
        [SerializeField] private float pushSpeed = 10f;
        [SerializeField] private float pushDistance = 3f;

        private IMessageHub _messageHub;

        private new Collider collider;
        private Vector3 initialLocalPosition;

        private Transform pusherPositionTransform;
        private PlayerController ownerPlayerController;
        private NetworkObject ownNetworkObject;
        private bool _disableTransformRotationSync;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Awake()
        {
            collider = GetComponent<Collider>();
            var marker = GetComponentInChildren<InteractionPositionMarker>();
            pusherPositionTransform = marker != null ? marker.transform : transform;
            ownerPlayerController = GetComponentInParent<PlayerController>();
            ownNetworkObject = GetComponentInParent<NetworkObject>();
            _disableTransformRotationSync = GetComponent<NetworkedCowsinsPlayerController>() != null;

            initialLocalPosition = transform.localPosition;
        }

        private void Start()
        {
            if (!IsOwner) return;

            if (_messageHub == null)
                TryResolveMessageHubFromScope();

            if (_messageHub == null)
                return;

            _messageHub.SubscribeSafe<PushEvent>(this, _ => PerformPush());
        }

        private void TryResolveMessageHubFromScope()
        {
            LifetimeScope scope = FindAnyObjectByType<GameplayLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();

            if (scope != null && scope.Container != null)
                _messageHub = scope.Container.Resolve<IMessageHub>();
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
            if (_disableTransformRotationSync) return;

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
            Vector3 pushDirection = CameraTransform != null ? CameraTransform.forward : transform.forward;

            Vector3 origin = pusherPositionTransform != null ? pusherPositionTransform.position : transform.position;
            if (CameraTransform != null && Physics.Raycast(origin, pushDirection, out RaycastHit hit, pushDistance))
            {
                if (IsHitSelf(hit))
                {
                    Debug.DrawRay(origin, pushDirection * pushDistance, Color.yellow, 2000);
                    yield return null;
                    yield break;
                }

                var hitPlayerController = hit.collider.GetComponentInParent<PlayerController>();
                if (hitPlayerController != null)
                {
                    Debug.Log($"[CharacterPusher] Hit player: {hitPlayerController.name}, triggering push with force {pushSpeed}");
                    RequestPushPlayerServerRpc(hitPlayerController.NetworkObjectId, pushSpeed);
                    Debug.DrawRay(origin, pushDirection * pushDistance, Color.red, 2000);
                    yield return null;
                    yield break;
                }

                var hitCowsinsController = hit.collider.GetComponentInParent<NetworkedCowsinsPlayerController>();
                if (hitCowsinsController != null)
                {
                    var hitNetworkObject = hitCowsinsController.NetworkObject;
                    if (hitNetworkObject != null && hitNetworkObject.NetworkObjectId != ownNetworkObject?.NetworkObjectId)
                    {
                        RequestPushCowsinsPlayerServerRpc(hitNetworkObject.NetworkObjectId, pushSpeed, origin);
                        Debug.DrawRay(origin, pushDirection * pushDistance, Color.red, 2000);
                        yield return null;
                        yield break;
                    }
                }

                var pushable = hit.collider.GetComponent<Assets.Scripts.Core.Player.Mechanics.Pushing.IPushable>();
                pushable?.OnPushed(pushSpeed);
            }
            Debug.DrawRay(origin, pushDirection * pushDistance, Color.green, 2000);

            yield return null;
        }

        private bool IsHitSelf(RaycastHit hit)
        {
            if (ownNetworkObject == null) return false;
            var hitRoot = hit.collider.transform.root;
            var ourRoot = ownNetworkObject.transform.root;
            return hitRoot == ourRoot || hit.collider.transform.IsChildOf(ownNetworkObject.transform);
        }

        [Rpc(SendTo.Server)]
        private void RequestPushPlayerServerRpc(ulong targetNetworkObjectId, float force)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetNetworkObject))
            {
                var targetPlayerController = targetNetworkObject.GetComponent<PlayerController>();
                if (targetPlayerController != null)
                {
                    targetPlayerController.TriggerPushRagdoll(force);
                }
            }
        }

        [Rpc(SendTo.Server)]
        private void RequestPushCowsinsPlayerServerRpc(ulong targetNetworkObjectId, float forceMagnitude, Vector3 pushOrigin)
        {
            if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(targetNetworkObjectId, out var targetNetworkObject))
                return;

            var targetRagdoll = targetNetworkObject.GetComponent<NetworkedCowsinsRagdollController>();
            if (targetRagdoll != null)
            {
                Vector3 targetPosition = targetNetworkObject.GetComponent<NetworkedCowsinsPlayerController>()?.GetMovingTransform()?.position ?? targetNetworkObject.transform.position;
                Vector3 direction = (targetPosition - pushOrigin).normalized;
                if (direction.sqrMagnitude < 0.01f)
                    direction = Vector3.forward;
                Vector3 forceVector = direction * forceMagnitude;
                targetRagdoll.TriggerPushRagdoll(forceVector);
                return;
            }

            var targetCowsins = targetNetworkObject.GetComponent<NetworkedCowsinsPlayerController>();
            if (targetCowsins == null) return;

            Vector3 targetPosition2 = targetCowsins.GetMovingTransform().position;
            Vector3 direction2 = (targetPosition2 - pushOrigin).normalized;
            if (direction2.sqrMagnitude < 0.01f)
                direction2 = Vector3.forward;
            Vector3 forceVector2 = direction2 * forceMagnitude;
            targetCowsins.ReceivePushFromServer(forceVector2);
        }
    }
}
