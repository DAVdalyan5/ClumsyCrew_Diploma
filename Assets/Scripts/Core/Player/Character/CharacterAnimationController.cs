using UnityEngine;

namespace HeistNSeek.Core.Character
{
    /// <summary>
    /// Controls character animations with smooth transitions between IDLE, WALK, RUN, and JUMP states.
    /// Attach this to your character GameObject with an Animator component.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class CharacterAnimationController : MonoBehaviour
    {
        #region Inspector Fields

        [Header("Animation Settings")]
        [Tooltip("Reference to the Animator component")]
        [SerializeField] private Animator animator;

        //[Header("Movement Thresholds")]
        //[Tooltip("Speed threshold to transition from IDLE to WALK")]
        //[SerializeField] private float walkThreshold = 0.1f;

        //[Tooltip("Speed threshold to transition from WALK to RUN")]
        //[SerializeField] private float runThreshold = 3f;

        [Header("Transition Settings")]
        [Tooltip("Smoothing factor for speed changes (higher = smoother but slower response)")]
        [SerializeField] private float speedDampTime = 0.1f;

        #endregion

        #region Animator Parameter Names (must match Animator Controller)

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int JumpHash = Animator.StringToHash("Jump");
        private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");

        #endregion

        #region Private Fields

        private float currentSpeed;
        private bool isJumping;
        private bool isGrounded = true;

        #endregion

        #region Unity Lifecycle

        private void Awake()
        {
            // Get Animator component if not assigned
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }

            if (animator == null)
            {
                Debug.LogError("[CharacterAnimationController] No Animator component found on " + gameObject.name);
                enabled = false;
            }
        }

        private void OnValidate()
        {
            // Auto-assign Animator in editor
            if (animator == null)
            {
                animator = GetComponent<Animator>();
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Update character movement speed. Call this every frame with the character's current velocity magnitude.
        /// Automatically transitions between IDLE, WALK, and RUN based on speed thresholds.
        /// </summary>
        /// <param name="speed">Current movement speed (magnitude of velocity)</param>
        public void SetMovementSpeed(float speed)
        {
            currentSpeed = speed;
            animator.SetFloat(SpeedHash, speed, speedDampTime, Time.deltaTime);
        }

        /// <summary>
        /// Trigger jump animation. Call this when the character jumps.
        /// Uses a Trigger parameter so animation plays only once per jump.
        /// </summary>
        public void TriggerJump()
        {
            if (isGrounded)
            {
                isJumping = true;
                isGrounded = false;
                animator.SetTrigger(JumpHash);
                animator.SetBool(IsGroundedHash, false);
            }
        }

        /// <summary>
        /// Set grounded state. Call this when the character lands.
        /// </summary>
        /// <param name="grounded">Is the character on the ground?</param>
        public void SetGrounded(bool grounded)
        {
            isGrounded = grounded;
            animator.SetBool(IsGroundedHash, grounded);

            if (grounded && isJumping)
            {
                isJumping = false;
            }
        }

        /// <summary>
        /// Force a specific animation state by name. Use for special cases.
        /// </summary>
        /// <param name="stateName">Name of the animation state (e.g., "IDLE", "WALK", "RUN", "JUMP")</param>
        public void ForceAnimationState(string stateName)
        {
            animator.Play(stateName);
        }

        /// <summary>
        /// Get the current animation state info.
        /// </summary>
        public AnimatorStateInfo GetCurrentStateInfo()
        {
            return animator.GetCurrentAnimatorStateInfo(0);
        }

        #endregion

        #region Debug Helpers

        /// <summary>
        /// Get current speed value for debugging.
        /// </summary>
        public float GetCurrentSpeed() => currentSpeed;

        /// <summary>
        /// Check if character is currently jumping.
        /// </summary>
        public bool IsJumping() => isJumping;

        /// <summary>
        /// Check if character is currently grounded.
        /// </summary>
        public bool IsGrounded() => isGrounded;

        #endregion
    }
}
