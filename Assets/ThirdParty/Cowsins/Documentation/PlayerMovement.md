# Player Movement Architecture (Cowsins FPS Engine)

This document describes how the player movement of `NetworkedMovementCowsinsFPSController` actually works at runtime so that future changes can be made safely.

> Important: the prefab uses the Cowsins **Rigidbody-based** controller. The `FirstPersonMovementHandler` script in `Assets/Scripts/Core/Player/` is *not* on this prefab — it's an older `CharacterController`-based path that is no longer wired up. Don't edit it expecting movement changes to take effect.

---

## High-level layout

```
NetworkedMovementCowsinsFPSController (Rigidbody + CapsuleCollider)
├─ PlayerStates           (state machine MonoBehaviour, drives Update/FixedUpdate)
├─ PlayerMovement         (coordinator MonoBehaviour, owns all behaviour instances)
├─ PlayerDependencies     (reference container: input, weapon, stats, fall height)
├─ InputManager           (reads new Input System, exposes X/Y/Jumping/Sprinting/...)
├─ PlayerControl          (IsControllable / IsMovementControllable gating)
└─ Behaviours (plain C# objects, not MonoBehaviours):
   ├─ BasicMovementBehaviour
   ├─ GroundDetectionBehaviour
   ├─ StepHandlingBehaviour
   ├─ JumpBehaviour
   ├─ CrouchSlideBehaviour
   ├─ DashBehaviour
   ├─ ClimbLadderBehaviour
   ├─ WallRunBehaviour / WallBounceBehaviour / GrapplingHookBehaviour
   ├─ CameraLookBehaviour
   ├─ VelocityHandlerBehaviour
   ├─ StaminaBehaviour / FootstepsBehaviour / SpeedLinesBehaviour
   └─ MovementContext (shared mutable state — Grounded, IsPlayerOnSlope, HasJumped, CoyoteTimer, …)
```

`PlayerMovement.InitializeBehaviours()` constructs all of these once in `Start`. Each one keeps a reference to the shared `MovementContext` plus the dependencies it needs — they communicate through that context, not directly.

---

## Tick distribution

There are **two `MonoBehaviour`s driving the loop**, each with its own `Update`/`FixedUpdate`:

| Phase | `PlayerMovement` | `PlayerStates` (current state) |
| --- | --- | --- |
| `Update` | `groundDetectionBehaviour.Tick()`, `jumpBehaviour.Tick()` | `cameraLookBehaviour.Tick()`, footsteps, grapple, sub-state checks, `CheckSwitchState()` |
| `FixedUpdate` | extra gravity (`Vector3.down * 30.19f`), `Vector3.ClampMagnitude(rb.linearVelocity, maxSpeedAllowed)`, `staminaBehaviour.Tick()`, `stepHandlingBehaviour.Tick()` | `basicMovementBehaviour.Movement()`, `wallRunBehaviour.FixedTick()` |

> **Execution-order caveat:** the order in which Unity calls `PlayerMovement.FixedUpdate` vs `PlayerStates.FixedUpdate` is not guaranteed. They both touch `rb.linearVelocity` in the same physics frame. If you observe one-frame velocity races, set Script Execution Order so `PlayerMovement` runs before `PlayerStates`.

---

## Movement loop in detail

### 1. Input
`InputManager` exposes `X`, `Y`, `Jumping`, `Sprinting`, `Crouching`, `Dashing`, mouse deltas, and events (`OnJump`, `OnSprintPressed`, …). Behaviours read these directly.

### 2. Ground detection (`GroundDetectionBehaviour.Tick`)
Runs every frame in `Update` (no frame-skip — `GROUND_CHECK_INTERVAL = 1`).

`PerformGroundCheck` does **three sweeps in order**, returning on first success:

1. **Capsule cast** straight down by `groundCheckDistance` from the player capsule. Result must satisfy `CowsinsUtilities.IsFloor(hit.normal, maxSlopeAngle)`.
2. **Single raycast** straight down from `position + 0.1f * up`, length `groundCheckDistance + 0.15f`. Same floor check.
3. **Sphere cast** from feet, radius `capsule.radius * 0.5`, length `groundCheckDistance + 0.1f`. Same floor check.

The successful hit is fed to `IsPlayerOnSlope`, which sets `context.IsPlayerOnSlope = (angle < maxSlopeAngle && angle != 0)`.

