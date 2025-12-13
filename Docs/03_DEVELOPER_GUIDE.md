# Developer Quick Reference Guide

## Getting Started

### Prerequisites
- Unity 6 (6000.2.10f1)
- C# 9+
- Basic understanding of Unity and MonoBehaviours
- Familiarity with async/await patterns

### First Steps
1. Open project in Unity 6.0
2. Navigate to `Assets/Scenes/Bootstrap.unity`
3. Open `CLAUDE.md` in project root for project guidelines
4. Read `Docs/01_ARCHITECTURE_OVERVIEW.md` for architecture patterns

---

## Common Tasks

### Task 1: Add a New Player Stat/Property

**Goal:** Add a new player property (e.g., stamina, health, etc.)

**Steps:**

1. **Create data model** in `Core/Player/Character/Models/`
```csharp
namespace HeistNSeek.Core.Player
{
    public class PlayerStats
    {
        public float Health { get; set; } = 100f;
        public float Stamina { get; set; } = 100f;
    }
}
```

2. **Add to PlayerController** in `Core/Player/PlayerController.cs`
```csharp
public class PlayerController : NetworkBehaviour
{
    private PlayerStats _stats;

    private void InitializeController()
    {
        _stats = new PlayerStats();
    }
}
```

3. **Create event** for stat changes in `Events/`
```csharp
public class HealthChangedEvent
{
    public float NewHealth { get; set; }
    public float MaxHealth { get; set; }
}
```

4. **Publish event** when stat changes
```csharp
_messageHub.Publish(new HealthChangedEvent
{
    NewHealth = _stats.Health,
    MaxHealth = 100f
});
```

5. **Subscribe to event** in UI or systems that need it
```csharp
public class HealthUI : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub.SubscribeSafe<HealthChangedEvent>(this, OnHealthChanged);
    }

    private void OnHealthChanged(HealthChangedEvent evt)
    {
        healthBar.SetValue(evt.NewHealth / evt.MaxHealth);
    }
}
```

---

### Task 2: Create a New Game State

**Goal:** Add a pause state, menu state, or other game phase

**Steps:**

1. **Create state class** in `Core/StateMachine/States/`
```csharp
using HeistNSeek.Core.StateMachine;
using UnityEngine;

namespace HeistNSeek.Core.StateMachine.States
{
    public class PauseState : BaseState
    {
        private readonly IMessageHub _messageHub;

        public PauseState(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        public override void Enter(object payload = null)
        {
            Debug.Log("[PauseState] Game paused");
            Time.timeScale = 0f;
        }

        public override void Exit()
        {
            Debug.Log("[PauseState] Game resumed");
            Time.timeScale = 1f;
        }
    }
}
```

2. **Register in BootstrapLifetimeScope** (`Core/InjectionBase/BootstrapLifetimeScope.cs`)
```csharp
protected override void Configure(IContainerBuilder builder)
{
    // ... existing code ...

    builder.Register<PauseState>(Lifetime.Singleton).As<IState>();
}
```

3. **Transition to new state** from other states
```csharp
public override void Enter(object payload = null)
{
    // Pause the game
    StateMachine.Enter<PauseState>();
}
```

4. **Listen for state changes** (optional)
```csharp
_messageHub.Subscribe<StateEnteredEvent>(evt =>
{
    if (evt.StateType == typeof(PauseState))
    {
        // Do something when paused
    }
});
```

---

### Task 3: Create a New Service

**Goal:** Add game logic as a reusable service

**Steps:**

1. **Create interface** (in same folder or `Core/`)
```csharp
namespace HeistNSeek.Core
{
    public interface IScoreService
    {
        int GetScore();
        void AddScore(int points);
    }
}
```

2. **Create implementation**
```csharp
namespace HeistNSeek.Core
{
    public class ScoreService : IScoreService
    {
        private readonly IMessageHub _messageHub;
        private int _score = 0;

        public ScoreService(IMessageHub messageHub)
        {
            _messageHub = messageHub;
        }

        public int GetScore() => _score;

        public void AddScore(int points)
        {
            _score += points;
            _messageHub.Publish(new ScoreChangedEvent { NewScore = _score });
        }
    }
}
```

