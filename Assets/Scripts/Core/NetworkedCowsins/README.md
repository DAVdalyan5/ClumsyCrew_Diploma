# Networked Cowsins FPS Controller

## Overview
This package provides network-aware versions of Cowsins FPS Engine components for multiplayer gameplay using Unity Netcode for GameObjects.

## Components

### 1. NetworkedCowsinsPlayerController
Main NetworkBehaviour that manages ownership-based behavior for Cowsins players.

**Features:**
- Ownership-based input processing
- Server-authoritative movement
- Teleportation and force application RPCs
- Automatic component reference management

**Usage:**
```csharp
// Get the controller from a player GameObject
var controller = GetComponent<NetworkedCowsinsPlayerController>();

// Teleport player (server-authoritative)
controller.TeleportPlayerServerRpc(new Vector3(0, 2, 0));

// Apply force to player
controller.ApplyForceServerRpc(new Vector3(0, 500, 0), ForceMode.Impulse);
```

### 2. NetworkedCowsinsInputManager
Network-aware replacement for Cowsins InputManager.

**Features:**
- Only processes input for owner clients
- Automatic disable/enable based on ownership
- Compatible with all Cowsins input events

**Note:** This component automatically replaces the original `InputManager` in the networked prefab.

### 3. NetworkedCowsinsStateSync
Synchronizes Cowsins player states across the network.

**Synchronized States:**
- Grounded state
- Crouching state
- Sliding state
- Climbing state
- Wall running state
- Dashing state

**Configuration:**
- Adjust sync rate for performance
- Enable/disable specific state synchronization
- Owner-write, everyone-read permission model

### 4. NetworkedCowsinsSetup (Editor)
Editor utilities for creating and configuring networked prefabs.

**Menu Items:**
- `Tools/Networked Cowsins/Create Networked Player Prefab`
- `Tools/Networked Cowsins/Setup Guide`
- `Tools/Networked Cowsins/Verify Setup`

## Setup Instructions

### Quick Setup
1. Open Unity Editor
2. Go to `Tools/Networked Cowsins/Create Networked Player Prefab`
3. Follow the prompts to create the networked prefab
4. Assign the new prefab to your PlayerSpawner
5. Add the prefab to NetworkManager's Network Prefabs List

### Manual Setup
1. Duplicate `MovementCowsinsFPSController.prefab`
2. Add required network components:
   - `NetworkObject`
   - `NetworkTransform` (configure interpolation)
   - `NetworkedCowsinsPlayerController`
   - `NetworkedCowsinsInputManager` (replace original InputManager)
   - `NetworkedCowsinsStateSync` (optional)
3. Configure component references
4. Update PlayerSpawner to use new prefab

## Integration with Existing Systems

### VContainer Dependency Injection
The `NetworkedCowsinsPlayerController` supports VContainer injection:

```csharp
[Inject]
public void Init(IMessageHub messageHub)
{
    // MessageHub is automatically injected
}
```

### MessageHub Events
All components work with the existing MessageHub system for event publishing/subscription.

### Existing Player Systems
Compatible with:
- Inventory systems
- Ragdoll systems
- Animation controllers
- UI systems

### Weapons and shooting
The networked Cowsins controller **supports the FPS engine weapon system** (WeaponController, shooting, reload, aim). Weapons and shooting work locally for the owner. For **networked damage to other players**, add **NetworkedHealth** on the player root (implements Cowsins `IDamageable`); the setup menu can add it automatically. See **WEAPON_SYSTEM_INTEGRATION.md** for details.

## Network Architecture

### Ownership Model
- **Owner Client**: Processes input, handles physics, publishes events
- **Non-owner Clients**: Observe synced state, render visuals, play audio
- **Server**: Validates actions, manages game state, authorizes changes

### Synchronization
1. **Transform Sync**: Via `NetworkTransform` component
2. **State Sync**: Via `NetworkedCowsinsStateSync` component
3. **Custom Sync**: Extend with `NetworkVariable<T>` for game-specific data