Grounding is **buffered**: `requiredFrames = 1` consecutive positive checks to become grounded; leaving ground starts a 0.05s coroutine (`StopGroundedCoroutine`) before flipping `Grounded = false`. This eliminates one-frame holes but introduces a small lag in coyote-time start. `OnLand` and `OnJump` have a 0.2s landing cooldown to avoid double-fire.

### 3. Movement (`BasicMovementBehaviour.Movement`)
Called every `FixedUpdate` from `PlayerDefaultState`/`PlayerJumpState`/`PlayerCrouchState`. Steps:

1. `FindVelRelativeToLook()` — yaw-rotates current velocity into local space.
2. `FrictionForce` — when grounded and (no input on an axis OR input opposes velocity on that axis), apply a counter-force of magnitude `acceleration * Time.deltaTime * -velComponent * controlsResponsiveness`. **Skipped** entirely while jumping or while crouch-sliding (unless `applyFrictionForceOnSliding`).
3. If velocity magnitude < 0.05 *and* no input: zero `linearVelocity` outright.
4. If `context.IsPlayerOnSlope`:
   - `moveDirection = Vector3.ProjectOnPlane(input, slopeNormal).normalized`
   - `rb.useGravity = false`
   - When idle and not jumping: dampen horizontal velocity by 0.9× per frame (prevents drift).
   - When moving on slope: add `Vector3.down * 150` continuous force to glue the player to the slope.
5. Otherwise re-enable gravity (unless climbing/wallrunning) and use world-space input direction.
6. Add movement force `moveDirection * acceleration * Time.deltaTime * (Grounded ? 1 : controlAirborne)`.
7. `LimitDiagonalVelocity()` — clamp horizontal speed to `CurrentSpeed * WeightMultiplier` after applying force.

**Speed selection** is owned by `VelocityHandlerBehaviour`. It listens to `OnIdleToMove`, `OnSprintPressed`, `OnAimStart`, etc., and writes `playerMovement.CurrentSpeed = walkSpeed | runSpeed | crouchSpeed | aiming-speed`.

### 4. Step handling (`StepHandlingBehaviour.Tick`)
Called every `FixedUpdate` from `PlayerMovement`. Skipped if not grounded, while jumping, or while on a slope.

For three forward angles (0°, ±30°) measured against the horizontal velocity direction:
1. **Lower ray** at ankle height (`feetY + 0.05`) cast forward by `capsule.radius + stepCheckDistance`. Must hit something — that's the step face.
2. **Upper ray** at `feetY + maxStepHeight + 0.01` cast forward the same distance. Must miss — otherwise it's a wall.
3. **Top-down ray** from above the lower hit, dropped down up to `maxStepHeight + 0.1`. Tells us the step's top surface height.

Largest valid height across the three angles becomes `bestStepHeight`. Then:
```
neededVelY  = bestStepHeight / fixedDeltaTime + (40 m/s² * fixedDeltaTime)
climbVelY   = min(neededVelY, stepClimbSpeed)
if rb.velocity.y < climbVelY: rb.velocity.y = climbVelY
```
The +40 m/s² compensates for Unity gravity (9.81) + `extraGravityForce` (30.19) that will be applied during the same physics step.

### 5. Jumping (`JumpBehaviour`)
- `Tick` (Update) handles cooldown, coyote-time decrement, `jumpCount` reset on ground.
- `Enter` is called by `PlayerJumpState.EnterState`: zeros `velocity.y`, applies `Vector3.up * jumpForce` impulse, runs directional-jump logic if multi-jumping, plays SFX.
- Coyote time is governed by `playerSettings.canCoyote` + `coyoteJumpTime`. It is currently disabled on the prefab (`canCoyote: 0`).

### 6. Camera (`CameraLookBehaviour.Tick`)
Runs in `Update`. Reads mouse/stick deltas, updates `cameraPitch`/`cameraYaw`/`cameraRoll`, applies them to `playerSettings.playerCam.localRotation`, and calls `orientation.UpdateOrientation(rb.transform.position, cameraYaw)` so movement direction follows where the player is looking.

`PlayerOrientation` is a tiny struct exposing `Forward`/`Right`/`Yaw`. It's the only place yaw lives — both basic movement and step handling pull from it.