3. **Register in appropriate scope**

   **Global service (entire game):**
   ```csharp
   // In BootstrapLifetimeScope.Configure()
   builder.Register<IScoreService, ScoreService>(Lifetime.Singleton);
   ```

   **Scene-specific service:**
   ```csharp
   // In GameplayLifetimeScope.Configure()
   builder.Register<IScoreService, ScoreService>(Lifetime.Singleton);
   ```

4. **Inject and use**
```csharp
public class GameController
{
    private readonly IScoreService _scoreService;

    public GameController(IScoreService scoreService)
    {
        _scoreService = scoreService;
    }

    public void PlayerKilledEnemy()
    {
        _scoreService.AddScore(100);
    }
}
```

---

### Task 4: Handle Player Input

**Goal:** Capture and process player input

**Steps:**

1. **Input is handled by FirstPersonInputService** (`Core/Player/FirstPersonInputService.cs`)
   - Uses Unity Input System
   - Maps to player actions

2. **Create event for input action** (if not exists)
```csharp
// In Events/
public class InteractEvent
{
    public Collider TargetCollider { get; set; }
}
```

3. **Detect input in appropriate system**
```csharp
public class PlayerController : NetworkBehaviour
{
    private void Update()
    {
        if (IsOwner && Input.GetKeyDown(KeyCode.E))
        {
            _messageHub.Publish(new InteractEvent { /* ... */ });
        }
    }
}
```

4. **Listen for input events**
```csharp
public class InteractableObject : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub.SubscribeSafe<InteractEvent>(this, OnInteract);
    }

    private void OnInteract(InteractEvent evt)
    {
        if (evt.TargetCollider == GetComponent<Collider>())
        {
            // Handle interaction
        }
    }
}
```

---

### Task 5: Add UI System

**Goal:** Create UI that responds to game events

**Steps:**

1. **Create UI Controller**
```csharp
namespace HeistNSeek.Core.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] private Image healthBar;
        [Inject] private IMessageHub _messageHub;

        [Inject]
        public void Init(IMessageHub messageHub)
        {
            _messageHub.SubscribeSafe<HealthChangedEvent>(this, OnHealthChanged);
        }

        private void OnHealthChanged(HealthChangedEvent evt)
        {
            healthBar.fillAmount = evt.NewHealth / evt.MaxHealth;
        }

        private void OnDestroy()
        {
            // Auto unsubscribed via SubscribeSafe
        }
    }
}
```

2. **Create UI prefab**
   - Add Canvas in scene
   - Add Image component for health bar
   - Attach HealthBarUI script
   - Assign Image in inspector

3. **Register in GameplayLifetimeScope** (if auto-instantiating)
```csharp
builder.RegisterComponent<HealthBarUI>(healthBarInstance);
```

---

### Task 6: Network Behavior (Multiplayer)

**Goal:** Sync behavior across network

**Steps:**

1. **Extend NetworkBehaviour**
```csharp
public class PlayerWeapon : NetworkBehaviour
{
    [SerializeField] private int ammunition = 30;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
        {
            // Non-owners can't fire
            return;
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void FireRpc()
    {
        // This runs on all clients + host
        PlayFireAnimation();
    }

    public void Fire()
    {
        if (IsOwner)
        {
            FireRpc();
        }
    }
}
```

2. **Important patterns:**

   **Owner-only logic:**
   ```csharp
   if (!IsOwner) return;
   // Only owner executes this
   ```

   **Broadcast to all:**
   ```csharp
   [Rpc(SendTo.ClientsAndHost)]
   public void SyncedMethod() { }
   ```

   **Server-only logic:**
   ```csharp
   if (!IsServer) return;
   // Only server executes this
   ```

---

### Task 7: Subscribe to Game Events

**Goal:** Listen for game events and react

**Steps:**

1. **In a service (constructor injection)**
```csharp
public class SoundManager
{
    private readonly IMessageHub _messageHub;

    public SoundManager(IMessageHub messageHub)
    {
        _messageHub = messageHub;
        _messageHub.Subscribe<BalanceLostEvent>(OnBalanceLost);
    }

    private void OnBalanceLost(BalanceLostEvent evt)
    {
        AudioSource.PlayClipAtPoint(fallSound, player.transform.position);
    }
}
```

