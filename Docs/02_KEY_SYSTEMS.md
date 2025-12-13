# Key Systems & Interactions

## System Overview

```
┌──────────────────────────────────────────────────────────────┐
│                    Game State Machine                         │
│  (Controls overall game flow & phase transitions)             │
└──────────────────────────────────────────────────────────────┘
                    │
        ┌───────────┼───────────┐
        ↓           ↓           ↓
    ┌─────────┐ ┌──────────┐ ┌──────────┐
    │Bootstrap│ │ Loading  │ │ Gameplay │
    │ State   │ │  State   │ │  State   │
    └─────────┘ └──────────┘ └──────────┘
        │           │            │
        │           │            └─────────────┐
        │           │                          │
        ↓           ↓                          ↓
    ┌──────────────────────────────────────────────────┐
    │           GAMEPLAY SYSTEMS                       │
    ├──────────────────────────────────────────────────┤
    │                                                  │
    │  Player System          Inventory System        │
    │  ├─ PlayerController    ├─ SessionInventory    │
    │  ├─ Input Service       ├─ ItemPickup          │
    │  ├─ Movement Handler    ├─ ItemDropper         │
    │  ├─ Ragdoll/Balance     └─ WeaponModel         │
    │  └─ Animations                                  │
    │                                                  │
    │  Network System         Event System            │
    │  ├─ PlayerSpawner       ├─ MessageHub          │
    │  ├─ PlayerScope         ├─ State Events        │
    │  └─ Netcode Integration ├─ Game Events         │
    │                         └─ Inventory Events     │
    │                                                  │
    └──────────────────────────────────────────────────┘
```

---

## 1. Player System

### Components

| Component | File | Responsibility |
|-----------|------|-----------------|
| **PlayerController** | `Core/Player/PlayerController.cs` | Central player controller (NetworkBehaviour) |
| **FirstPersonInputService** | `Core/Player/FirstPersonInputService.cs` | Input handling |
| **FirstPersonMovementHandler** | `Core/Player/FirstPersonMovementHandler.cs` | Movement logic (FPS Engine integration) |
| **CharacterAnimationController** | `Core/Player/Character/CharacterAnimationController.cs` | Animation control |

### Player Subsystems

#### A. Movement System

**Responsibility:** Handle player movement, acceleration, jumping

**Key Properties:**
```csharp
public class FirstPersonMovementHandler
{
    public float CurrentSpeed { get; }
    public bool IsBalanced { get; set; }  // Affected by ragdoll
    public void Move(Vector2 inputDirection);
    public void Jump();
}
```

**Integration with FPS Engine:**
- Uses Starter Assets movement system
- Modified to support balance state
- Network synced via OwnerAuthoritativeNetworkAnimator

#### B. Input System

**Responsibility:** Capture & distribute player input

**Key Methods:**
```csharp
public class FirstPersonInputService
{
    public Vector2 GetMovementInput();
    public bool GetJumpInput();
    public bool GetInteractInput();
}
```

**Network Handling:**
- Input only processed by owner
- Non-owners receive synced position/animation

#### C. Ragdoll & Balance System

**Responsibility:** Detect impacts, manage balance state, trigger ragdoll

**Flow:**
```
Player Movement
        ↓
CollisionDetector.OnCollisionEnter()
        ↓
Check impact speed > threshold?
        ↓
Yes → BalanceLostEvent
      ├─ Publish via MessageHub
      ├─ Enable ragdoll
      ├─ Drop inventory items
      └─ Disable movement
        ↓
No → Continue moving

Player recovers → ResetBalanceEvent
      ├─ Disable ragdoll
      ├─ Restore movement
      └─ Restore balance
```

**Key Components:**
- **CollisionDetector** (`Core/Player/Mechanics/Ragdoll/CollisionDetector.cs`)
  - Monitors collision speed
  - Subscribes to events
  - Triggers RPC calls

- **BalanceInfo** (`Core/Player/Mechanics/Ragdoll/BalanceInfo.cs`)
  - Holds balance state
  - IsBalanced flag

- **RagdollUtilities** (`Core/Player/Mechanics/Ragdoll/RagdollUtilities.cs`)
  - Toggle ragdoll (enable/disable colliders & animator)
  - Track ragdoll state

