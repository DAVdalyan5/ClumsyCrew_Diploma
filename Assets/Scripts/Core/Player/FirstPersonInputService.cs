using Assets.Scripts.Events;
using Assets.Scripts.Events.Actions;
using Assets.Scripts.Runtime.Helpers;
using Easy.MessageHub;
using HeistNSeek.Events;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace HeistNSeek.Core.Player
{
    public class FirstPersonInputService : MonoBehaviour
    {
        [Header("Input Action Asset")]
        [SerializeField] private InputActionAsset playerControls;

        [Header("Action Map Name References")]
        [SerializeField] private string actionMapName = "Player";

        [Header("Mouse Cursor Settings")]
        public bool cursorLocked = true;
        public bool cursorInputForLook = true;

        private const string moveActionName = "Move";
        private const string lookActionName = "Look";
        private const string sprintActionName = "Sprint";
        private const string jumpActionName = "Jump";
        private const string resetBalanceActionName = "ResetBalance";
        private const string interactActionName = "Interact";
        private const string pushActionName = "Push";

        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction jumpAction;
        private InputAction resetBalanceAction;
        private InputAction interactAction;
        private InputAction pushAction;

        private IMessageHub messageHub;
        private NetworkBehaviour networkBehaviour;

        public Vector2 MoveInput { get; private set; }
        public Vector2 LookInput { get; private set; }
        public bool SprintValue { get; private set; }

        [Inject]
        private void Init(IMessageHub messageHub)
        {
            this.messageHub = messageHub;
        }

        private void Start()
        {
            networkBehaviour = GetComponent<NetworkBehaviour>();

            if (!networkBehaviour.IsOwnerOrStandalone())
            {
                // Non-owner clients don't process input
                Debug.Log($"[FirstPersonInputService] Not owner, disabling input processing.");
                enabled = false;
                return;
            }

            var actionMap = playerControls.FindActionMap(actionMapName);

            moveAction = actionMap.FindAction(moveActionName);
            lookAction = actionMap.FindAction(lookActionName);
            jumpAction = actionMap.FindAction(jumpActionName);
            sprintAction = actionMap.FindAction(sprintActionName);
            resetBalanceAction = actionMap.FindAction(resetBalanceActionName);
            interactAction = actionMap.FindAction(interactActionName);
            pushAction = actionMap.FindAction(pushActionName);

            RegisterInputActions();
            SetCursorState(cursorLocked);
        }

        private void RegisterInputActions()
        {
            moveAction.performed += ctx => MoveInput = ctx.ReadValue<Vector2>();
            moveAction.canceled += ctx => MoveInput = Vector2.zero;

            lookAction.performed += ctx =>
            {
                if (cursorInputForLook)
                {
                    LookInput = ctx.ReadValue<Vector2>();
                }
            };
            lookAction.canceled += ctx => LookInput = Vector2.zero;

            sprintAction.performed += ctx => SprintValue = ctx.ReadValue<float>() > 0.1f;
            sprintAction.canceled += _ => SprintValue = false;

            jumpAction.performed += ctx => messageHub.Publish(new JumpEvent());
            resetBalanceAction.performed += ctx => messageHub.Publish(new ResetBalanceEvent());
            interactAction.performed += ctx => messageHub.Publish(new InteractEvent());
            interactAction.canceled += ctx => messageHub.Publish(new InteractReleasedEvent());
            pushAction.performed += ctx => messageHub.Publish(new PushEvent());
        }

        private void OnEnable()
        {
            if (!networkBehaviour.IsOwnerOrStandalone()) return;

            moveAction?.Enable();
            lookAction?.Enable();
            sprintAction?.Enable();
            jumpAction?.Enable();
            resetBalanceAction?.Enable();
            interactAction?.Enable();
            pushAction?.Enable();
        }

        private void OnDisable()
        {
            if (!networkBehaviour.IsOwnerOrStandalone()) return;

            moveAction?.Disable();
            lookAction?.Disable();
            sprintAction?.Disable();
            jumpAction?.Disable();
            resetBalanceAction?.Disable();
            interactAction?.Disable();
            pushAction?.Disable();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!networkBehaviour.IsOwnerOrStandalone()) return;
            SetCursorState(cursorLocked);
        }

        private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}
