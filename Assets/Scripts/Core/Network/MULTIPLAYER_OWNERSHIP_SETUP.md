# Multiplayer Ownership Setup

## Overview

The HeistNSeek project is now configured for proper client-server multiplayer where:
- **Owner clients** process full input and logic
- **Non-owner clients** only observe synced state from the network

## Ownership Implementation

### 1. PlayerController (NetworkBehaviour)
**File:** `Assets/Scripts/Core/Player/PlayerController.cs`

- ✅ Extends `NetworkBehaviour` for network functionality
- ✅ Uses `IsOwner` property to determine ownership
- ✅ **Owner behavior:**
  - Processes collision detection
  - Handles balance/ragdoll logic
  - Subscribes to gameplay events (ResetBalanceEvent)
- ✅ **Non-owner behavior:**
  - Disables collision detectors (physics handled by owner)
  - Only observes state changes from network

**Key Methods:**
- `InitializeController()` - Runs for all clients
- `SetupOwnerBehavior()` - Only runs for owner
- `DisableCollisionDetectors()` - Disables physics on non-owners

### 2. FirstPersonInputService
**File:** `Assets/Scripts/Core/Player/FirstPersonInputService.cs`

- ✅ Checks ownership via `NetworkBehaviour.IsOwner`
- ✅ **Owner behavior:**
  - Enables all input actions (Move, Look, Jump, Sprint, etc.)
  - Publishes input events via MessageHub
  - Locks cursor for FPS controls
- ✅ **Non-owner behavior:**
  - Completely disables the component (`enabled = false`)
  - No input processing or event publishing

**Key Methods:**
- `Start()` - Checks ownership and disables if not owner
- `IsOwner()` - Helper method to check NetworkBehaviour ownership
- `OnEnable()/OnDisable()` - Guards with ownership checks

### 3. FirstPersonMovementHandler
**File:** `Assets/Scripts/Core/Player/FirstPersonMovementHandler.cs`

- ✅ Checks ownership via `NetworkBehaviour` reference
- ✅ **Owner behavior:**
  - Processes movement input and physics
  - Handles camera rotation
  - Applies gravity and ground checks
  - Subscribes to JumpEvent
- ✅ **Non-owner behavior:**
  - Skips all Update/LateUpdate logic
  - No event subscriptions
  - Movement is synced from network

**Key Methods:**
- `Update()` - Early returns if not owner
- `LateUpdate()` - Early returns if not owner
- `RegisterEvents()` - Only subscribes if owner
- `IsOwner()` - Helper method to check NetworkBehaviour ownership

## Network Synchronization

### NetworkObject Configuration
The player prefab (`PlayerCapsule.prefab`) has:
- ✅ `NetworkObject` component attached
- ✅ `SynchronizeTransform: 1` enabled (basic position sync)
- ✅ `Ownership: 1` (client ownership mode)
- ✅ Spawned via `networkObject.SpawnAsPlayerObject(clientId, true)`

### Current Synchronization
**Built-in NetworkObject Transform Sync:**
- Position, rotation syncing via `SynchronizeTransform`
- Suitable for basic player movement
- May experience slight jitter with fast movement

### Optional: Enhanced Synchronization
For smoother replication, consider adding a `NetworkTransform` component:

```csharp
// In Unity Editor or via code:
// Add NetworkTransform component to PlayerCapsule prefab
// Configure:
// - Sync Position X, Y, Z
// - Sync Rotation Y (for character facing)
// - Interpolate: true (smooth movement)
// - Use Half Floats: true (bandwidth optimization)
```

## Per-Player Dependency Injection

### PlayerProvider
**File:** `Assets/Scripts/Core/Network/PlayerProvider.cs`

- ✅ Creates isolated DI scope per player
- ✅ Each player gets:
  - Own `SessionInventory` instance
  - Own `ItemDropper` instance
  - Shared `IMessageHub` (global singleton)
  - Shared `ScatterConfigSO`

### PlayerSpawner
**File:** `Assets/Scripts/Core/Network/PlayerSpawner.cs`

- ✅ Spawns players on server when clients connect
- ✅ Uses `PlayerProvider` to create players with DI
- ✅ Assigns ownership via `SpawnAsPlayerObject(clientId, true)`
- ✅ Sets up Cinemachine camera for local player

## Testing Checklist

### Local Player (Owner)
- [ ] Input controls work (WASD, mouse look, jump, sprint)
- [ ] Movement is smooth and responsive
- [ ] Camera follows and rotates correctly
- [ ] Collision detection works
- [ ] Balance/ragdoll system activates on impact
- [ ] Inventory system works (pick up, drop items)
- [ ] Push mechanic works

### Remote Players (Non-Owner)
- [ ] Remote player appears in scene
- [ ] Position updates from network
- [ ] Rotation syncs correctly
- [ ] Animations sync (if using NetworkAnimator)
- [ ] No input processing on remote players
- [ ] No duplicate collision detection
- [ ] Remote player's inventory changes visible (if networked)

### Network Events
- [ ] Player spawns when client connects
- [ ] Player despawns when client disconnects
- [ ] Ownership is correctly assigned
- [ ] Multiple players can connect simultaneously
- [ ] Each player has isolated inventory

## Architecture Flow

```
Client Connects
    ↓
PlayerSpawner.OnClientConnected (Server)
    ↓
PlayerProvider.CreatePlayer
    ↓
Create Per-Player DI Scope
    ↓
Instantiate PlayerPrefab with DI
    ↓
NetworkObject.SpawnAsPlayerObject(clientId)
    ↓
On Owner Client:
    - FirstPersonInputService: Enabled, processes input
    - FirstPersonMovementHandler: Processes movement
    - PlayerController: Handles physics & collisions
    ↓
On Non-Owner Clients:
    - FirstPersonInputService: Disabled
    - FirstPersonMovementHandler: Skips updates
    - PlayerController: Collision detectors disabled
    ↓
Network syncs position/rotation to all clients
```

## Important Notes

1. **MessageHub Scope:**
   - `IMessageHub` is a global singleton
   - Events published by one player are seen by all
   - Use client-side filtering if needed

2. **CharacterController Limitation:**
   - `CharacterController` doesn't work well with NetworkRigidbody
   - Current setup uses basic transform sync
   - Consider `NetworkTransform` for smoother replication

3. **Camera Setup:**
   - Only the owner should control the camera
   - Remote players don't need camera updates
   - Cinemachine is set up in `PlayerSpawner` for local player

4. **Physics Authority:**
   - Owner client has physics authority
   - Owner processes collisions and sends results
   - Non-owners observe synced state

## Future Enhancements

1. **Add NetworkTransform** for smoother position/rotation sync
2. **Add NetworkAnimator** if character animations need syncing
3. **Implement RPCs** for gameplay actions (push, interact)
4. **Add NetworkVariables** for syncing balance state, inventory, etc.
5. **Implement client prediction** for more responsive movement
6. **Add lag compensation** for hit detection if needed

## References

- VContainer: https://vcontainer.hadashikick.jp/
- Unity Netcode: https://docs-multiplayer.unity3d.com/netcode/current/
- NetworkBehaviour: https://docs-multiplayer.unity3d.com/netcode/current/basics/networkbehavior/
- NetworkTransform: https://docs-multiplayer.unity3d.com/netcode/current/components/networktransform/

