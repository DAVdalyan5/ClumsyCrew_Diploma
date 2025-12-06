using Assets.Scripts.Core.Player;
using Cinemachine;
using HeistNSeek.Core;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Assets.Scripts.Runtime.Core
{
    //all this should run locally
    public class ClientSidePlayerConfigurator : NetworkBehaviour
    {
        private IObjectResolver _container;

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            // Find a LifetimeScope in the scene for dependency injection
            // Prefer GameplayLifetimeScope as it contains scene-specific registrations
            LifetimeScope scope = FindAnyObjectByType<GameplayLifetimeScope>();
            if (scope == null)
            {
                // Fallback to any LifetimeScope (e.g., BootstrapLifetimeScope)
                scope = FindAnyObjectByType<LifetimeScope>();
            }

            if (scope != null)
            {
                _container = scope.Container;
                _container.InjectGameObject(gameObject);
                Debug.Log($"[ClientSidePlayerConfigurator] Injected dependencies using container from {scope.GetType().Name}");
            }
            else
            {
                Debug.LogWarning($"[ClientSidePlayerConfigurator] No LifetimeScope found. Dependency injection skipped.");
            }

            SetupCameras();
            SetupPusherCameraRpc();
        }

        private void SetupCameras()
        {
            if (!IsOwner) return;

            var playerFollowCamera = FindObjectsByType<CinemachineVirtualCamera>(FindObjectsSortMode.None)[0];
            var mainCamera = FindObjectsByType<Camera>(FindObjectsSortMode.None)[0];

            var followTransform = this.GetComponentInChildren<CameraRootMarker>()?.transform;
            var pusher = this.GetComponentInChildren<CharacterPusher>();
            if (playerFollowCamera != null)
            {
                playerFollowCamera.Follow = followTransform;
                pusher.CameraTransform = mainCamera.transform;
            }
        }

        private void SetupPusherCameraRpc()
        {
            if (!IsOwner) return;

            var playerPusher = FindObjectsByType<CharacterPusher>(FindObjectsSortMode.None)[0];
            var mainCamera = FindObjectsByType<Camera>(FindObjectsSortMode.None)[0];

            if (mainCamera != null)
            {
                playerPusher.CameraTransform = mainCamera.transform;
            }
        }

    }
}
