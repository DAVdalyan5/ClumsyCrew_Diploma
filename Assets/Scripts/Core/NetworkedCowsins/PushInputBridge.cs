using Assets.Scripts.Events.Actions;
using cowsins;
using Easy.MessageHub;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Bridges Cowsins "Push" input action to HeistNSeek PushEvent so CharacterPusher can perform pushes.
    /// Place on the same GameObject as Cowsins InputManager (or ensure it can access the input asset).
    /// Only active for the owning client.
    /// </summary>
    [RequireComponent(typeof(InputManager))]
    public class PushInputBridge : MonoBehaviour
    {
        private const string PushActionName = "Push";

        private IMessageHub _messageHub;
        private InputAction _pushAction;
        private System.Action<InputAction.CallbackContext> _performedHandler;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        private void Start()
        {
            if (!IsOwner())
                return;

            if (_messageHub == null)
                TryResolveMessageHubFromScope();

            TrySubscribeToPushAction();
        }

        private bool IsOwner()
        {
            var net = GetComponentInParent<NetworkBehaviour>();
            return net != null && net.IsOwner;
        }

        private void TrySubscribeToPushAction()
        {
            if (InputManager.inputActions == null)
            {
                InputManager.inputActions = new PlayerActions();
                if (InputManager.inputActions == null) return;
            }

            var asset = InputManager.inputActions.asset;
            if (asset == null) return;

            _pushAction = asset.FindAction(PushActionName, throwIfNotFound: false);
            if (_pushAction == null) return;
            if (_messageHub == null) return;

            _performedHandler = _ => _messageHub.Publish(new PushEvent());
            _pushAction.performed += _performedHandler;
        }

        private void TryResolveMessageHubFromScope()
        {
            LifetimeScope scope = FindAnyObjectByType<GameplayLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();

            if (scope != null && scope.Container != null)
                _messageHub = scope.Container.Resolve<IMessageHub>();
        }

        private void OnDestroy()
        {
            if (_pushAction != null && _performedHandler != null)
            {
                _pushAction.performed -= _performedHandler;
                _performedHandler = null;
            }
        }
    }
}