2. **In a MonoBehaviour (with safe disposal)**
```csharp
public class EffectSystem : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub.SubscribeSafe<BalanceLostEvent>(this, OnBalanceLost);
    }

    private void OnBalanceLost(BalanceLostEvent evt)
    {
        SpawnRagdollEffect(transform.position);
    }
}
```

---

### Task 8: Add to Inventory System

**Goal:** Add new item type or change inventory behavior

**Steps:**

1. **Create ItemDataSO ScriptableObject**
```
Right-click in Assets/SO/
→ Create → HeistNSeek → Item Data
```

2. **Configure in Inspector**
   - Item Name: "Health Pack"
   - Weight: 0.5
   - Is Stackable: true
   - Max Stack Size: 10

3. **Add pickup to scene**
```csharp
public class HealthPickup : MonoBehaviour
{
    [SerializeField] private ItemDataSO healthItem;

    private void OnTriggerEnter(Collider other)
    {
        var inventory = other.GetComponent<PlayerController>()
            ?.GetComponent<SessionInventory>();

        if (inventory != null)
        {
            inventory.AddItem(healthItem, 1);
            Destroy(gameObject);
        }
    }
}
```

4. **Listen for item events**
```csharp
public class InventoryUI : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    [Inject]
    public void Init(IMessageHub messageHub)
    {
        _messageHub.SubscribeSafe<ItemAddedEvent>(this, OnItemAdded);
        _messageHub.SubscribeSafe<ItemRemovedEvent>(this, OnItemRemoved);
    }

    private void OnItemAdded(ItemAddedEvent evt)
    {
        UpdateInventoryDisplay();
    }
}
```

---

## Common Patterns

### Pattern 1: Message Publishing

```csharp
// Publish an event
_messageHub.Publish(new MyEvent { Data = value });

// All subscribers receive event immediately
// No return value, no blocking
```

### Pattern 2: Event Subscription

```csharp
// Constructor injection (services)
public class MyService
{
    public MyService(IMessageHub messageHub)
    {
        messageHub.Subscribe<MyEvent>(OnMyEvent);
    }

    private void OnMyEvent(MyEvent evt) { }
}

// Safe subscription (MonoBehaviours)
[Inject]
public void Init(IMessageHub messageHub)
{
    messageHub.SubscribeSafe<MyEvent>(this, OnMyEvent);
}
```

### Pattern 3: Dependency Injection

```csharp
// Register service
builder.Register<IMyService, MyService>(Lifetime.Singleton);

// Inject in service
public class Consumer
{
    public Consumer(IMyService myService)
    {
        // Use myService
    }
}

// Inject in MonoBehaviour
public class MonoConsumer : MonoBehaviour
{
    [Inject] private IMyService _myService;

    [Inject]
    public void Init(IMyService myService)
    {
        _myService = myService;
    }
}
```

### Pattern 4: State Machine Transitions

```csharp
// Enter state
StateMachine.Enter<GameplayState>();

// Enter state with data
StateMachine.Enter<LoadingState>(sceneIndex);

// Access payload in state
public override void Enter(object payload = null)
{
    int sceneIndex = (int)payload;
    LoadScene(sceneIndex);
}
```

### Pattern 5: NetworkBehaviour

```csharp
public class NetworkLogic : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            // Owner-specific setup
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SyncedRpc()
    {
        // Runs on all clients
    }

    public void TriggerSync()
    {
        if (IsOwner)
        {
            SyncedRpc();
        }
    }
}
```

---

## Testing & Debugging

### Test Scenes Setup

```csharp
// Always start from Bootstrap scene
// Bootstrap → Main (additive load)
// Never run Main scene directly
```

### Debug Logging

```csharp
// Standard logging
Debug.Log("[SystemName] Message");
Debug.LogWarning("[SystemName] Warning");
Debug.LogError("[SystemName] Error");

// Conditional logging
if (Debug.isDebugBuild)
{
    Debug.Log("[Performance] Detailed metrics");
}
```

### Inspecting State Machine

```csharp
// In any system with GameStateMachine injected
public GameStateMachine StateMachine { get; }

// Check active state
var activeState = StateMachine.ActiveState;
Debug.Log($"Current state: {activeState.GetType().Name}");
```

