using cowsins;
using UnityEngine;

public class BasicMovementBehaviour
{
    private MovementContext context;
    private Rigidbody rb;
    private InputManager inputManager;
    private IPlayerMovementStateProvider playerMovement;
    private IPlayerMovementEventsProvider playerEvents;
    private IPlayerControlProvider playerControl;
    private IWeaponReferenceProvider weaponReference;
    private IWeaponBehaviourProvider weaponController;
    private IPlayerMultipliers playerMultipliers;

    private PlayerOrientation orientation => playerMovement?.Orientation;

    private Vector3 moveDirection;
    private PlayerMovementSettings playerSettings;

    private CapsuleCollider playerCapsuleCollider;

    public RaycastHit SlopeHit;
    private const float frictionThreshold = 0.1f;
    private const float slopeGravityMultiplier = 150;
    private bool wasMovingLastFrame;

    public BasicMovementBehaviour(MovementContext context)
    {
        this.context = context;
        this.rb = context.Rigidbody;
        this.inputManager = context.InputManager;

        this.playerMovement = context.Dependencies.PlayerMovementState;
        this.playerEvents = context.Dependencies.PlayerMovementEvents;
        this.playerControl = context.Dependencies.PlayerControl;
        this.weaponReference = context.Dependencies.WeaponReference;
        this.weaponController = context.Dependencies.WeaponBehaviour;
        this.playerMultipliers = context.Dependencies.PlayerMultipliers;

        this.playerSettings = context.Settings;

        this.playerCapsuleCollider = context.Capsule;
    }

    /// <summary>
    /// Handle all the basics related to the movement of the player.
    /// </summary>
    public void Movement()
    {
        if(!playerControl.IsMovementControllable) return;

        // Note: Extra gravity is already applied in PlayerMovement.FixedUpdate()
        // Removed redundant gravity here to prevent double-application causing jitter

        //Find actual velocity relative to where player is looking
        Vector2 relativeVelocity = FindVelRelativeToLook();

        // Counteract sliding and sloppy movement
        FrictionForce(inputManager.X, inputManager.Y, relativeVelocity);

        // Only zero velocity when truly stationary AND no input is being applied
        // This prevents the janky stop-start behavior when running
        bool hasMovementInput = Mathf.Abs(inputManager.X) > 0.01f || Mathf.Abs(inputManager.Y) > 0.01f;
        if (rb.linearVelocity.magnitude < .05f && !hasMovementInput) rb.linearVelocity = Vector3.zero;

        if (!playerControl.IsControllable)
        {
            // Don't zero velocity immediately - let physics handle deceleration naturally
            // Only apply drag when grounded to prevent floating
            return;
        }

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        bool isCrouchSliding = playerMovement.IsCrouching && horizontalVel.magnitude >= playerMovement.CrouchSpeed;

        if (isCrouchSliding && !playerSettings.allowMoveWhileSliding) return;

        float airborneMultiplier = !playerMovement.Grounded ? playerSettings.controlAirborne : 1f;
        float movementMultipliers = playerSettings.acceleration * Time.deltaTime * airborneMultiplier;

        // Reduce movement influence while sliding if sliding is active and movement while sliding isn't allowed
        if (isCrouchSliding && !playerSettings.allowMoveWhileSliding)
            movementMultipliers *= 0f;

        if (context.IsPlayerOnSlope)
        {
            moveDirection = GetSlopeDirection();
            rb.useGravity = false;
            // Only zero velocity if truly idle (no input) and not recently jumped - prevents unwanted sliding
            if (moveDirection.magnitude == 0 && !context.HasJumped && !hasMovementInput)
            {
                // Gradually slow down instead of instant stop for smoother feel
                Vector3 vel = rb.linearVelocity;
                vel.x *= 0.9f;
                vel.z *= 0.9f;
                rb.linearVelocity = vel;
            }
            if (rb.linearVelocity.y != 0 && moveDirection.magnitude != 0) rb.AddForce(Vector3.down * slopeGravityMultiplier);
        }
        else
        {
            // Re-enable gravity when not on slope (fixes bug where gravity stayed disabled)
            if (!rb.useGravity && !playerMovement.IsClimbing && !playerMovement.IsWallRunning)
            {
                rb.useGravity = true;
            }
            moveDirection = (orientation.Forward * inputManager.Y + orientation.Right * inputManager.X).normalized;
        }

        bool isMoving = moveDirection.magnitude > .1f;

        if (isMoving && !wasMovingLastFrame)
        {
            // Transition Idle -> Moving
            playerEvents.Events.OnIdleToMove?.Invoke();
        }
        else if (!isMoving && wasMovingLastFrame)
        {
            // Transition Moving -> Idle
            playerEvents.Events.OnMovingToIdle?.Invoke();
        }

        // Update tracking variable for next frame
        wasMovingLastFrame = isMoving;

        if (moveDirection.magnitude > .1f)
        {
            playerEvents.Events.OnMoving?.Invoke();
            playerSettings.events.OnMoving?.Invoke();
        }
        else
        {
            playerEvents.Events.OnIdle?.Invoke();
            playerSettings.events.OnIdle?.Invoke();
        }

        // If crouch-sliding, respect steering multiplier and don't add full movement force
        if (isCrouchSliding) rb.AddForce(moveDirection * movementMultipliers * playerSettings.slideSteerMultiplier);
        else rb.AddForce(moveDirection * movementMultipliers);

        // Clamp velocity AFTER applying movement force to prevent oscillation
        LimitDiagonalVelocity();
    }


