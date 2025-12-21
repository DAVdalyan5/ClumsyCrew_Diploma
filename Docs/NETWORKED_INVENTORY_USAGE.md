# Networked Inventory Usage Guide

This guide explains how to use the networked inventory system in your game logic.

## Table of Contents

1. [ItemRegistry Overview](#itemregistry-overview)
2. [How to Use the Inventory](#how-to-use-the-inventory)
3. [Event Subscription Patterns](#event-subscription-patterns)
4. [Complete Examples](#complete-examples)
5. [API Reference](#api-reference)

---

## ItemRegistry Overview

### What is ItemRegistry?

`ItemRegistry` is a **global, centralized lookup table** that maps item IDs to `ItemDataSO` assets. It's used by the entire game—not per-player or per-scene.

**Location:** `Assets/Scripts/Core/Inventory/NetworkedInventory/ItemRegistry.cs`

### Why Do We Need It?

**Network Serialization Optimization**

Instead of sending entire `ItemDataSO` objects over the network (expensive), you send just the item ID (a string), and each client/server looks up the actual item data locally:

```
Server: Player picks up item with ID "HealthPotion"
  → Send only the string "HealthPotion" over network

Client receives:
  → Looks up "HealthPotion" in ItemRegistry
  → Gets full ItemDataSO with stats, visuals, effects, etc.
```

### How It Works

1. **Initialize once at game startup** with all available `ItemDataSO` assets
2. **Maps item names** (asset filenames) to their `ItemDataSO` objects
3. **Used everywhere**: inventory items, world items, UI, effects systems

Example:
```csharp
// Get item data by ID
ItemDataSO goldCoin = ItemRegistry.GetItemData("GoldCoin");

// Check if item exists
bool hasHealth = ItemRegistry.HasItem("HealthPotion");

// Get all registered items
var allItems = ItemRegistry.GetAllItems();
```

### Initialization

ItemRegistry is initialized by `ItemRegistryInitializer` at scene start. See [NETWORKED_INVENTORY_SETUP.md](NETWORKED_INVENTORY_SETUP.md) for setup instructions.

---

## How to Use the Inventory

There are **three main ways** to interact with the networked inventory:

### 1. MessageHub Events (Recommended)

Subscribe to inventory change events. These fire automatically when items are added/removed/dropped.

**Available Events:**
- `ItemAddedEvent` - When an item is added to inventory
- `ItemRemovedEvent` - When an item is removed from inventory
- `ItemsDroppedEvent` - When items are dropped (balance lost)

**Benefits:**
- Decoupled from inventory component
- Works in any service or MonoBehaviour
- Perfect for game logic and effects

### 2. Direct Callback

Subscribe to the `OnInventoryChanged` event on the player's `NetworkedPlayerInventory` component.

**Benefits:**
- Simple for UI updates
- Tightly coupled to inventory component
- Fires on any change

### 3. Direct API Queries

Call methods directly on `NetworkedPlayerInventory` to check inventory state.

**Benefits:**
- Synchronous queries
- No event subscription needed
- Good for conditional logic

---

## Event Subscription Patterns

### Pattern 1: Service-Based (Recommended for Game Logic)

Create a service that subscribes to inventory events and runs your custom logic:

```csharp
using Easy.MessageHub;
using HeistNSeek.Events;
using UnityEngine;

namespace HeistNSeek.Core.Gameplay
{
    /// <summary>
    /// Handles game effects when items are picked up or used.
    /// This is a plain C# service injected via VContainer.
    /// </summary>
    public interface IInventoryEffectsService
    {
        // Interface for dependency injection
    }

    public class InventoryEffectsService : IInventoryEffectsService
    {
        private readonly IMessageHub _messageHub;

        public InventoryEffectsService(IMessageHub messageHub)
        {
            _messageHub = messageHub;

            // Subscribe to inventory events
            _messageHub.Subscribe<ItemAddedEvent>(OnItemAdded);
            _messageHub.Subscribe<ItemRemovedEvent>(OnItemRemoved);
            _messageHub.Subscribe<ItemsDroppedEvent>(OnItemsDropped);
        }

        private void OnItemAdded(ItemAddedEvent evt)
        {
            // evt.ItemData - The ItemDataSO of the added item
            // evt.Amount - How many were added
            // evt.TotalCount - Total count of this item in inventory

            Debug.Log($"[InventoryEffects] Picked up {evt.Amount}x {evt.ItemData.itemName}");

            // Run your custom logic based on item type
            if (evt.ItemData.itemName == "HealthPotion")
            {
                // Apply health buff
                RestoreHealth(evt.Amount * 25); // 25 HP per potion
            }
            else if (evt.ItemData.itemName == "GoldCoin")
            {
                // Update score or currency
                AddCurrency(evt.Amount);
            }
            else if (evt.ItemData is WeaponDataSO weaponData)
            {
                // Handle weapon pickup
                OnWeaponPickedUp(weaponData);
            }
        }

        private void OnItemRemoved(ItemRemovedEvent evt)
        {
            // evt.ItemData - The ItemDataSO
            // evt.Amount - How many were removed
            // evt.RemainingCount - How many left in inventory

            Debug.Log($"[InventoryEffects] Used {evt.Amount}x {evt.ItemData.itemName}, {evt.RemainingCount} remaining");

            // Handle item usage effects
            if (evt.ItemData.itemName == "MagicScroll")
            {
                CastSpell(evt.ItemData);
            }
        }

        private void OnItemsDropped(ItemsDroppedEvent evt)
        {
            // evt.DropPosition - Where items were dropped
            // evt.ImpactSpeed - Force applied to dropped items

            Debug.Log($"[InventoryEffects] Items dropped at {evt.DropPosition} with force {evt.ImpactSpeed}");
            PlayDropSound(evt.DropPosition);
        }

        private void RestoreHealth(int amount) { /* ... */ }
        private void AddCurrency(int amount) { /* ... */ }
        private void OnWeaponPickedUp(WeaponDataSO weapon) { /* ... */ }
        private void CastSpell(ItemDataSO itemData) { /* ... */ }
        private void PlayDropSound(Vector3 position) { /* ... */ }
    }
}
```

**Register in GameplayLifetimeScope:**
```csharp
builder.Register<InventoryEffectsService>(Lifetime.Singleton).As<IInventoryEffectsService>();
```

### Pattern 2: MonoBehaviour UI Update

Subscribe to inventory changes for UI updates:

```csharp
using HeistNSeek.Core.Inventory.NetworkedInventory;
using UnityEngine;
using VContainer;

namespace HeistNSeek.UI
{
    public class InventoryPanel : MonoBehaviour
    {
        private NetworkedPlayerInventory _playerInventory;

        [Inject]
        public void Init(NetworkedPlayerInventory playerInventory)
        {
            _playerInventory = playerInventory;

            // Subscribe to inventory changes
            _playerInventory.OnInventoryChanged += RefreshInventoryDisplay;

            // Initial display
            RefreshInventoryDisplay();
        }

        private void OnDestroy()
        {
            if (_playerInventory != null)
            {
                _playerInventory.OnInventoryChanged -= RefreshInventoryDisplay;
            }
        }

        private void RefreshInventoryDisplay()
        {
            // Clear and rebuild UI
            ClearSlots();

            // Display all items
            foreach (var (itemId, amount) in _playerInventory.GetAllItems())
            {
                var itemData = ItemRegistry.GetItemData(itemId);
                if (itemData != null)
                {
                    DisplayInventorySlot(itemData, amount);
                }
            }
        }

        private void ClearSlots() { /* ... */ }
        private void DisplayInventorySlot(ItemDataSO item, int amount) { /* ... */ }
    }
}
```

### Pattern 3: Direct Query

Check inventory state directly without events:

```csharp
public class WeaponSystem
{
    private readonly NetworkedPlayerInventory _inventory;

    public WeaponSystem(NetworkedPlayerInventory inventory)
    {
        _inventory = inventory;
    }

    public void TryEquipWeapon(string weaponId)
    {
        // Check if player has this weapon
        if (_inventory.HasItem(weaponId, amount: 1))
        {
            int weaponCount = _inventory.GetItemCount(weaponId);
            var weaponData = ItemRegistry.GetItemData(weaponId);

            Debug.Log($"Equipping {weaponData.itemName} (have {weaponCount})");
            EquipWeapon(weaponData);
        }
        else
        {
            Debug.Log("Weapon not in inventory!");
        }
    }

    private void EquipWeapon(ItemDataSO weapon) { /* ... */ }
}
```

---

## Complete Examples

### Example 1: Health System with Potions

```csharp
public class HealthEffectsService : IHealthEffectsService
{
    private readonly IMessageHub _messageHub;
    private readonly IPlayerHealth _playerHealth;

    public HealthEffectsService(IMessageHub messageHub, IPlayerHealth playerHealth)
    {
        _messageHub = messageHub;
        _playerHealth = playerHealth;
        _messageHub.Subscribe<ItemAddedEvent>(OnItemPickedUp);
    }

    private void OnItemPickedUp(ItemAddedEvent evt)
    {
        if (evt.ItemData.itemName == "HealthPotion")
        {
            // Restore health when picked up
            int healAmount = 25;
            _playerHealth.Heal(healAmount);

            Debug.Log($"[Health] Restored {healAmount} HP");
        }
        else if (evt.ItemData.itemName == "MajorHealthPotion")
        {
            // Stronger potion
            _playerHealth.Heal(50);
        }
    }
}
```

### Example 2: Weapon Switching with Inventory

```csharp
public class WeaponManager : MonoBehaviour
{
    [SerializeField] private string equippedWeaponId;
    private NetworkedPlayerInventory _inventory;
    private WeaponVisuals _weaponVisuals;

    [Inject]
    public void Init(NetworkedPlayerInventory inventory, WeaponVisuals weaponVisuals)
    {
        _inventory = inventory;
        _weaponVisuals = weaponVisuals;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            CycleWeapons();
        }
    }

    private void CycleWeapons()
    {
        var weapons = new List<string>();

        // Collect all weapons in inventory
        foreach (var (itemId, amount) in _inventory.GetAllItems())
        {
            var itemData = ItemRegistry.GetItemData(itemId);
            if (itemData is WeaponDataSO)
            {
                weapons.Add(itemId);
            }
        }

        if (weapons.Count == 0)
        {
            Debug.Log("No weapons in inventory!");
            return;
        }

        // Find current weapon index
        int currentIndex = weapons.IndexOf(equippedWeaponId);
        int nextIndex = (currentIndex + 1) % weapons.Count;

        EquipWeapon(weapons[nextIndex]);
    }

    private void EquipWeapon(string weaponId)
    {
        equippedWeaponId = weaponId;
        var weaponData = ItemRegistry.GetItemData(weaponId) as WeaponDataSO;

        _weaponVisuals.SetWeapon(weaponData);
        Debug.Log($"[Weapons] Equipped {weaponData.itemName}");
    }
}
```

### Example 3: Crafting System

```csharp
public class CraftingService
{
    private readonly NetworkedPlayerInventory _inventory;
    private readonly IMessageHub _messageHub;

    public CraftingService(NetworkedPlayerInventory inventory, IMessageHub messageHub)
    {
        _inventory = inventory;
        _messageHub = messageHub;
    }

    public bool TryCraft(string recipeId, CraftingRecipe recipe)
    {
        // Check if player has all ingredients
        foreach (var (ingredientId, amount) in recipe.Ingredients)
        {
            if (!_inventory.HasItem(ingredientId, amount))
            {
                Debug.Log($"Missing {amount}x {ingredientId}");
                return false;
            }
        }

        // Remove ingredients
        foreach (var (ingredientId, amount) in recipe.Ingredients)
        {
            _inventory.RequestRemoveItem(ingredientId, amount);
        }

        // Add result
        foreach (var (resultId, amount) in recipe.Results)
        {
            _inventory.RequestAddItem(resultId, amount);
        }

        Debug.Log($"[Crafting] Crafted {recipeId}");
        return true;
    }
}
```

---

## API Reference

### ItemRegistry (Static)

```csharp
// Initialize at startup
ItemRegistry.Initialize(IEnumerable<ItemDataSO> allItems);
ItemRegistry.InitializeFromResources(string resourcePath = "Items");

// Query
ItemDataSO GetItemData(string itemId);
bool HasItem(string itemId);
IEnumerable<ItemDataSO> GetAllItems();
string GetItemId(ItemDataSO item);

// Runtime registration
void RegisterItem(ItemDataSO item);
void Clear();
```

### NetworkedPlayerInventory

```csharp
// Request item operations (work on client or server)
void RequestAddItem(string itemId, int amount = 1);
void RequestRemoveItem(string itemId, int amount = 1);
void RequestDropAllItems(Vector3 dropPosition, float impactSpeed);

// Query inventory (client-side)
int GetItemCount(string itemId);
bool HasItem(string itemId, int amount = 1);
IEnumerable<(string itemId, int amount)> GetAllItems();

// Properties
int SlotCount { get; }
int MaxSlots { get; }

// Events
event Action OnInventoryChanged;
```

### Event Classes

#### ItemAddedEvent
```csharp
public class ItemAddedEvent
{
    public ItemDataSO ItemData { get; }      // The item that was added
    public int Amount { get; }               // How many were added
    public int TotalCount { get; }          // Total count of this item now
}
```

#### ItemRemovedEvent
```csharp
public class ItemRemovedEvent
{
    public ItemDataSO ItemData { get; }      // The item that was removed
    public int Amount { get; }               // How many were removed
    public int RemainingCount { get; }      // How many left after removal
}
```

#### ItemsDroppedEvent
```csharp
public class ItemsDroppedEvent
{
    public List<ItemDataSO> Items { get; }   // Items that were dropped
    public Vector3 DropPosition { get; }     // Where they were dropped
    public float ImpactSpeed { get; }        // Force applied to dropped items
}
```

---

## Best Practices

### ✅ Do:
- **Use MessageHub events** for decoupled game logic
- **Subscribe in services** instead of MonoBehaviours when possible
- **Check inventory** before removing items
- **Use ItemRegistry** to get item data by ID
- **Publish custom events** when custom item logic happens

### ❌ Don't:
- **Don't modify `_syncedInventory` directly** - use Request methods
- **Don't assume client-side state is authoritative** - server is authoritative
- **Don't cache ItemDataSO references** - look them up via ItemRegistry
- **Don't forget to unsubscribe** from events in OnDestroy

### Network Authority

**Important:** Only the server can modify inventory:
```csharp
// On client, these send RPCs to server
_inventory.RequestAddItem("Gold", 10);

// On server, these execute immediately
_inventory.RequestAddItem("Gold", 10);
```

The server is authoritative and syncs state to all clients. Clients never directly modify their own inventory.

---

## Troubleshooting

### "Item not found in registry" Error
- Check ItemRegistryInitializer has all items assigned
- Verify ItemDataSO asset names match expected IDs
- Restart play mode after adding new items

### Inventory Changes Not Showing
- Verify MessageHub subscription is active
- Check that `OnInventoryChanged` event is being fired
- Ensure your system is receiving events (check logs)

### Items Not Adding to Inventory
- Check server-side errors in logs
- Verify player is connected and owned by server
- Check inventory isn't at max capacity

---

## Related Documentation

- [NETWORKED_INVENTORY_SETUP.md](NETWORKED_INVENTORY_SETUP.md) - Setup and prefab configuration
- [Project Architecture (CLAUDE.md)](../CLAUDE.md) - VContainer DI and service patterns
