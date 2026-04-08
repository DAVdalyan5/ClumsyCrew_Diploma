# Networked Cowsins FPS Controller Setup Guide

## Overview
This guide explains how to create a network-aware version of the Cowsins MovementCowsinsFPSController prefab for multiplayer gameplay.

## Prerequisites
- Unity Netcode for GameObjects installed
- Cowsins FPS Engine assets imported
- Basic understanding of Unity's networking system

## Step 1: Create Networked Prefab

### 1.1 Duplicate the Original Prefab
1. In Unity Editor, navigate to `Assets/ThirdParty/Cowsins/Prefabs/PlayerControllers/`
2. Right-click on `MovementCowsinsFPSController.prefab`
3. Select "Copy" then "Paste" in the same folder
4. Rename the copy to `NetworkedMovementCowsinsFPSController.prefab`

### 1.2 Add Network Components
Open the new prefab in Prefab Mode and add the following components:

#### Required Components:
1. **NetworkObject** (Add if not present)
   - This component identifies the GameObject as a networkable object

2. **NetworkTransform** (Add if not present)
   - This component synchronizes position, rotation, and scale across the network
   - Configure settings:
     - Interpolate: `true` (for smooth movement on clients)
     - Sync Position X/Y/Z: `true`
     - Sync Rotation X/Y/Z: `true`
     - Sync Scale X/Y/Z: `false` (usually not needed for players)

3. **NetworkedCowsinsPlayerController** (Add new component)
   - This is our custom NetworkBehaviour that manages ownership
   - Assign references:
     - Player Movement: Drag the `PlayerMovement` component
     - Input Manager: Drag the `InputManager` component

### 1.3 Add NetworkedCowsinsInputManager
1. Keep the existing `InputManager` component (required for functionality)
2. Add `NetworkedCowsinsInputManager` component
3. This component works alongside the original InputManager and handles ownership-based enable/disable

## Step 2: Configure Network Settings

### 2.1 Ownership Settings
In the `NetworkedCowsinsPlayerController` component:
- **Owner Only Input**: `true` (only owner processes input)
- **Owner Only Physics**: `true` (only owner has active physics)

### 2.2 NetworkTransform Settings
Configure `NetworkTransform` for optimal player synchronization:
- **Transform Sync Mode**: `Sync Rigidbody 3D` (if using Rigidbody)
- **Interpolate**: `true`
- **Teleport Threshold**: `3.0` (adjust based on game scale)
- **Max Interpolation Distance**: `30.0`

## Step 3: Update Player Spawner

### 3.1 Locate PlayerSpawner
Find the `PlayerSpawner` script in your scene (usually attached to a NetworkManager or similar GameObject).

### 3.2 Update Prefab Reference
1. Select the GameObject with `PlayerSpawner`
2. In the Inspector, find the `playerPrefab` field
3. Assign the new `NetworkedMovementCowsinsFPSController` prefab

## Step 4: Configure Network Manager

### 4.1 Add to Network Prefab List
1. Select your NetworkManager GameObject
2. In the NetworkManager component, find "Network Prefabs List"
3. Add the `NetworkedMovementCowsinsFPSController` prefab to the list

### 4.2 Configure Spawn Settings
Ensure the player prefab is configured for player spawning:
- The prefab should have `NetworkObject` component
- Player ownership should be assigned automatically on spawn

## Step 5: Test the Setup

### 5.1 Build and Run Test
1. Build the project as a standalone executable
2. Run one instance as host (Host mode)
3. Run another instance as client (Client mode)
4. Verify:
   - Players can connect
   - Each client controls their own player
   - Other players are visible and synchronized
   - Input only works for owned player

### 5.2 Common Issues and Solutions

#### Issue: Players don't move on other clients
**Solution**: Check NetworkTransform settings and ensure interpolation is enabled.

#### Issue: Input works for all clients
**Solution**: Verify `NetworkedCowsinsInputManager` is properly managing the original `InputManager` enable/disable state.

#### Issue: Physics don't sync properly
**Solution**: Ensure `Owner Only Physics` is set correctly and Rigidbody sync is configured.

## Step 6: Advanced Configuration

### 6.1 Custom Network Variables
You can add custom network synchronization by extending `NetworkedCowsinsPlayerController`:

```csharp
// Example: Sync player health
private NetworkVariable<float> _health = new NetworkVariable<float>(
    100f,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

// Example: Sync player state
private NetworkVariable<PlayerState> _playerState = new NetworkVariable<PlayerState>(
    PlayerState.Alive,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);
```

### 6.2 Server RPCs for Game Actions
Add server-authoritative actions:

```csharp
[ServerRpc]
public void TakeDamageServerRpc(float damage, ServerRpcParams rpcParams = default)
{
    if (!IsServer) return;
    
    _health.Value -= damage;
    
    if (_health.Value <= 0)
    {
        DieServerRpc();
    }
}

[ClientRpc]
private void DieClientRpc()
{
    // Handle death on all clients
    Debug.Log("Player died!");
}
```