    /// <summary>
    /// Limits diagonal velocity
    /// </summary>
    private void LimitDiagonalVelocity()
    {
        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        float currentWeightedSpeed = playerMovement.CurrentSpeed * playerMultipliers.WeightMultiplier;
        if (horizontalVelocity.magnitude > currentWeightedSpeed)
        {
            horizontalVelocity = horizontalVelocity.normalized * currentWeightedSpeed;
            rb.linearVelocity = new Vector3(horizontalVelocity.x, rb.linearVelocity.y, horizontalVelocity.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    private Vector2 FindVelRelativeToLook()
    {
        // Convert velocity to local space relative to the player's look direction
        Vector3 localVel = Quaternion.Euler(0, -orientation.Yaw, 0) * rb.linearVelocity;
        return new Vector2(localVel.x, localVel.z);
    }

    /// <summary>
    /// Get the direction of movement in a slope
    /// </summary>
    /// <returns></returns>
    private Vector3 GetSlopeDirection()
    {
        return Vector3.ProjectOnPlane(orientation.Forward * inputManager.Y + orientation.Right * inputManager.X, SlopeHit.normal).normalized;
    }

    /// <summary>
    /// Add friction force to the player when it's not airborne
    /// Please note that it counters movement, since it goes in the opposite direction to velocity
    /// </summary>
    private void FrictionForce(float x, float y, Vector2 mag)
    {
        // Prevent from adding friction on an airborne body
        if (!playerMovement.Grounded || inputManager.Jumping || context.HasJumped) return;

        Vector3 horizontalVel = new Vector3(rb.linearVelocity.x, 0, rb.linearVelocity.z);
        bool isCrouchSliding = playerMovement.IsCrouching && horizontalVel.magnitude >= playerMovement.CrouchSpeed;

        // If crouch-sliding and sliding friction is disabled, skip friction here. Sliding behaviour handles deceleration.
        if (isCrouchSliding && !playerSettings.applyFrictionForceOnSliding) return;

        float friction = isCrouchSliding ? playerSettings.slideFrictionForceAmount : playerSettings.controlsResponsiveness;

        // Apply friction on X axis (strafe) - only when:
        // 1. There's velocity but no input (stopping), OR
        // 2. Velocity and input are in opposite directions (direction change)
        bool noXInput = Mathf.Abs(x) < 0.1f;
        bool hasXVelocity = Mathf.Abs(mag.x) > frictionThreshold;
        bool xDirectionsOpposed = (mag.x * x) < 0; // negative product means opposite signs

        if (hasXVelocity && (noXInput || xDirectionsOpposed))
        {
            rb.AddForce(playerSettings.acceleration * orientation.Right * Time.deltaTime * -mag.x * friction);
        }

        // Apply friction on Y axis (forward/back) - same logic
        bool noYInput = Mathf.Abs(y) < 0.1f;
        bool hasYVelocity = Mathf.Abs(mag.y) > frictionThreshold;
        bool yDirectionsOpposed = (mag.y * y) < 0;

        if (hasYVelocity && (noYInput || yDirectionsOpposed))
        {
            rb.AddForce(playerSettings.acceleration * orientation.Forward * Time.deltaTime * -mag.y * friction);
        }
    }
}