#### D. Push Mechanic

**Responsibility:** Allow players to push each other

**Key Components:**
- **CharacterPusher** (`Core/Player/Mechanics/Pushing/CharacterPusher.cs`)
  - Applies force to pushable objects
  - Integrates with push detection

- **IPushable** (`Core/Player/Mechanics/Pushing/IPushable.cs`)
  - Interface for pushable objects

**Push Flow:**
```
Player initiates push
        ↓
CharacterPusher.Push()
        ↓
Find IPushable in range
        ↓
Apply force to target
        ↓
RPC: OnBalanceLostRpc() on target
        ↓
Ragdoll triggered on target
```

#### E. Network Animation Sync

**Responsibility:** Synchronize animations across network

**Key Component:**
- **OwnerAuthoritativeNetworkAnimator** (`Core/Player/Animation/OwnerAuthoritativeNetworkAnimator.cs`)
  - Owner controls animator
  - Broadcasts animation state to observers
  - Handles ragdoll sync

---

## 2. Inventory System

### Components

| Component | File | Responsibility |
|-----------|------|-----------------|
| **SessionInventory** | `Core/Inventory/SessionInventory/SessionInventory.cs` | Core inventory logic |
| **InventoryItem** | `Core/Inventory/SessionInventory/InventoryItem.cs` | Individual item representation |
| **ItemPickup** | `Core/Inventory/SessionInventory/ItemPickup.cs` | Item pickup logic |
| **ItemDropper** | `Core/Inventory/SessionInventory/ItemDropper.cs` | Item dropping logic |
| **ItemDataSO** | `Core/Inventory/Models/ItemDataSO.cs` | Item data definition |
| **WeaponDataSO** | `Core/Inventory/Models/WeaponDataSO.cs` | Weapon data definition |

### System Flow

```
┌──────────────────────────────────────────┐
│        SessionInventory                  │
│  List<InventoryItem> _items              │
├──────────────────────────────────────────┤
│ Public API:                              │
│  • AddItem(ItemDataSO, amount)          │
│  • RemoveItem(ItemDataSO, amount)       │
│  • GetItemCount(ItemDataSO)             │
│  • HasItem(ItemDataSO, amount)          │
│  • DropItems(position, impactSpeed)     │
│  • Clear()                               │
│                                          │
│ Events Published:                        │
│  • ItemAddedEvent                       │
│  • ItemRemovedEvent                     │
│  • ItemsDroppedEvent                    │
└──────────────────────────────────────────┘
```

### Item Management

#### Adding Items

```csharp
public bool AddItem(ItemDataSO itemData, int amount = 1)
{
    // 1. Check if stackable
    // 2. Try to stack with existing items
    // 3. Create new entries for remainder
    // 4. Publish ItemAddedEvent
    // 5. Return success
}
```

**Stacking Rules:**
- Only stackable items stack
- Each entry has maxStackSize
- Multiple entries allowed for same item

#### Removing Items

```csharp
public bool RemoveItem(ItemDataSO itemData, int amount = 1)
{
    // 1. Iterate inventory (reverse)
    // 2. Remove from stacks
    // 3. Remove empty entries
    // 4. Publish ItemRemovedEvent
    // 5. Return success
}
```

#### Dropping Items

```csharp
public List<ItemDataSO> DropItems(Vector3 dropPosition, float impactSpeed)
{
    // 1. Calculate items to drop (all items on impact)
    // 2. Randomly select items from inventory
    // 3. Remove from inventory
    // 4. Publish ItemsDroppedEvent
    // 5. Return dropped items list
}
```

**Drop Trigger:**
- Triggered when `BalanceLostEvent` occurs
- All items drop (100% drop rate)
- Items scattered based on impactSpeed

### Item Pickup

```csharp
public class ItemPickup : MonoBehaviour
{
    // Detects when player enters trigger
    // Adds item to SessionInventory
    // Destroys pickup object
}
```

---

## 3. Network System

### Components

| Component | File | Responsibility |
|-----------|------|-----------------|
| **PlayerSpawner** | `Core/Network/PlayerSpawner.cs` | Spawn players on connection |
| **PlayerScope** | `Core/Network/PlayerScope.cs` | Per-player DI scope |
| **ClientSidePlayerConfigurator** | `Core/Network/ClientSidePlayerConfigurator.cs` | Client-side setup |

