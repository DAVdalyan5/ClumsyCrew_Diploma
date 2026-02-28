using HeistNSeek.Core;
using HeistNSeek.Events;
using cowsins;
using Easy.MessageHub;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace HeistNSeek.Core.NetworkedCowsins
{
    /// <summary>
    /// Bridges "ResetBalance" input action to HeistNSeek ResetBalanceEvent for ragdoll get-up.
    /// Place on the same GameObject as Cowsins InputManager.
    /// Only active for the owning client.
    /// If Cowsins input asset lacks "ResetBalance", add it manually (GameControls map, Button type).
    /// </summary>
    [RequireComponent(typeof(InputManager))]
    public class ResetBalanceInputBridge : MonoBehaviour
    {
        private const string ResetBalanceActionName = "ResetBalance";

        private IMessageHub _messageHub;
        private InputAction _resetBalanceAction;
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

            TrySubscribeToResetBalanceAction();
        }

        private bool IsOwner()
        {
            var net = GetComponentInParent<Unity.Netcode.NetworkBehaviour>();
            return net != null && net.IsOwner;
        }

        private void TrySubscribeToResetBalanceAction()
        {
            if (InputManager.inputActions == null)
            {
                InputManager.inputActions = new PlayerActions();
                if (InputManager.inputActions == null) return;
            }

            var asset = InputManager.inputActions.asset;
            if (asset == null) return;

            _resetBalanceAction = asset.FindAction(ResetBalanceActionName, throwIfNotFound: false);
            if (_resetBalanceAction == null)
            {
                Debug.LogWarning($"[ResetBalanceInputBridge] Action '{ResetBalanceActionName}' not found in input asset. Add it to GameControls map for ragdoll get-up.");
                return;
            }
            if (_messageHub == null) return;

            _performedHandler = _ => _messageHub.Publish(new ResetBalanceEvent());
            _resetBalanceAction.performed += _performedHandler;
        }

        private void TryResolveMessageHubFromScope()
        {
            LifetimeScope scope = FindAnyObjectByType<GameplayLifetimeScope>();
            if (scope == null)
                scope = FindAnyObjectByType<LifetimeScope>();

            if (scope?.Container != null)
                _messageHub = scope.Container.Resolve<IMessageHub>();
        }

        private void OnDestroy()
        {
            if (_resetBalanceAction != null && _performedHandler != null)
            {
                _resetBalanceAction.performed -= _performedHandler;
                _performedHandler = null;
            }
        }
    }
}
