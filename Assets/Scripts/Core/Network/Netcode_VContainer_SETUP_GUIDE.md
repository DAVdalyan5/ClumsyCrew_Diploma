# VContainer + Unity Netcode Setup Guide

## What Was Implemented

Your player spawning system now uses VContainer for automatic dependency injection when network players are spawned at runtime. This solves the issue where runtime-spawned players don't receive injected dependencies.

## Files Created/Modified

1. ✅ **PlayerSpawner.cs** - Handles spawning players through VContainer
2. ✅ **GameLifetimeScope.cs** - VContainer configuration for the game
3. ✅ **PlayerBehavior.cs** - Updated to receive injected dependencies

## Setup Steps

### 1. Setup NetworkManager (Inspector)

In your scene's NetworkManager component:
- **LEAVE "Player Prefab" FIELD EMPTY** (None)
- This prevents automatic spawning so PlayerSpawner can handle it

### 2. Create a LifetimeScope GameObject

1. Create an empty GameObject in your main scene
2. Name it `GameLifetimeScope`
3. Add the `GameLifetimeScope` component to it
4. In the Inspector, assign:
   - **Game Loop Manager**: Your GameLoopManager in the scene
   - **Game Timer Service**: Your GameTimerService in the scene
   - **Network Manager**: Your NetworkManager in the scene

### 3. Add PlayerSpawner to the Scene

The `PlayerSpawner` is automatically registered as an EntryPoint by VContainer, so it will be created automatically when the scene starts. However, you need to assign some references:

1. The PlayerSpawner will automatically get its references from VContainer
2. You need to add a public reference in the GameLifetimeScope to configure it

**Update GameLifetimeScope.cs:**

```csharp
[SerializeField] private GameObject playerPrefab;
[SerializeField] private Transform defaultSpawnPoint;
```

Then in Configure:
```csharp
builder.RegisterEntryPoint<PlayerSpawner>()
    .WithParameter("playerPrefab", playerPrefab)
    .WithParameter("defaultSpawnPoint", defaultSpawnPoint);
```

**OR** create a separate GameObject with PlayerSpawner component:

1. Create an empty GameObject named `PlayerSpawner`
2. Add the `PlayerSpawner` component
3. Assign in Inspector:
   - **Player Prefab**: Your player prefab (must have NetworkObject component)
   - **Default Spawn Point**: Transform for spawn location

Then update GameLifetimeScope.cs:
```csharp
[SerializeField] private PlayerSpawner playerSpawner;

// In Configure:
builder.RegisterComponent(playerSpawner);
```

### 4. Prepare Your Player Prefab

Your player prefab must have:
- ✅ `NetworkObject` component
- ✅ `PlayerBehavior` component
- ✅ Any other required components

**Make sure the player prefab is NOT in the scene!**

### 5. Test It

1. Start the game as Host
2. Connect clients
3. Players should spawn automatically with all dependencies injected!

## How It Works

```
Client Connects
    ↓
PlayerSpawner.OnClientConnected()
    ↓
container.Instantiate(playerPrefab) ← VContainer injects dependencies here!
    ↓
networkObject.SpawnAsPlayerObject(clientId)
    ↓
Player spawned on network with DI complete ✓
```

## Adding More Injected Dependencies

In `PlayerBehavior.cs`, just add more `[Inject]` fields:

```csharp
[Inject] private IMyService myService;
[Inject] private IAnotherService anotherService;
```

Then register them in `GameLifetimeScope.cs`:

```csharp
builder.Register<IMyService, MyServiceImpl>(Lifetime.Singleton);
```

## Troubleshooting

### "Player prefab needs NetworkObject component!"
- Add a NetworkObject component to your player prefab

### "NetworkManager not found!"
- Make sure NetworkManager is assigned in GameLifetimeScope inspector

### Dependencies are null
- Check that services are registered in GameLifetimeScope.Configure()
- Make sure GameLifetimeScope GameObject exists in the scene

### Two players spawning
- Ensure NetworkManager's "Player Prefab" field is empty (None)

## Next Steps

You can now use `gameTimerService` and `gameLoopManager` directly in PlayerBehavior! Add any additional services you need through VContainer.


