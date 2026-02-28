# Ragdoll & Collision Detection – Debug Summary

> Copy this file into a new chat to continue debugging the "player doesn't fall" issue.

---

## Goal
Migrate the ragdoll system to the NetworkedCowsins player so the character goes ragdoll on high-speed impact and can reset with input.

---

## Current Problem
**The player doesn't fall** – ragdoll doesn't trigger on impact.

---

## Architecture Overview

### Flow
1. **CollisionDetector** (Head, Torso, LeftLeg, RightLeg) – trigger colliders on body parts
2. **CowsinsCollisionPushHandler** – subscribes to detectors, calls `TriggerRagdollFromImpactServerRpc` on high-speed impact
3. **NetworkedCowsinsRagdollController** – handles ragdoll state, RPCs, balance, reset
4. **CharacterPusher** – applies push; prefers `NetworkedCowsinsRagdollController.TriggerPushRagdoll` when present
5. **ResetBalanceInputBridge** – bridges "ResetBalance" input to `ResetBalanceEvent` for get-up

### Key Files
| File | Purpose |
|------|---------|
| `Assets/Scripts/Core/NetworkedCowsins/NetworkedCowsinsRagdollController.cs` | Ragdoll state, balance, RPCs, `TriggerPushRagdoll`, `TriggerRagdollFromImpactServerRpc`, `OnBalanceLostClientRpc`, `ResetPlayerBalanceClientRpc` |
| `Assets/Scripts/Core/NetworkedCowsins/CowsinsCollisionPushHandler.cs` | Subscribes to CollisionDetectors, triggers ragdoll on high-speed impact |
| `Assets/Scripts/Core/NetworkedCowsins/NetworkedCowsinsSetup.cs` | Editor setup: adds components, parents detectors to bones, removes arm detectors |
| `Assets/Scripts/Core/Player/Mechanics/Ragdoll/RagdollUtilities.cs` | `ToggleRagdoll`, `ApplyForceToRagdoll`, `IsRagdollEnabled` |
| `Assets/Scripts/Core/NetworkedCowsins/ResetBalanceInputBridge.cs` | Bridges "ResetBalance" input to `ResetBalanceEvent` |
| `Assets/Scripts/Core/Player/Mechanics/Ragdoll/CollisionDetector.cs` | Trigger collider + `Subscribe()` for collision callbacks |

### Prefab
- **NetworkedMovementCowsinsFPSController** – `Assets/ThirdParty/Cowsins/Prefabs/PlayerControllers/`
- **Rio** – character model with Animator (RioAvatar); collision detectors parented to its bones
- **RioRagdoll** – ragdoll hierarchy (from `Assets/Prefabs/HalfBaked/RioRagdoll.prefab`)

---

## Collision Detector Setup

- **Detectors:** Head, Torso, LeftLeg, RightLeg (arm detectors removed)
- **Parenting:** Parented to Animator bones when Humanoid Animator with avatar is found
- **Fallback:** If no Animator found, parented to `Player > CollisionDetectors` with fixed positions
- **Animator selection:** Skips Animators with `avatar == null` (e.g. Melee weapon); picks first Humanoid Animator with avatar

---

## Setup Wizard

- **Tools > Networked Cowsins > Setup Selected Cowsins Player Prefab**
- Adds: RioRagdoll, NetworkedCowsinsRagdollController, CowsinsCollisionPushHandler, ResetBalanceInputBridge, body-part detectors
- **Logs:** `[NetworkedCowsinsSetup]` in Console for Animator selection, parent paths, detector summary

---

## Input

- **ResetBalance** action must exist in `Assets/ThirdParty/Cowsins/Inputs/PlayerActions.inputactions` (GameControls map, Button type)
- Without it, `ResetBalanceInputBridge` logs a warning

---

## Debugging Checklist

1. **CollisionDetectors firing?**
   - Are detectors subscribed to `CowsinsCollisionPushHandler`?
   - Add debug logs in `CowsinsCollisionPushHandler` when detector fires

2. **Impact speed threshold?**
   - Check `highSpeedThreshold` in `CollisionDetector` and `CowsinsCollisionPushHandler`
   - Add logs for impact speed vs threshold

3. **Network / ownership?**
   - `TriggerRagdollFromImpactServerRpc` only runs when `senderId == OwnerClientId`
   - Is the hitting client the owner?

4. **Ragdoll hierarchy?**
   - `ragdollHierarchyPart` and `ragdollPositionRoot` assigned on `NetworkedCowsinsRagdollController`?
   - `RagdollUtilities.ToggleRagdoll` and `ApplyForceToRagdoll` called correctly?

5. **Layers / physics?**
   - CollisionDetectors and obstacles on layers that trigger physics?
   - Trigger colliders enabled, `IsTrigger = true`?

6. **RPC flow?**
   - `TriggerRagdollFromImpactServerRpc` → `OnBalanceLostClientRpc` → `EnableRagdoll()` + `ApplyForceToRagdoll`
   - Add logs at each step

---

## User Changes Made

- **Rio Animator:** RioAvatar assigned, Animate Physics enabled
- **Network Animator:** Added on Rio, references the Animator

---

## Quick Grep for Verification

- `TriggerRagdollFromImpactServerRpc` – RPC entry point
- `OnBalanceLostClientRpc` – client-side ragdoll enable
- `CollisionDetector` + `Subscribe` – detector wiring
- `highSpeedThreshold` – impact threshold in `CowsinsCollisionPushHandler` and `CollisionDetector`
