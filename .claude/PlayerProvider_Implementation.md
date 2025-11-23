# PlayerProvider System - Implementation Summary

## Overview
Created a per-player dependency injection system for multiplayer scenarios where each player needs isolated instances of SessionInventory and ItemDropper.

## Problem Solved
- **Before**: Player components registered with `RegisterComponentInHierarchy` only worked for single player
- **Before**: SessionInventory and ItemDropper were Singletons shared across all players
- **Before**: Players joining mid-game couldn't get proper dependency injection
- **After**: Each player gets their own child DI scope with isolated per-player services

## Architecture

### **PlayerProvider.cs** (`Assets/Scripts/Core/Player/PlayerProvider.cs`)
Central service that manages player creation with VContainer child scopes.

**Key Methods:**
- `CreatePlayer(clientId, playerPrefab, position, rotation)` - Creates player with isolated DI scope
- `RemovePlayer(clientId)` - Cleans up player and disposes their scope
- `GetPlayerInventory(clientId)` - Retrieves specific player's inventory
- `GetPlayerInstance(clientId)` - Retrieves specific player's GameObject

**How it works:**
1. Creates `IScopedObjectResolver` child scope using `_parentResolver.CreateScope()`
2. Registers per-player services in child scope:
   - `SessionInventory` (Singleton within scope)
   - `ItemDropper` (Singleton within scope)
3. Instantiates player prefab using `childScope.Instantiate()`
4. Manually creates `ItemDropper` (because it needs `PlayerController` reference)
5. Stores scope in dictionary: `Dictionary<ulong, PlayerScope>`
6. On disconnect: disposes ItemDropper → destroys GameObject → disposes scope

**Dependencies:**
- `IObjectResolver _parentResolver` (injected)
- `ScatterConfigSO _scatterConfig` (injected)

---

## Modified Files

### **GameplayLifetimeScope.cs**
**Removed:**
- `RegisterComponentInHierarchy<PlayerController>()` - redundant with autoInject
- `RegisterComponentInHierarchy<FirstPersonInputService>()` - redundant with autoInject
- `RegisterComponentInHierarchy<FirstPersonMovementHandler>()` - redundant with autoInject
- `RegisterComponentInHierarchy<CharacterPusher>()` - redundant with autoInject
- `Register<SessionInventory>(Lifetime.Singleton)` - now per-player
- `Register<ItemDropper>(Lifetime.Singleton)` - now per-player
- `Register<ItemPickup>(Lifetime.Transient)` - handled by autoInject

**Added:**
- `Register<PlayerProvider>(Lifetime.Singleton)` - the new player management service

**Kept:**
- Testing services (PlainService, MonoService, InjectedConsumer)
- `ScatterConfigSO` instance registration (shared config)
- `PlayerSpawner` instance registration (user added)
- `GameplayEntryPoint` registration

---

### **PlayerSpawner.cs** (`Assets/Scripts/Core/Network/PlayerSpawner.cs`)
**Changes:**
- Inject `PlayerProvider` instead of `IObjectResolver`
- `SpawnPlayerForClient()` calls `playerProvider.CreatePlayer()` instead of `container.Instantiate()`
- `OnClientDisconnected()` calls `playerProvider.RemovePlayer(clientId)` for cleanup
- `SpawnPlayerAtPosition()` updated to use PlayerProvider
- User added `SpawnPlayer()` helper method

---

## Dependency Flow

### Per-Player Services (isolated per player):
```
PlayerProvider creates child scope
  ├─ SessionInventory (per-player singleton)
  │   └─ IMessageHub (inherited from parent)
  └─ ItemDropper (per-player singleton)
      ├─ SessionInventory (from child scope)
      ├─ IMessageHub (inherited from parent)
      ├─ PlayerController (from instantiated prefab)
      └─ ScatterConfigSO (registered in child scope)
```

### Global Services (shared across all players):
- `IMessageHub` - registered in BootstrapLifetimeScope
- `ScatterConfigSO` - registered in GameplayLifetimeScope
- `PlayerProvider` - registered in GameplayLifetimeScope