### Network Flow

```
Client connects to host
        ↓
OnClientConnected(clientId)
        ↓
PlayerSpawner.SpawnPlayerForClient(clientId)
        ├─ Server instantiates player prefab
        ├─ Spawns as PlayerObject (for clientId)
        └─ Passes to client
        ↓
Client receives PlayerObject
        ↓
ClientSidePlayerConfigurator.OnNetworkSpawn()
        ├─ Create per-player DI scope (PlayerScope)
        ├─ Register player-specific services
        ├─ Inject into PlayerController
        └─ Initialize
        ↓
Player is ready to play
```

### Owner vs Observer

**Owner Client:**
- Processes input
- Handles collision detection
- Controls movement physics
- Triggers RPCs

**Observer Clients:**
- Disable CollisionDetectors
- Receive position updates
- Observe animation state
- Respond to RPC calls

---

## 4. Event System

### Event Bus Architecture

```
All events flow through IMessageHub (singleton)

Producer            MessageHub              Consumers
  │                    │                        │
  ├─ PlayerController  │                        │
  ├─ StateMachine      ├─ Subscriptions         │
  ├─ SessionInventory  │   (Type → Handlers)    │
  │                    │                        │
  │                    ├─→ UI System
  │                    ├─→ Audio System
  │                    ├─→ Analytics
  │                    └─→ Game Logic
```

### Event Categories

#### State Machine Events
- **StateEnteredEvent** - Game phase changed
- **StateExitedEvent** - Exiting previous phase

#### Player Events
- **BalanceLostEvent** - Player lost balance (impact speed included)
- **BalanceRegainedEvent** - Player regained balance
- **PushEvent** - Player pushed another object
- **JumpEvent** - Player jumped
- **InteractEvent** - Player interacted with something
- **ResetBalanceEvent** - Request to reset balance

#### Inventory Events
- **ItemAddedEvent** - Item(s) added (amount & total count included)
- **ItemRemovedEvent** - Item(s) removed (amount & total count included)
- **ItemsDroppedEvent** - Items dropped (position & impact speed included)

### Event Publishing Pattern

```csharp
public class MyService
{
    private readonly IMessageHub _messageHub;

    public void SomethingHappened(float impactSpeed)
    {
        // Publish event
        _messageHub.Publish(new BalanceLostEvent(impactSpeed));
    }
}
```

### Event Subscription Pattern

```csharp
// In service (constructor)
public class MyListener
{
    public MyListener(IMessageHub messageHub)
    {
        messageHub.Subscribe<BalanceLostEvent>(OnBalanceLost);
    }

    private void OnBalanceLost(BalanceLostEvent evt)
    {
        Debug.Log($"Lost balance with speed: {evt.ImpactSpeed}");
    }
}

// In MonoBehaviour (with safe disposal)
public class MyUI : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        messageHub.SubscribeSafe<BalanceLostEvent>(this, OnBalanceLost);
    }

    private void OnBalanceLost(BalanceLostEvent evt) { }
}
```

---

## 5. Game State Machine

### States

#### BootstrapState
**Triggered by:** BootstrapEntryPoint.Start()
**Responsibility:** Bootstrap initialization

```csharp
public override void Enter(object payload = null)
{
    // 1. Initialize bootstrap systems
    // 2. Load Main scene additively
    // 3. Transition to LoadingState
    StateMachine.Enter<LoadingState>(_gameplaySceneIndex);
}
```

#### LoadingState
**Triggered by:** BootstrapState or GameplayState
**Responsibility:** Async scene loading

```csharp
public override void Enter(object payload = null)
{
    // 1. Show loading screen
    // 2. Await async scene load
    // 3. Transition to next state
    StateMachine.Enter<GameplayState>();
}
```

#### GameplayState
**Triggered by:** LoadingState
**Responsibility:** Active gameplay

```csharp
public override void Enter(object payload = null)
{
    // 1. Hide loading screen
    // 2. Enable player input
    // 3. Resume game
}

public override void Exit()
{
    // Cleanup gameplay-specific systems
}
```

### State Transitions

```
[Bootstrap] → [Loading] → [Gameplay]
                ↑            ↓
                └────────────┘
                (Level reload)
```

