using Assets.Scripts.Infrastructure.EasyMessageHub;
using Assets.Scripts.Runtime.Helpers;
using Easy.MessageHub;
using HeistNSeek.Core.Character;
using HeistNSeek.Events;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

namespace HeistNSeek.Core.Player
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class FirstPersonMovementHandler : MonoBehaviour
    {
        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 4.0f;
        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 6.0f;
        [Tooltip("Rotation speed of the character")]
        public float RotationSpeed = 1.0f;
        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;
        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.1f;
        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;
        [Tooltip("How far below the player position to check for ground (positive value = below feet)")]
        public float GroundedOffset = 0.14f;
        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.5f;
        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;
        [Tooltip("Time in seconds the player can still jump after leaving the ground (coyote time)")]
        public float CoyoteTime = 0.15f;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;
        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 90.0f;
        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -90.0f;

        private bool isBalanced;
        public bool IsBalanced
        {
            get => isBalanced;
            set => isBalanced = value;
        }

        private float _cinemachineTargetPitch;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;
        private float _coyoteTimeCounter;
        private float _speed;
        private bool _wasGroundedLastFrame;

        public float CurrentSpeed => _speed;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private CharacterController _controller;
        private FirstPersonInputService _input;
        private GameObject _mainCamera;
        private CharacterAnimationController _animController;
        private IMessageHub _messageHub;
        private NetworkBehaviour _networkBehaviour;

        private const float _threshold = 0.01f;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        [Inject]
        private void Init(IMessageHub messageHub)
        {
            _messageHub = messageHub;
            RegisterEvents();
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            _controller = GetComponent<CharacterController>();
            _input = GetComponent<FirstPersonInputService>();
            _animController = GetComponentInChildren<CharacterAnimationController>();
            _networkBehaviour = GetComponent<NetworkBehaviour>();
#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#endif

            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            _coyoteTimeCounter = 0f;
            _wasGroundedLastFrame = true;
        }

        private void RegisterEvents()
        {
            if (!_networkBehaviour.IsOwnerOrStandalone()) return;
            _messageHub.SubscribeSafe<JumpEvent>(this, OnJumpEvent);
        }

        private void Update()
        {
            if (!_networkBehaviour.IsOwnerOrStandalone()) return;

            HandleGroundCheck();
            ApplyGravity();
            HandleMovement();
        }

        private void LateUpdate()
        {
            if (!_networkBehaviour.IsOwnerOrStandalone()) return;
            HandleCameraRotation();
        }

        private void HandleGroundCheck()
        {
            // Sphere position is below the player's feet (offset is positive, placed downward)
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            bool groundedThisFrame = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            // Track coyote time - allows jumping shortly after leaving ground
            if (groundedThisFrame)
            {
                _coyoteTimeCounter = CoyoteTime;
            }
            else
            {
                _coyoteTimeCounter -= Time.deltaTime;
            }

            // Hysteresis: once grounded, require being airborne for at least one frame before ungrounding
            // This prevents flickering on rough terrain
            if (_wasGroundedLastFrame && !groundedThisFrame)
            {
                // Just left ground - still allow jumping via coyote time
                Grounded = false;
            }
            else
            {
                Grounded = groundedThisFrame;
            }

            _wasGroundedLastFrame = groundedThisFrame;

            if (_animController != null)
            {
                _animController.SetGrounded(Grounded);
            }
        }

        private void HandleCameraRotation()
        {
            if (_input.LookInput.sqrMagnitude >= _threshold)
            {
                //float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                float deltaTimeMultiplier = 1.0f;

                _cinemachineTargetPitch += _input.LookInput.y * RotationSpeed * deltaTimeMultiplier;
                _rotationVelocity = _input.LookInput.x * RotationSpeed * deltaTimeMultiplier;

                _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

                CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

                transform.Rotate(Vector3.up * _rotationVelocity);
            }
        }

        private void HandleMovement()
        {
            if (!IsBalanced) return;

            float targetSpeed = _input.SprintValue ? SprintSpeed : MoveSpeed;

            if (_input.MoveInput == Vector2.zero) targetSpeed = 0.0f;

            // Smooth acceleration/deceleration using MoveTowards for linear, predictable speed changes
            _speed = Mathf.MoveTowards(_speed, targetSpeed, SpeedChangeRate * Time.deltaTime);

            Vector3 inputDirection = Vector3.zero;

            if (_input.MoveInput != Vector2.zero)
            {
                inputDirection = transform.right * _input.MoveInput.x + transform.forward * _input.MoveInput.y;
                inputDirection.Normalize();
            }

            _controller.Move(inputDirection * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            if (_animController != null)
            {
                _animController.SetMovementSpeed(_speed);
            }
        }

        private void ApplyGravity()
        {
            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;

                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;

                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
            }

            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private void OnJumpEvent(JumpEvent evt)
        {
            // Allow jumping if grounded OR within coyote time window AND jump timeout has passed
            bool canJump = (Grounded || _coyoteTimeCounter > 0f) && _jumpTimeoutDelta <= 0.0f;

            if (canJump)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                _coyoteTimeCounter = 0f; // Consume coyote time on jump

                if (_animController != null)
                {
                    _animController.TriggerJump();
                }
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            Gizmos.color = Grounded ? transparentGreen : transparentRed;

            // Ground check sphere is below player position (positive offset = downward)
            Gizmos.DrawSphere(new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z), GroundedRadius);
        }
    }
}
