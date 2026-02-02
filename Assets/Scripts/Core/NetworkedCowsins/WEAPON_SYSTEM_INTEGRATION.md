# FPS Engine Weapon System & Networked Cowsins Integration

## Does the Networked Cowsins FPS Controller Support Weapons?

**Yes.** The networked player prefab is the same Cowsins FPS controller with our networking layer. It already includes the full Cowsins weapon system:

- **WeaponController** on the `Player` child (implements `IWeaponReferenceProvider`, `IWeaponBehaviourProvider`, `IWeaponEventsProvider`)
- **weapons** array and **initialWeapons** in WeaponController
- **InputManager** with `Firing` and `ChangeWeapons` actions
- **OnShoot** events, **canRunWhileShooting**, **canShootWhileDashing**
- **PlayerDependencies** wires **WeaponEffects**; **CowsinsNetcodeFixups** / **CowsinsPreEnableGuard** wire **weaponEvents** for InteractManager and CameraEffects

So the owner can equip weapons, shoot (hitscan, projectile, melee), reload, and aim. All Cowsins weapon behaviour runs locally for the owner.

---

## What Works Out of the Box

| Feature | Owner | Other clients |
|--------|--------|----------------|
| Equip / switch weapons | ✓ | — |
| Shoot (hitscan / projectile / melee) | ✓ | — |
| Reload, aim, recoil, FX (muzzle, camera shake, crosshair) | ✓ | — |
| Hit detection (raycast / projectile) | ✓ (local only) | — |
| Damage to **non-networked** targets (e.g. Cowsins enemies with `IDamageable`) | ✓ | — |

---

## What Does Not Work Over the Network (Without Extra Work)

1. **Damage to other players**  
   Cowsins applies damage locally via `IDamageable.Damage()`. For networked players, that call runs only on the shooter’s client. You need a **networked damage path** so the server applies health changes.

2. **Other clients seeing/hearing your shots**  
   Muzzle flash, tracers, and gun sounds are local. To show them to others, you need a **ClientRpc** (or replicated state) when firing.

3. **Other clients seeing your current weapon**  
   Weapon model and index are local. For third-person or spectator you’d sync current weapon index (e.g. `NetworkVariable<int>`).

---

## Integration Approach

### 1. Use Cowsins Weapon System As-Is (Local / Single Player)

- Prefab: **NetworkedMovementCowsinsFPSController** (or your copy set up via **Tools → Networked Cowsins → Setup Selected Cowsins Player Prefab**).
- Assign **weapons** and **initialWeapons** in the prefab’s WeaponController (on the `Player` child).
- No code changes needed for local shooting and damage to non-networked `IDamageable` (e.g. Cowsins enemies).

### 2. Networked Damage to Players (Recommended)

To have shooting apply damage to other networked players:

1. **Add `NetworkedHealth`** on the **root** of the player prefab (same object as `NetworkObject`).
   - Implements Cowsins’ **`IDamageable`**.
   - When `Damage(damage, isHeadshot)` is called (by Cowsins hit detection on the shooter’s client), it sends a **ServerRpc** to the server.
   - Server applies the damage to a **NetworkVariable** (e.g. health). Other clients see health via the variable.

2. **Hit resolution**  
   Cowsins uses `HitDetectionSystem` and `CowsinsUtilities.GatherDamageableParent(hitTransform)`. If the hit collider is on a child of the root (e.g. the `Player` child), `GatherDamageableParent` will find **IDamageable** on the root. So **IDamageable** must be on the **root** (e.g. **NetworkedHealth**).

3. **Prefab setup**  
   Run **Tools → Networked Cowsins → Setup Selected Cowsins Player Prefab** so the root gets **NetworkedHealth** and any references. Alternatively add **NetworkedHealth** manually to the root and configure initial health.

After this, when the owner shoots and the raycast hits another player, Cowsins will call `Damage()` on that player’s **NetworkedHealth**, which forwards to the server and updates health for everyone.

### 3. Optional: Replicate Shooting FX to Other Clients

- Subscribe to **WeaponController.Events.OnShoot** (or **OnShootSpawnEffects**).
- From a **NetworkBehaviour** on the same root, send a **ClientRpc** with origin/direction (or fire point) so non-owner clients spawn muzzle FX / play sounds.
- Keep FX lightweight (pooled, short-lived).

### 4. Optional: Sync Current Weapon for Third-Person / Spectator

- Add a **NetworkVariable&lt;int&gt;** for current weapon index.
- When the owner changes weapon (e.g. via **WeaponController.Events.OnSelectWeapon** / **OnEquipWeapon**), update that variable (preferably from server after validation, or owner-write with server validation).

---

## Summary

| Question | Answer |
|----------|--------|
| Does the Networked Cowsins FPS controller support weapons? | **Yes.** Weapons and shooting are supported locally. |
| Do I need to “integrate” the FPS engine weapon system? | Only for **networked** behaviour: damage to players, FX replication, and optional weapon sync. |
| Minimal change for networked player-vs-player damage? | Add **NetworkedHealth** (implementing **IDamageable**) on the **root** of the player prefab and run the networked Cowsins setup so the prefab is correctly configured. |

For full multiplayer shooting, implement **NetworkedHealth** as above; then add ClientRpc for FX and optional weapon index sync if needed.