### 7. State machine (`PlayerStates` + `PlayerStateFactory`)
States: `Default`, `Jump`, `Crouch`, `Climb`, `Dash`, `Die`. Each implements:
```csharp
EnterState() / UpdateState() / FixedUpdateState() / ExitState() / CheckSwitchState()
```
Transitions happen inside `CheckSwitchState`. Default → Jump on `inputManager.OnJump` if `JumpBehaviour.CanExecute()`, etc. The state currently in `_currentState` is what gets ticked.

---

## Configuration touch points

These are the knobs that actually matter for "feel":

| Setting | Default in base prefab | Variant override | What it controls |
| --- | --- | --- | --- |
| `walkSpeed` / `runSpeed` | 5 / 10 | **8 / 16** | Target horizontal speed for walk vs. sprint |
| `acceleration` | 4500 | – | Force scale per FixedUpdate. With `maxSpeedAllowed` clamp, this controls how snappy you reach top speed. |
| `controlsResponsiveness` | 0.3 | – | Friction multiplier when stopping or reversing direction. Higher = snappier stop. |
| `controlAirborne` | 0.5 | **0.7** | Multiplier on movement force while airborne (0 = no air control). |
| `maxSpeedAllowed` | 20 | **30** | Hard `linearVelocity` clamp in `PlayerMovement.FixedUpdate`. Must be ≥ runSpeed + jump arc. |
| `groundCheckDistance` | 0.4 | – | Cast length for ground detection. |
| `maxSlopeAngle` | 35 | – | Slopes steeper than this aren't ground; flatter than this trigger slope handling. |
| `enableStepHandling` | true | – | Master toggle. |
| `maxStepHeight` | 0.35 (default) | **1** | Tallest auto-climbable step. |
| `stepClimbSpeed` | 4 | – | Cap on the upward velocity used during a climb (m/s). |
| `stepCheckDistance` | 0.15 (default) | **1** | How far ahead step rays look. |
| `minStepHeight` | 0.05 (default) | **0.1** | Detected steps shorter than this are ignored. |
| `jumpForce` | 20 | – | Impulse magnitude on jump. |
| `jumpCooldown` | 0.25 | – | Lockout before next jump can register. |
| `maxJumps` | 1 | **2** | Allows one mid-air jump. |
| `canCoyote` | false | – | Coyote-time toggle. |

The variant capsule is `radius 0.2 / height 1 / center (0, 0, 0)`. The rigidbody is `mass 1.5, Interpolate=Interpolate, CollisionDetection=Continuous Dynamic`.

---

## Why steps/slopes can feel jittery

Three known interaction patterns (this section is intentionally diagnostic — these are the symptoms to look for if jitter regresses):

1. **Step probe range too long.** `stepCheckDistance` is added to `capsule.radius` — with the variant's 1.0 + 0.2 = 1.2m forward sweep, the step climb starts firing while the player is still far from the step, producing a phantom "lift" before contact.
2. **Slope flicker on stair edges.** `IsPlayerOnSlope` is true for *any* normal between (0°, maxSlopeAngle). The capsule on a stair edge gets a normal that oscillates between the tread (≈0°, treated as flat) and the corner (≈40-45°, often rejected as wall, falling through to fallback rays that pick up the next surface). Each toggle flips `useGravity` and the move direction, producing micro-discontinuities.
3. **Ground-check tiers using different surfaces.** Capsule cast → raycast → sphere cast can each return a *different* hit normal. The slope state therefore depends on which tier matched this frame. Changing tiers between frames can flip the slope state without the player actually changing surfaces.

The current code mitigates these with: capsule-cast slope rejection, the 0.05s ungrounding coroutine, the `wasMovingLastFrame` smoothing, and gravity compensation in the step climb. There's still room for tightening — see the inline TODOs in `BasicMovementBehaviour` and `GroundDetectionBehaviour`.

---

## Where to make changes

- **Tuning numbers** → `PlayerMovementSettings` on the prefab in the Inspector.
- **Movement physics rules** → `BasicMovementBehaviour.cs`.
- **Step climbing** → `StepHandlingBehaviour.cs`.
- **Ground/slope detection** → `GroundDetectionBehaviour.cs`.
- **Jump arc / coyote time** → `JumpBehaviour.cs` + matching settings.
- **State transitions** → the state in `Cowsins/Scripts/Player/PlayerState/`.
- **Camera / look** → `CameraLookBehaviour.cs`.

> Network owner gating happens *outside* the Cowsins scripts, in `FirstPersonInputService` and `PlayerController`. Non-owners disable input + collision detection but keep visuals.
