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
        private static IObjectResolver sharedContainer;

        public void SetContainer(IObjectResolver container)
        {
            sharedContainer = container;
        }

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            if (sharedContainer == null)
            {
                sharedContainer = FindAnyObjectByType<LifetimeScope>()?.Container;
            }

            if (sharedContainer != null)
            {
                sharedContainer.InjectGameObject(gameObject);
            }

            SetupCameras();
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
    }
}