### 6.3 Client-side Prediction (Optional)
For smoother movement, implement client-side prediction:

1. Store input commands locally
2. Apply movement immediately on owner client
3. Send commands to server for validation
4. Reconcile if server correction is needed

## Performance Considerations

### Network Bandwidth
- Use appropriate sync rates (30-60 Hz for players)
- Consider compressing movement data
- Use delta compression where possible

### Client Performance
- Non-owner clients should have minimal physics processing
- Use LOD (Level of Detail) for distant players
- Optimize network message frequency

## Troubleshooting

### Logging
Enable detailed logging in `NetworkedCowsinsPlayerController`:

```csharp
Debug.Log($"[NetworkedCowsins] Client {OwnerClientId} - IsOwner: {IsOwner}, IsServer: {IsServer}");
```

### Network Visualization
Use Unity's Netcode Profiler to monitor:
- Network traffic
- RPC calls
- Ownership transfers
- Synchronization issues

## Session services (PoolManager, SoundManager, economy UI mirrors)

Cowsins expects global `PoolManager` and `SoundManager` (and `CoinManager` / `ExperienceManager` for HUD). Those no longer live under each player prefab.

1. **Prefab**: `Assets/Prefabs/Networked/CowsinsSessionServices.prefab` — contains pool, sound, coin/XP mirrors (for local HUD only), `AddonManager`, and `CowsinsSingletonLifetimeGuard` (clears static singletons when destroyed).
2. **Gameplay scene**: On `GameplayLifetimeScope` (Main scene), assign **Cowsins Session Services Prefab**. `GameplayEntryPoint` instantiates it once per machine when gameplay starts (before server spawner work), so clients also get a local pool/sound manager.
3. **Player prefab**: `NetworkedMovementCowsinsFPSController` variant removes embedded `GeneralManagers` via `m_RemovedGameObjects` so duplicate singleton races do not occur.
4. **Optional**: Menu **Tools → HeistNSeek → Build Cowsins Session Services Prefab** regenerates the asset from Cowsins `CowsinsFPSController` if needed. **Tools → HeistNSeek → Verify Networked Cowsins Prefabs** checks assignment and stripped player hierarchy.

## Per-player coins and XP (`NetworkedCowsinsProgression`)

Server-authoritative `NetworkVariable` values on the player root mirror into the session `CoinManager` / `ExperienceManager` **on the owning client only** so `UIController` keeps working. Use `ServerAddCoins`, `ServerTrySpendCoins`, `ServerAddExperience`, etc. from server-side gameplay code. Cowsins pickups that call `CoinManager` directly still need to be routed through this behaviour for correct multiplayer (not done automatically).

## Observer weapon SFX (`NetworkedCowsinsRemoteFireSfx`)

Replicates fire/reload sounds to non-owners using `SoundManager.PlaySoundAtPosition` at an approximate weapon origin. Footsteps and other movement SFX are not fully replicated yet; extend `NetworkedCowsinsStateSync` or add animation/RPC hooks if you need them.

## AudioListener and 3D world audio vs voice chat

- **One listener per client**: The Cowsins `Main Camera` under the `Camera` hierarchy includes Unity’s `AudioListener`. `NetworkedCowsinsPlayerController.ownerOnlyObjects` enables that hierarchy only for the **owning** client, so each machine has a single active listener at the local player’s head. Remote player instances keep their camera rig **disabled** on your machine; you still hear their **world** sounds when your client plays spatialized `AudioSource` clips (e.g. RPC-driven SFX such as `NetworkedCowsinsRemoteFireSfx`).
- **Pre-spawn warnings**: Before the local player spawns, there would otherwise be no listener in the scene. **Main** includes a root object `SceneFallbackAudioListener` with `AudioListener` + `LocalPlayerAudioListenerHandoff`, which keeps the fallback enabled until `NetworkManager.LocalClient.PlayerObject` has an active listener, then turns the fallback off in `LateUpdate` to avoid duplicate listeners. Dedicated server-only processes (`IsServer && !IsClient`) keep the fallback disabled.
- **Voice chat** (Vivox, Unity Voice, etc.) is **not** driven by `AudioListener`; it uses the voice SDK’s capture/playback path. Do not expect multiplayer voice to “just work” from scene audio alone.

## Verification checklist (two clients)

- [ ] One `CowsinsSessionServices` instance per machine; no `GeneralManagers` under spawned players.
- [ ] Shooting: remote player fire/reload audible at plausible 3D position.
- [ ] HUD coins/XP: each client only sees their own values after server grants (test with `NetworkedCowsinsProgression` server methods).
- [ ] Peek inventory still unlocks mouse via local `UIController`.
- [ ] Console: no repeated “no audio listeners” spam; with local player spawned, Hierarchy shows exactly one enabled `AudioListener` (player camera), fallback off.

## Conclusion
The networked Cowsins controller provides:
- Ownership-based input processing
- Server-authoritative movement
- Smooth client interpolation
- Easy integration with existing Cowsins systems

Remember to test thoroughly with multiple clients to ensure proper synchronization and performance.