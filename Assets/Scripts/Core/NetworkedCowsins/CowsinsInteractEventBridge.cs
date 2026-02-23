using Assets.Scripts.Events;
using cowsins;
using Easy.MessageHub;
using Unity.Netcode;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Bridges Cowsins InputManager.StartInteraction to InteractEvent for use with NetworkedItemCollector.
    /// Attach to the Cowsins player prefab so pick/peek works when the player presses interact.
    /// </summary>
    [RequireComponent(typeof(NetworkBehaviour))]
    public class CowsinsInteractEventBridge : NetworkBehaviour
    {
        private IMessageHub _messageHub;
        private InputManager _inputManager;
        private NetworkedCowsinsPlayerController _playerController;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Awake()
        {
            _playerController = GetComponent<NetworkedCowsinsPlayerController>();
        }

        /// <summary>
        /// Resolves IMessageHub from a LifetimeScope when injection hasn't run yet (e.g. client-spawned networked prefabs).
        /// </summary>
        private void EnsureMessageHubResolved()
        {
            if (_messageHub != null) return;

            LifetimeScope scope = FindAnyObjectByType<global::BootstrapLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();
            if (scope != null)
            {
                _messageHub = scope.Container.Resolve<IMessageHub>();
            }
        }

        private void Update()
        {
            if (!IsOwner) return;

            EnsureMessageHubResolved();
            if (_messageHub == null) return;

            if (_inputManager == null)
            {
                _inputManager = _playerController?.GetInputManager();
                if (_inputManager == null) return;
            }

            if (_inputManager.StartInteraction)
            {
                _messageHub.Publish(new InteractEvent());
            }
        }
    }
}