### Player Components (auto-injected):
- `PlayerController` → receives `IMessageHub`
- `FirstPersonInputService` → receives `IMessageHub`
- `FirstPersonMovementHandler` → receives `IMessageHub`
- `CharacterPusher` → receives `IMessageHub`

All player components get dependencies through VContainer's autoInject feature when instantiated via `childScope.Instantiate()`.

---

## Key Technical Details

**VContainer Child Scopes:**
- `IScopedObjectResolver childScope = parentResolver.CreateScope(builder => {...})`
- Returns `IScopedObjectResolver`, not `LifetimeScope`
- Use `childScope.Instantiate()` not `childScope.Container.Instantiate()`
- Use `childScope.Resolve<T>()` not `childScope.Container.Resolve<T>()`

**Lifecycle Management:**
- Child scope created per player in `CreatePlayer()`
- Stored in `Dictionary<ulong, PlayerScope>`
- Disposed in `RemovePlayer()` when player disconnects
- ItemDropper.Dispose() unsubscribes from MessageHub events

**PlayerScope Structure:**
```csharp
private class PlayerScope
{
    public IScopedObjectResolver Scope { get; set; }
    public GameObject PlayerInstance { get; set; }
    public SessionInventory SessionInventory { get; set; }
    public ItemDropper ItemDropper { get; set; }
}
```

---

## Usage Example

```csharp
// PlayerSpawner automatically uses PlayerProvider
private void OnClientConnected(ulong clientId)
{
    // PlayerProvider.CreatePlayer() called internally
    // Creates child scope → registers per-player services → instantiates player
}

private void OnClientDisconnected(ulong clientId)
{
    // PlayerProvider.RemovePlayer() called
    // Disposes services → destroys GameObject → disposes scope
}

// Accessing player's inventory from elsewhere
SessionInventory playerInventory = playerProvider.GetPlayerInventory(clientId);
```

---

## Files Summary

**Created:**
- `Assets/Scripts/Core/Player/PlayerProvider.cs`

**Modified:**
- `Assets/Scripts/Core/InjectionBase/GameplayLifetimeScope.cs`
- `Assets/Scripts/Core/Network/PlayerSpawner.cs`

**Dependencies:**
- VContainer 1.17.0 (child scopes, IScopedObjectResolver)
- Unity Netcode (NetworkManager, NetworkObject)
- Easy.MessageHub (IMessageHub)

---

## Result
✅ Multiple players can exist with proper isolated DI
✅ Players joining mid-game get full dependency injection
✅ Each player has their own SessionInventory and ItemDropper
✅ Proper cleanup when players disconnect
✅ GameplayLifetimeScope is clean and minimal
✅ Works out of the box with existing player prefabs

---

## Important Notes for Future Development

### Adding New Per-Player Services
To add a new service that should be isolated per player:

1. Create your service class (e.g., `PlayerStats.cs`)
2. Add registration in `PlayerProvider.CreatePlayer()`:
   ```csharp
   IScopedObjectResolver childScope = _parentResolver.CreateScope(builder =>
   {
       builder.Register<SessionInventory>(Lifetime.Singleton);
       builder.Register<ItemDropper>(Lifetime.Singleton);
       builder.Register<PlayerStats>(Lifetime.Singleton); // NEW
   });
   ```
3. Resolve and store in PlayerScope if needed:
   ```csharp
   var playerStats = childScope.Resolve<PlayerStats>();
   _playerScopes[clientId] = new PlayerScope
   {
       Scope = childScope,
       PlayerInstance = playerInstance,
       SessionInventory = sessionInventory,
       ItemDropper = itemDropper,
       PlayerStats = playerStats // Add to PlayerScope class
   };
   ```

### Debugging Tips
- Check `PlayerProvider._playerScopes.Count` to see active players
- Use `PlayerProvider.GetPlayerInventory(clientId)` to inspect per-player state
- Each player's scope is independent - changes in one won't affect others
- ItemDropper subscribes to BalanceLostEvent - ensure proper disposal on disconnect

### Known Limitations
- ItemPickup (scene-placed items) still uses static reference fallback
- For multiplayer pickup, need proximity/interaction detection to determine which player's inventory to use
- NetworkManager must be registered in LifetimeScope for injection into PlayerSpawner
