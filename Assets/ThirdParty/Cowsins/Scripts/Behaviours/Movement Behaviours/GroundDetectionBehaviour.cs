using UnityEngine;
using System.Collections;

namespace cowsins
{
    public class GroundDetectionBehaviour
    {
        private MovementContext context;
        private Rigidbody rb;
        private InputManager inputManager;
        private IPlayerMovementStateProvider playerMovement;
        private IPlayerMovementEventsProvider playerEvents;

        private PlayerOrientation orientation => playerMovement?.Orientation;

        private CapsuleCollider playerCapsuleCollider;

        private PlayerMovementSettings playerSettings;

        private Vector3 playerScale;

        private Coroutine ungroundCoroutine;
        private MonoBehaviour coroutineRunner;

        // Landing Cooldown
        private float lastLandingTime = -1f;
        private const float landingCooldown = 0.2f;

        // Ground Buffer - reduced from 2 to 1 for more responsive grounding
        private int groundedFrameCount = 0;
        private int notGroundedFrameCount = 0;
        private const int requiredFrames = 1;

        private bool cancellingGrounded;

        // Ground detection optimization
        private int groundCheckFrameSkip = 0;
        // Check ground state every frame for responsive movement (was 2, causing stutters)
        private const int GROUND_CHECK_INTERVAL = 1;
        private bool lastGroundCheckResult = false;
        private RaycastHit cachedGroundHit;

        public GroundDetectionBehaviour(MovementContext context)
        {
            this.context = context;
            this.rb = context.Rigidbody;
            this.inputManager = context.InputManager;
            this.playerMovement = context.Dependencies.PlayerMovementState;
            this.playerEvents = context.Dependencies.PlayerMovementEvents;

            this.playerSettings = context.Settings;
            this.playerCapsuleCollider = context.Capsule;

            this.coroutineRunner = context.Transform.GetComponent<MonoBehaviour>();

            this.playerScale = context.Transform.localScale;

            playerEvents.Events.OnJump.AddListener(NegateCancellingGrounded);
            playerEvents.Events.OnJump.AddListener(InvalidateGroundCheckCache);
        }

        public void Tick()
        {
            if (IsSliding())
                playerMovement.IsSliding = true;

            // Handle Frame skips
            bool foundGroundThisFrame;
            RaycastHit hit;

            if (++groundCheckFrameSkip >= GROUND_CHECK_INTERVAL)
            {
                groundCheckFrameSkip = 0;
                foundGroundThisFrame = PerformGroundCheck(out hit);
                lastGroundCheckResult = foundGroundThisFrame;
                cachedGroundHit = hit;
            }
            else
            {
                // Use cached result for skipped frames
                foundGroundThisFrame = lastGroundCheckResult;
                hit = cachedGroundHit;
            }

            context.IsPlayerOnSlope = IsPlayerOnSlope(hit, foundGroundThisFrame);

            // Buffered grounding system
            bool wasGrounded = playerMovement.Grounded;
            bool newGroundedState = UpdateGroundedState(foundGroundThisFrame, wasGrounded);

            if (newGroundedState != wasGrounded)
            {
                // Just landed
                if (newGroundedState)
                {
                    HandleLanding();
                }
                else // Just left ground
                {
                    HandleLeavingGround();
                }
            }

            playerMovement.Grounded = newGroundedState;
        }

        private bool UpdateGroundedState(bool foundGroundThisFrame, bool wasGrounded)
        {
            if (foundGroundThisFrame)
            {
                groundedFrameCount++;
                notGroundedFrameCount = 0;

                // Cancel any pending unground coroutine
                if (ungroundCoroutine != null)
                {
                    coroutineRunner.StopCoroutine(ungroundCoroutine);
                    ungroundCoroutine = null;
                    cancellingGrounded = false;
                }

                // Become grounded after consistent detection
                if (!wasGrounded && groundedFrameCount >= requiredFrames)
                {
                    return true;
                }
                else if (wasGrounded)
                {
                    return true; // Stay grounded
                }
            }
            else
            {
                notGroundedFrameCount++;
                groundedFrameCount = 0;

                // Only start leaving ground process if we were previously grounded
                if (wasGrounded && !cancellingGrounded)
                {
                    cancellingGrounded = true;
                    if (ungroundCoroutine == null)
                    {
                        ungroundCoroutine = coroutineRunner.StartCoroutine(StopGroundedCoroutine());
                    }
                }
            }

            // No state change
            return wasGrounded;
        }