### RPC Communication
- **ServerRpc**: Client → Server requests
- **ClientRpc**: Server → Client updates
- **Ownership RPCs**: Specific to owner-client communication

## Performance Considerations

### Bandwidth Optimization
- Adjust `NetworkTransform` sync rate (10-30 Hz recommended)
- Enable interpolation for smooth movement
- Use delta compression where available
- Limit state synchronization to essential states

### Client Performance
- Non-owners have physics disabled (`Rigidbody.isKinematic = true`)
- Input processing disabled for non-owners
- Consider LOD for distant players

### Server Performance
- Server-authoritative validation
- Rate limiting for RPC calls
- Efficient state change detection

## Testing

### Automated Tests
Use `NetworkedCowsinsTest` component to run automated tests:

1. Add `NetworkedCowsinsTest` to a GameObject in scene
2. Assign the networked player prefab
3. Run tests via Context Menu or automatically

**Test Coverage:**
- Component presence verification
- Ownership behavior
- Network synchronization
- RPC functionality

### Manual Testing
1. Build standalone executable
2. Run multiple instances (Host + Clients)
3. Verify:
   - Each client controls only their player
   - Movement is synchronized
   - States are correctly replicated
   - Input works only for owned player

## Troubleshooting

### Common Issues

#### Players don't move on other clients
- Check `NetworkTransform` interpolation settings
- Verify `NetworkObject` is spawned
- Ensure `Rigidbody` sync mode is correct

#### Input works for all clients
- Verify `NetworkedCowsinsInputManager` replaced original `InputManager`
- Check ownership checks in input processing

#### Physics don't sync properly
- Configure `Owner Only Physics` setting
- Check `Rigidbody` kinematic state for non-owners

#### State synchronization delayed
- Adjust sync rate in `NetworkedCowsinsStateSync`
- Check network bandwidth and latency

### Debugging
Enable debug logging in components:
```csharp
// In NetworkedCowsinsPlayerController.cs
Debug.Log($"[NetworkedCowsins] Client {OwnerClientId} - IsOwner: {IsOwner}");
```

Use Unity's Netcode Profiler for network analysis.

## Extending the System

### Adding Custom Network Variables
```csharp
public class ExtendedNetworkedPlayer : NetworkedCowsinsPlayerController
{
    private NetworkVariable<int> _score = new NetworkVariable<int>();
    private NetworkVariable<PlayerTeam> _team = new NetworkVariable<PlayerTeam>();
    
    // Add custom RPCs and logic
}
```

### Custom State Synchronization
```csharp
public class CustomStateSync : NetworkBehaviour
{
    private NetworkVariable<CustomPlayerState> _state = new NetworkVariable<CustomPlayerState>();
    
    // Sync custom states as needed
}
```

### Integration with Game Systems
```csharp
public class GameIntegration : NetworkBehaviour
{
    [Inject] private NetworkedCowsinsPlayerController _playerController;
    
    private void OnPlayerDamaged(float damage)
    {
        // Integrate with health system, UI, etc.
    }
}
```

## Best Practices

1. **Always test with multiple clients**
2. **Use server-authoritative design**
3. **Optimize network traffic**
4. **Handle edge cases (disconnects, reconnects)**
5. **Implement client-side prediction for critical actions**
6. **Use interpolation for smooth visuals**
7. **Monitor performance metrics**

## Support

For issues or questions:
1. Check the setup guide (`NETWORKED_COWSINS_SETUP.md`)
2. Review component documentation
3. Test with the provided test scripts
4. Consult Unity Netcode documentation

## License
This package is part of the HeistNSeek project and follows the project's licensing terms.

## Credits
- Built on Cowsins FPS Engine
- Integrated with Unity Netcode for GameObjects
- Designed for VContainer dependency injection
- Compatible with existing HeistNSeek architecture