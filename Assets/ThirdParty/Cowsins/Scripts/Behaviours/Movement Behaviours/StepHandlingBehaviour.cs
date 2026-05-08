using cowsins;
using UnityEngine;

public class StepHandlingBehaviour
{
    private readonly MovementContext context;
    private readonly Rigidbody rb;
    private readonly PlayerMovementSettings playerSettings;
    private readonly IPlayerMovementStateProvider playerMovement;
    private readonly CapsuleCollider capsule;

    private const int solidLayerMask = ~0;
    private static readonly QueryTriggerInteraction noTriggers = QueryTriggerInteraction.Ignore;
    private static readonly float[] checkAngles = { 0f, 30f, -30f };

    // Total downward acceleration this Rigidbody experiences per second:
    // PlayerMovement.extraGravityForce (30.19) + Unity gravity (~9.81) ≈ 40 m/s²
    // Used to compensate the climb velocity so gravity doesn't cancel upward movement.
    private const float totalGravityAccel = 40f;

    // Internal noise floor — below this height, floating-point error makes detection unreliable
    private const float minStepThreshold = 0.005f;

    // Tracks whether we applied climb velocity last fixed frame. When step handling
    // stops detecting a climbable step, we use this to dampen residual upward velocity
    // so the player doesn't pop into the air after cresting a stair.
    private bool wasClimbingLastFrame;

    public StepHandlingBehaviour(MovementContext context)
    {
        this.context = context;
        this.rb = context.Rigidbody;
        this.playerSettings = context.Settings;
        this.playerMovement = context.Dependencies.PlayerMovementState;
        this.capsule = context.Capsule;
    }

    public void Tick()
    {
        if (!playerSettings.enableStepHandling || !playerMovement.Grounded
            || context.HasJumped || context.IsPlayerOnSlope)
        {
            EndClimbIfNeeded();
            return;
        }

        HandleStep();
    }

    private void HandleStep()
    {
        // Require actual input so residual physics contact velocity doesn't trigger climbing
        // while the player is standing still next to a step
        InputManager input = context.InputManager;
        if (Mathf.Abs(input.X) < 0.1f && Mathf.Abs(input.Y) < 0.1f)
        {
            EndClimbIfNeeded();
            return;
        }

        Vector3 horizontalVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
        if (horizontalVelocity.magnitude < 0.1f)
        {
            EndClimbIfNeeded();
            return;
        }

        Vector3 moveDir = horizontalVelocity.normalized;
        float feetY = capsule.bounds.min.y;
        // Cap forward sweep at ~1.5x capsule radius so an over-tuned stepCheckDistance
        // can't trigger climbs while the step is still well in front of the player.
        float maxSweep = capsule.radius * 1.5f;
        float requestedSweep = Mathf.Min(playerSettings.stepCheckDistance, maxSweep);
        float checkDist = capsule.radius + requestedSweep;
        // Also clamp the climbable height to something physical: roughly knee-high relative
        // to the capsule. Prevents an over-tuned maxStepHeight from teleporting up walls.
        float maxStep = Mathf.Min(playerSettings.maxStepHeight, capsule.height * 0.5f);

        float bestStepHeight = 0f;
        foreach (float angle in checkAngles)
        {
            Vector3 dir = Quaternion.Euler(0f, angle, 0f) * moveDir;
            float h = GetStepHeight(dir, feetY, checkDist, maxStep);
            if (h > bestStepHeight)
                bestStepHeight = h;
        }

        float effectiveMin = Mathf.Max(minStepThreshold, playerSettings.minStepHeight);
        if (bestStepHeight < effectiveMin)
        {
            EndClimbIfNeeded();
            return;
        }

        // Velocity needed to cover exactly bestStepHeight in one physics step,
        // plus compensation for gravity that will subtract from vel during integration.
        // When bestStepHeight is large this exceeds stepClimbSpeed and gets capped — smooth rise.
        // When bestStepHeight is small (near step top), neededVelY naturally falls below the cap
        // and we apply the exact amount needed — no overshoot, no bounce.
        float gravityCompPerFrame = totalGravityAccel * Time.fixedDeltaTime;
        float neededVelY = bestStepHeight / Time.fixedDeltaTime + gravityCompPerFrame;
        float climbVelY = Mathf.Min(neededVelY, playerSettings.stepClimbSpeed);

        Vector3 vel = rb.linearVelocity;
        if (vel.y < climbVelY)
        {
            vel.y = climbVelY;
            rb.linearVelocity = vel;
        }

        wasClimbingLastFrame = true;
    }

    // Called whenever step handling decides not to apply a climb impulse this frame.
    // If we were climbing last frame and still have residual upward velocity, dampen it
    // so cresting a step doesn't launch the player into the air.
    private void EndClimbIfNeeded()
    {
        if (!wasClimbingLastFrame) return;

        Vector3 vel = rb.linearVelocity;
        if (vel.y > 0f)
        {
            vel.y = 0f;
            rb.linearVelocity = vel;
        }

        wasClimbingLastFrame = false;
    }

    private float GetStepHeight(Vector3 direction, float feetY, float checkDist, float maxStep)
    {
        // Lower ray: detects the step face at ankle level
        var lowerOrigin = new Vector3(rb.position.x, feetY + 0.05f, rb.position.z);
        if (!Physics.Raycast(lowerOrigin, direction, out RaycastHit lowerHit, checkDist, solidLayerMask, noTriggers))
            return 0f;

        // Upper ray: if something is here the obstacle is a full wall, not a climbable step
        var upperOrigin = new Vector3(rb.position.x, feetY + maxStep + 0.01f, rb.position.z);
        if (Physics.Raycast(upperOrigin, direction, checkDist, solidLayerMask, noTriggers))
            return 0f;

        // Downward ray: find the walkable surface on top of the step
        var overStepOrigin = lowerHit.point + direction * 0.05f + Vector3.up * (maxStep + 0.01f);
        if (!Physics.Raycast(overStepOrigin, Vector3.down, out RaycastHit stepTopHit, maxStep + 0.1f, solidLayerMask, noTriggers))
            return 0f;

        float height = stepTopHit.point.y - feetY;
        float effectiveMin = Mathf.Max(minStepThreshold, playerSettings.minStepHeight);
        return (height > effectiveMin && height <= maxStep) ? height : 0f;
    }
}