        private void HandleLanding()
        {
            if (Time.time - lastLandingTime < landingCooldown)
            {
                return;
            }

            lastLandingTime = Time.time;

            cancellingGrounded = false;

            playerEvents.Events.OnLand?.Invoke();
            playerSettings.events.OnLand.Invoke();
            SoundManager.Instance.PlaySound(playerSettings.sounds.landSFX, 0, 0, false);
        }

        private void HandleLeavingGround()
        {
            context.CoyoteTimer = context.CoyoteJumpTime;
            cancellingGrounded = false;
        }

        private void ResetGroundingState()
        {
            playerMovement.Grounded = true;
            cancellingGrounded = false;
            groundedFrameCount = requiredFrames;
            notGroundedFrameCount = 0;

            if (ungroundCoroutine != null)
            {
                coroutineRunner.StopCoroutine(ungroundCoroutine);
                ungroundCoroutine = null;
            }
        }

        private IEnumerator StopGroundedCoroutine()
        {
            // Reduced from 0.1f to 0.05f for more responsive ground detection
            yield return new WaitForSeconds(0.05f);

            // Check if we are grounded
            bool stillHasGround = PerformGroundCheck(out RaycastHit hit);

            if (!stillHasGround)
            {
                playerMovement.Grounded = false;
                context.CoyoteTimer = context.CoyoteJumpTime;
            }

            cancellingGrounded = false;
            ungroundCoroutine = null;
        }

        /// <summary>
        /// Determines whether the player is on a slope.
        /// </summary>
        private bool IsPlayerOnSlope(RaycastHit hit, bool isGrounded)
        {
            if (isGrounded)
            {
                float angle = Vector3.Angle(Vector3.up, hit.normal);
                return angle < playerSettings.maxSlopeAngle && angle != 0;
            }
            return false;
        }


        private bool IsSliding()
        {
            return playerMovement.IsCrouching && rb.linearVelocity.magnitude > playerMovement.WalkSpeed;
        }

        private void NegateCancellingGrounded() => cancellingGrounded = false;

        // Force immediate check on next frame
        private void InvalidateGroundCheckCache()
        {
            groundCheckFrameSkip = GROUND_CHECK_INTERVAL;
        }

        /// <summary>
        /// Checks if the player is grounded through physics raycast.
        /// </summary>
        private bool PerformGroundCheck(out RaycastHit hit)
        {
            // Capsule endpoints
            Vector3 center = playerCapsuleCollider.transform.TransformPoint(playerCapsuleCollider.center);
            float height = playerMovement.IsCrouching ? playerCapsuleCollider.height * 0.5f : playerCapsuleCollider.height;
            float halfHeight = Mathf.Max(0, (playerCapsuleCollider.height * 0.5f) - playerCapsuleCollider.radius);

            Vector3 bottom = playerCapsuleCollider.bounds.min + Vector3.up * playerCapsuleCollider.radius;
            Vector3 top = playerCapsuleCollider.bounds.max - Vector3.up * playerCapsuleCollider.radius;
            float radius = playerCapsuleCollider.radius * 0.95f;

            // Primary ground check with capsule cast
            bool foundGround = Physics.CapsuleCast(top, bottom, radius, Vector3.down, out hit, playerSettings.groundCheckDistance, context.WhatIsGround)
                              && CowsinsUtilities.IsFloor(hit.normal, playerSettings.maxSlopeAngle);

            // If capsule cast fails (e.g., hitting stair edge), try a simple raycast from center-bottom
            // This helps with stairs and uneven terrain where capsule cast might hit vertical surfaces
            if (!foundGround)
            {
                Vector3 rayOrigin = context.Transform.position + Vector3.up * 0.1f;
                if (Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit rayHit, playerSettings.groundCheckDistance + 0.15f, context.WhatIsGround))
                {
                    if (CowsinsUtilities.IsFloor(rayHit.normal, playerSettings.maxSlopeAngle))
                    {
                        hit = rayHit;
                        foundGround = true;
                    }
                }
            }

            // Additional check: sphere cast at feet for better stair detection
            if (!foundGround)
            {
                Vector3 sphereOrigin = playerCapsuleCollider.bounds.center;
                sphereOrigin.y = playerCapsuleCollider.bounds.min.y + radius;
                if (Physics.SphereCast(sphereOrigin, radius * 0.5f, Vector3.down, out RaycastHit sphereHit, playerSettings.groundCheckDistance + 0.1f, context.WhatIsGround))
                {
                    if (CowsinsUtilities.IsFloor(sphereHit.normal, playerSettings.maxSlopeAngle))
                    {
                        hit = sphereHit;
                        foundGround = true;
                    }
                }
            }

            return foundGround;
        }
    }
}