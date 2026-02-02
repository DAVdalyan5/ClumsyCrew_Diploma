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

---

## PvP: Shooting and Damaging Other Players

To make shooting work over the network and damage other players (PvP shooter):

### 1. Networked damage (already in place)

- **NetworkedHealth** on the **root** of the player prefab implements Cowsins `IDamageable`.
- When the shooter’s client hits another player, Cowsins calls `Damage()` on that player’s **NetworkedHealth**; it sends a **ServerRpc** and the server applies damage to the target’s health.
- Ensure the prefab has **NetworkedHealth**: run **Tools → Networked Cowsins → Setup Selected Cowsins Player Prefab** (it adds `NetworkedHealth` to the root).

### 2. Hit layer: allow hitting other players

Weapon raycasts use **WeaponController → Settings → Hit Layer**. If the **Player** layer is not in that mask, shots will not hit other players.

**Do this on your networked player prefab:**

1. Open the prefab (e.g. `NetworkedMovementCowsinsFPSController`).
2. Select the **Player** child (the one with `WeaponController`).
3. In the Inspector, find **Weapon Controller** → **Settings** → **Hit Layer**.
4. Enable **Player** (and any other layers you want to hit: environment, Enemy, etc.).

Without this, the raycast will ignore other players and PvP damage will not work.

### 3. Player colliders and layer

- Other players must have **colliders** (e.g. capsule on the Player child) so the raycast can hit them.
- Put those colliders (or their GameObjects) on the **Player** layer so they are included when **Hit Layer** contains Player.
- Cowsins finds damage via **GatherDamageableParent**: it walks up from the hit collider to the root. **NetworkedHealth** (IDamageable) must be on the **root**, which the setup already does.

**Where to check before play**

- Open your **networked player prefab** (e.g. `NetworkedMovementCowsinsFPSController`).
- In the hierarchy, select the **Player** child (the one that has **PlayerMovement**, **Rigidbody**, **WeaponController**).
- On that **Player** GameObject:
  - There must be a **CapsuleCollider** (Cowsins movement expects it; this is also what other players’ shots hit).
  - In the Inspector top bar, set **Layer** to **Player** so weapon raycasts can hit it.
- Root has **NetworkedHealth**; the **Player** child is under the root, so a hit on the capsule resolves to the root’s IDamageable correctly.

### 4. Optional: headshot (Critical / BodyShot)

Cowsins uses tags for damage type:

- **Critical** → headshot (uses weapon’s critical damage multiplier).
- **BodyShot** → normal body damage.

To use this, add a small collider (e.g. on the head bone) and tag it **Critical**; tag body colliders **BodyShot**. If you don’t tag them, hits use the `else` path and still call `Damage(finalDamage, false)` via `GetComponent<IDamageable>` / **GatherDamageableParent**, so PvP damage works either way.

### 5. Optional: other clients see/hear your shots

Shooting FX (muzzle flash, sound) are local. To show them to other clients:

- Subscribe to **WeaponController.Events.OnShoot** (or **OnShootSpawnEffects**) from a **NetworkBehaviour** on the same root.
- Send a **ClientRpc** with fire position/direction so non-owner clients spawn FX or play sounds (keep it lightweight).

### PvP checklist

| Step | Action |
|------|--------|
| 1 | Run **Tools → Networked Cowsins → Setup Selected Cowsins Player Prefab** so the root has **NetworkedHealth**. |
| 2 | On the prefab: **Player** child → **Weapon Controller** → **Settings** → **Hit Layer** → include **Player**. |
| 3 | Ensure other players have colliders on the **Player** layer (or a layer included in Hit Layer). |
| 4 | (Optional) Add **Critical** / **BodyShot** tags and colliders for headshot vs body. |
| 5 | (Optional) Add a **ClientRpc** on shoot for muzzle/sound FX for other clients. |

After step 1–3, shooting another player on your client will call their **NetworkedHealth.Damage()**, which applies damage on the server and replicates health to everyone.

### How to verify you're damaging the other player

1. **Console (server/host)**  
   On the player prefab root, select **NetworkedHealth** and enable **Log Damage In Console**. When someone takes damage, the **server/host** console will show lines like:  
   `[NetworkedHealth] Player (owner 1) took 25 damage. Health: 100 -> 75`  
   So you can confirm hits and health changes on the host.

2. **Health UI**  
   **NetworkedHealth.Health** is a replicated value. Add a UI (e.g. text or bar) that reads `GetComponent<NetworkedHealth>().Health` (or subscribe to the `_health` NetworkVariable’s `OnValueChanged`) so the **damaged player** sees their health go down when hit.

3. **Quick test**  
   Run as Host + one Client. As host, shoot the client (or as client, shoot the host). With **Log Damage In Console** on, the **host’s** console should show damage lines when you hit the other player. If you never see those lines, check: Hit Layer includes Player, other player’s collider is on Player layer, root has NetworkedHealth.