---

## 6. Dependency Injection Scopes

### Bootstrap Scope Services

```csharp
// BootstrapLifetimeScope.Configure()
builder.RegisterInstance<IMessageHub>(messageHub);           // Singleton
builder.Register<GameStateMachine>(Lifetime.Singleton);      // Singleton
builder.Register<BootstrapState>(Lifetime.Singleton);        // Singleton
builder.Register<LoadingState>(Lifetime.Singleton);          // Singleton
builder.Register<GameplayState>(Lifetime.Singleton);         // Singleton
builder.RegisterEntryPoint<BootstrapEntryPoint>();          // Singleton
```

### Gameplay Scope Services

```csharp
// GameplayLifetimeScope.Configure()
builder.Register<SessionInventory>(Lifetime.Singleton);
builder.RegisterEntryPoint<GameplayEntryPoint>();
// + Scene-specific services
```

### Per-Player Scope Services

```csharp
// PlayerScope.Configure()
builder.Register<IInputService, FirstPersonInputService>();
builder.Register<FirstPersonMovementHandler>();
// + Player-specific services
```

---

## System Interactions

### Adding Inventory Item

```
ItemPickup.OnTriggerEnter()
    ↓
SessionInventory.AddItem()
    ├─ Stacks item
    ├─ Creates inventory entries
    └─ Publishes ItemAddedEvent
        ↓
    IMessageHub.Publish(ItemAddedEvent)
        ├─ UI listens → Update display
        ├─ Audio listens → Play pickup sound
        └─ Game logic listens → Update state
```

### Player Loses Balance

```
CollisionDetector.OnCollisionEnter()
    ├─ Calculate impact speed
    └─ If > threshold:
        ↓
PlayerController.OnBalanceLostRpc()
    ├─ Set balance to false
    ├─ Enable ragdoll
    ├─ Publish BalanceLostEvent
    │   ├─ Drop items via SessionInventory
    │   └─ Disable movement
    └─ Broadcast to all clients via RPC
```

### Pushing Another Player

```
CharacterPusher.Push(target)
    ├─ Find target in range
    ├─ Apply force
    └─ Call target.TriggerPushRagdoll()
        ↓
PlayerController.TriggerPushRagdoll()
    └─ Call OnBalanceLostRpc()
        ├─ Enable ragdoll on target
        ├─ Disable target's movement
        └─ Drop target's items
```

---

## Data Flow Diagram

```
┌──────────────────┐
│  Input System    │
│ (Owner only)     │
└────────┬─────────┘
         │
         ↓
┌──────────────────┐      ┌──────────────────┐
│  Movement        │─────→│  Collision       │
│  Handler         │      │  Detector        │
└────────┬─────────┘      └────────┬─────────┘
         │                         │
         ↓                         ↓
┌──────────────────┐      ┌──────────────────┐
│  Animation Sync  │      │  Impact Detection│
│  (Network)       │      │  (Balance Loss)  │
└────────┬─────────┘      └────────┬─────────┘
         │                         │
         └────────────┬────────────┘
                      ↓
            ┌──────────────────────┐
            │  Event Publishing    │
            │  (IMessageHub)       │
            └──────────┬───────────┘
                       │
         ┌─────────────┼──────────────┐
         ↓             ↓              ↓
    ┌────────┐  ┌──────────┐  ┌──────────┐
    │ Ragdoll│  │ Inventory│  │  Audio   │
    │System  │  │ Dropping │  │  System  │
    └────────┘  └──────────┘  └──────────┘
```

---

## Key Data Structures

### BalanceInfo
```csharp
public class BalanceInfo
{
    public bool IsBalanced { get; set; }
    public float LastImpactSpeed { get; set; }
}
```

### InventoryItem
```csharp
public class InventoryItem
{
    public ItemDataSO ItemData { get; }
    public int CurrentStackCount { get; }
    public bool CanStackWith(ItemDataSO other);
    public void AddToStack(int amount);
    public int RemoveFromStack(int amount);
}
```

### ItemDataSO
```csharp
public class ItemDataSO : ScriptableObject
{
    public string itemName;
    public float weight;
    public bool isStackable;
    public int maxStackSize;
}
```