### Inspecting Inventory

```csharp
// In systems with SessionInventory injected
public SessionInventory Inventory { get; }

// Check contents
foreach (var item in Inventory.Items)
{
    Debug.Log($"{item.ItemData.itemName}: {item.CurrentStackCount}");
}

// Get counts
int totalItems = Inventory.TotalItemCount;
float totalWeight = Inventory.TotalWeight;
```

---

## Code Organization Tips

### File Structure
```
Your Feature/
├── MyFeature.cs                  # Main component
├── IMyFeatureService.cs          # Interface
├── MyFeatureService.cs           # Implementation
├── MyFeatureEvent.cs             # Events
└── Models/
    └── MyFeatureData.cs          # Data classes
```

### Namespace Structure
```csharp
namespace HeistNSeek.Core.YourFeature
{
    public class YourClass { }
}
```

### Dependency Flow
```
1. Request dependency in constructor
2. VContainer provides it
3. Use through interface
4. Never use GetComponent or singletons
5. Never use new for managed classes
```

---

## Performance Considerations

### Avoid
```csharp
// ❌ Creating objects frequently
new Vector3(x, y, z);  // OK for simple types
Instantiate(prefab);   // OK for actual objects
// But avoid in Update/LateUpdate

// ❌ Expensive operations in Update
GetComponent<>() in Update  // Cache in Start
Physics.OverlapSphere() every frame  // Cache or batch
foreach on large lists repeatedly
```

### Prefer
```csharp
// ✅ Cache references
private Collider _collider;
private void Start() { _collider = GetComponent<Collider>(); }

// ✅ Use object pooling
ObjectPool.GetOrCreate(prefab);

// ✅ Batch operations
_items.ForEach(item => item.Update());  // Once per frame

// ✅ Use coroutines for delayed logic
CoroutineHelper.RunCoroutine(DelayedAction());
```

---

## Common Mistakes & Solutions

| Mistake | Issue | Solution |
|---------|-------|----------|
| Using `new` for services | Not DI managed, singleton antipattern | Register in LifetimeScope, inject |
| Subscribing without unsubscribe | Memory leak in MonoBehaviours | Use `SubscribeSafe` or unsubscribe in OnDestroy |
| Running non-Owner code | Network desync, phantom inputs | Check `IsOwner` before input/physics |
| Direct scene load | Breaks DI setup | Use `LoadingState` or `SceneLoader` |
| Modifying `Helpers/` folder | Breaks shared utilities | Create own utilities instead |
| Using singletons | Lifetime management issues | Use VContainer scopes |
| Accessing `GetComponent<>()` before Start | Null reference if not initialized | Cache in Start, not Awake |
| Throwing from subscribers | Breaks event loop, crashes game | Handle exceptions in subscribers |

---

## References

- **Project Root:** `CLAUDE.md` - Project-specific guidelines
- **Architecture:** `Docs/01_ARCHITECTURE_OVERVIEW.md`
- **Systems:** `Docs/02_KEY_SYSTEMS.md`
- **VContainer Docs:** https://vcontainer.hadashikick.jp/
- **Netcode Docs:** https://docs-multiplayer.unity3d.com/
- **MessageHub:** Easy.MessageHub NuGet package

---

## Quick Checklist for New Features

- [ ] Read and understand the feature requirement
- [ ] Check if similar feature exists (DRY principle)
- [ ] Plan data flow and event triggers
- [ ] Create necessary events in `Events/`
- [ ] Create service/interface if needed
- [ ] Register in appropriate LifetimeScope
- [ ] Test with Bootstrap → Main flow
- [ ] Add event subscribers (UI, audio, etc.)
- [ ] Test in multiplayer if networking required
- [ ] Document new systems in comments
- [ ] Test cleanup on scene unload

---

## Getting Help

1. **Check documentation** - Read relevant docs first
2. **Search codebase** - Find similar implementations
3. **Check CLAUDE.md** - Project-specific guidelines
4. **Inspect existing code** - Learn from patterns
5. **Use Debug.Log** - Trace execution flow
6. **Test incrementally** - Build and test frequently

