# Session Inventory Setup Instructions

This document provides step-by-step instructions for setting up the Session Inventory system in Unity Editor.

## Overview

The Session Inventory system allows players to:
- Pick up items from the scene into their backpack
- Items are stacked when possible
- Items remain in the scene (disabled visually) after pickup
- Items are dropped and scattered when the player loses balance
- Drop amount depends on impact speed (harder fall = more items dropped)

## Prerequisites

- Unity 6 (6000.2.10f1)
- VContainer is installed and configured
- Bootstrap and Main scenes are set up with LifetimeScopes
- Player has the `PlayerController` component attached

## Step 1: Create Item Data Assets

Item data is stored as ScriptableObjects. You need to create ItemData assets for each item type.

### Creating an ItemData Asset:

1. In the Unity Editor, right-click in the Project window
2. Select **Create → Robbery → Item**
3. Name your item (e.g., "GoldBar", "Diamond", "Cash")
4. Select the created asset and configure it in the Inspector:

   - **Identity:**
     - `Item Name`: Display name for the item
     - `Description`: Description of the item
     - `Icon`: Sprite for inventory UI (optional for now)
     - `Prefab`: 3D model of the item (REQUIRED - see Step 2)

   - **Properties:**
     - `Item Type`: Loot, Tool, Consumable, or KeyItem (NOT Weapon - those use WeaponData)
     - `Weight`: Affects carrying capacity (for future use)
     - `Price`: Value of the item
     - `Rarity`: Common, Uncommon, Rare, or Legendary

   - **Gameplay:**
     - `Is Stackable`: Check if multiple items should stack together
     - `Max Stack Size`: Maximum number that can stack (e.g., 99 for cash)
     - `Can Be Sold`: Whether item can be sold (for future use)

## Step 2: Create Item Prefabs

Each item needs a prefab that exists in the scene.

### Creating an Item Prefab:

1. Create a 3D GameObject for your item:
   - Right-click in Hierarchy → **3D Object → Cube** (or any shape)
   - Name it appropriately (e.g., "GoldBar_Prefab")

2. Add required components to the prefab:

   **A. Add Colliders (IMPORTANT - Two colliders needed):**

   Items need **TWO colliders** to work properly:

   1. **Physics Collider (Main)** - MUST be set as **NOT a trigger**:
      - This prevents items from falling through the ground
      - Keep **"Is Trigger"** UNCHECKED
      - Size it to fit your item's visual bounds
      - Can be BoxCollider, SphereCollider, CapsuleCollider, etc.

   2. **Pickup Trigger (For detection)** - MUST be set as **a trigger**:
      - This detects when the player is near the item
      - Check **"Is Trigger"** checkbox
      - Should be slightly larger than the physics collider (e.g., 1.2x size)
      - Can be on the same GameObject or a child object

   **Easy Setup Options:**

   - **Option 1: Let the script create it automatically**
     - Just add ONE non-trigger collider (physics only)
     - The ItemPickup script will automatically create the pickup trigger at runtime

   - **Option 2: Manual setup**
     - Add a BoxCollider/SphereCollider (Is Trigger: UNCHECKED)
     - Create a child GameObject named "PickupTrigger"
     - Add a collider to the child (Is Trigger: CHECKED, slightly larger)
     - Assign this trigger to the ItemPickup component's "Pickup Trigger" field

   **B. Add the ItemPickup component:**
   - Click **Add Component** → search for **"ItemPickup"**
   - Configure the ItemPickup settings:
     - `Item Data`: Drag your ItemData asset here (created in Step 1)
     - `Amount`: How many of this item to add when picked up (default: 1)
     - `Player Tag`: Should be "Player" (default)
     - `Auto Pickup`: Check if item should be picked up automatically on contact
     - `Pickup Key`: If Auto Pickup is unchecked, set the key to press (default: E)
     - `Visual Root`: (Optional) If your item has a specific visual parent, assign it here. Otherwise, leave empty.

   **C. Add a Rigidbody (REQUIRED):**
   - Click **Add Component** → **Rigidbody**
   - This is required for physics simulation and drop mechanics
   - **Settings:**
     - Mass: 0.5 - 2.0 (adjust based on item size/type)
     - Drag: 0.5 (prevents excessive sliding)
     - Angular Drag: 0.5 (prevents excessive spinning)
     - Use Gravity: CHECK (items should fall naturally)
     - Is Kinematic: UNCHECK (needed for physics to work)
   - The script will automatically manage kinematic state during pickup/drop

   **D. Add visual elements:**
   - Add a **MeshRenderer** and **MeshFilter** for 3D models
   - Or use a child GameObject with the 3D model
   - Customize materials, textures, etc.

3. Create a prefab:
   - Drag the GameObject from Hierarchy into the Project window to create a prefab
   - You can now delete the original from the scene (or keep it for testing)

4. Link the prefab to the ItemData:
   - Select your ItemData asset (from Step 1)
   - In the Inspector, find the **Prefab** field
   - Drag your newly created prefab into this field

## Step 3: Place Items in the Scene

1. Drag item prefabs from the Project window into your scene (Main.unity)
2. Position them where players can find them
3. The items are now ready to be picked up!

**Important Notes:**
- Items placed in the scene will be disabled visually when picked up, but remain in the scene
- When dropped, they will re-enable at the drop location
- No additional setup is needed for scene-placed items

## Step 4: Verify Player Setup

The player needs to have the correct tag and components for pickup to work.

1. Select your Player GameObject in the scene
2. In the Inspector, verify:
   - **Tag** is set to "Player" (or match the tag in ItemPickup settings)
   - **PlayerController** component is attached
   - The PlayerController should have a **Collider** component (not a trigger)

## Step 5: Test the System

### Testing Pickup:

1. Enter Play Mode
2. Move the player close to an item
3. If `Auto Pickup` is enabled, the item should be picked up automatically
4. If `Auto Pickup` is disabled, press the configured key (default: E) when near the item
5. Check the Console for log messages:
   ```
   [SessionInventory] Added 1x 'ItemName'. Total: 1
   [ItemPickup] Picked up 1x 'ItemName'
   ```

### Testing Item Drop on Balance Lost:

1. While holding items, make the player lose balance
2. This can be done by:
   - Running into a wall at high speed
   - Triggering a high-speed collision with the CollisionDetectors
3. Items should scatter on the floor around the player
4. Check the Console for log messages:
   ```
   [ItemDropper] Dropped X items due to impact speed Y
   [ItemDropper] Spawned ItemName at (position)
   ```

### Testing Item Re-Pickup:

1. After items are dropped, move the player near them
2. Items should be pickable again
3. They will be added back to the inventory

## Step 6: Inventory Events (For Future UI Integration)

The inventory system publishes events via `IMessageHub` that you can subscribe to:

### Available Events:

- **ItemAddedEvent**: Published when an item is added to inventory
  - `ItemData ItemData`: The item that was added
  - `int Amount`: How many were added
  - `int TotalCount`: Total count of this item in inventory

- **ItemRemovedEvent**: Published when an item is removed
  - `ItemData ItemData`: The item that was removed
  - `int Amount`: How many were removed
  - `int RemainingCount`: How many remain

- **ItemsDroppedEvent**: Published when items are dropped due to balance loss
  - `List<ItemData> DroppedItems`: List of items that were dropped
  - `Vector3 DropPosition`: Where items were dropped
  - `float ImpactSpeed`: The impact speed that caused the drop

### Example: Subscribing to Events

```csharp
using Easy.MessageHub;
using HeistNSeek.Events;
using UnityEngine;
using VContainer;

public class InventoryUI : MonoBehaviour
{
    [Inject] private IMessageHub _messageHub;

    private void Start()
    {
        _messageHub.Subscribe<ItemAddedEvent>(OnItemAdded);
        _messageHub.Subscribe<ItemRemovedEvent>(OnItemRemoved);
        _messageHub.Subscribe<ItemsDroppedEvent>(OnItemsDropped);
    }

    private void OnItemAdded(ItemAddedEvent evt)
    {
        Debug.Log($"Item added: {evt.ItemData.itemName} x{evt.Amount}. Total: {evt.TotalCount}");
        // Update UI here
    }

    private void OnItemRemoved(ItemRemovedEvent evt)
    {
        Debug.Log($"Item removed: {evt.ItemData.itemName} x{evt.Amount}. Remaining: {evt.RemainingCount}");
        // Update UI here
    }

    private void OnItemsDropped(ItemsDroppedEvent evt)
    {
        Debug.Log($"Dropped {evt.DroppedItems.Count} items at {evt.DropPosition}");
        // Update UI here
    }
}
```

## Troubleshooting

### Items falling through the ground:

**This is the most common issue!** Items need TWO colliders to work properly.

1. **Check Main Collider**: Ensure there's a collider with "Is Trigger" **UNCHECKED** (physics collider)
   - This collider prevents the item from falling through the ground
   - Without this, the item will fall forever!

2. **Check Pickup Trigger**: Ensure there's a trigger collider for pickup detection
   - Can be on the same GameObject or a child
   - "Is Trigger" should be **CHECKED**
   - If missing, the script will create one automatically

3. **Check Rigidbody**: Ensure the Rigidbody is configured correctly
   - "Use Gravity" should be CHECKED
   - "Is Kinematic" should be UNCHECKED (for scene-placed items)

4. **Quick Fix**: If items are falling, add a non-trigger BoxCollider to the item GameObject

### Items not being picked up:

1. **Check Player Tag**: Ensure the player GameObject has the "Player" tag
2. **Check Pickup Trigger**: Ensure the item has a trigger collider (Is Trigger = true)
3. **Check ItemData**: Ensure the ItemPickup component has a valid ItemData assigned
4. **Check Console**: Look for warning messages about missing dependencies or collider setup
5. **Check LifetimeScope**: Ensure GameplayLifetimeScope is in the scene and configured
6. **Check Player Collider**: The player needs a collider (NOT a trigger) to detect the pickup trigger

### Items not dropping when balance lost:

1. **Check Impact Speed**: The default minimum impact speed is 5.0. Try a harder collision.
2. **Check BalanceLostEvent**: Ensure the PlayerController is publishing BalanceLostEvent
3. **Check Inventory**: Ensure you have items in the inventory before testing
4. **Check Console**: Look for log messages from ItemDropper

### Items spawning but not visible:

1. **Check Prefab**: Ensure the ItemData has a valid prefab assigned
2. **Check Visual Root**: Ensure the prefab has visual components (MeshRenderer, etc.)
3. **Check Position**: Items spawn slightly above the ground - check Y position

### Items not stacking:

1. **Check ItemData**: Ensure "Is Stackable" is checked
2. **Check Max Stack Size**: Ensure it's greater than 1
3. **Check Item Type**: Ensure both items are using the exact same ItemData asset

## Configuration Options

### Adjusting Drop Behavior:

The drop system is configured in `ItemDropper.cs` with these settings:

```csharp
private readonly float _minImpactForDrop = 5f;  // Minimum impact speed to drop items
private readonly float _maxImpactSpeed = 20f;   // Impact speed for 100% drop
private readonly float _scatterRadius = 3f;     // Radius for item scatter
private readonly float _scatterForce = 5f;      // Force applied to dropped items
```

To adjust these values:
1. Open `ItemDropper.cs` in your code editor
2. Modify the values as needed
3. Save and return to Unity

**Drop Percentage Calculation:**
- Impact speed < 5: No items dropped
- Impact speed = 5: ~10% of items dropped
- Impact speed = 20 or higher: 100% of items dropped
- Linear interpolation between these values

## Next Steps

1. **Create UI for Inventory**: Display current items, counts, and weight
2. **Add Item Usage**: Allow items to be used (consumables, tools, etc.)
3. **Add Carrying Capacity**: Limit inventory by weight or item count
4. **Add Item Selling**: Implement a shop system to sell items
5. **Add Inventory Persistence**: Save inventory between sessions

---

**Files Created:**
- `SessionInventory.cs` - Main inventory management service
- `InventoryItem.cs` - Item wrapper with stack count
- `ItemPickup.cs` - MonoBehaviour for scene items
- `ItemDropper.cs` - Handles dropping items on balance lost
- `ItemAddedEvent.cs` - Event published when item added
- `ItemRemovedEvent.cs` - Event published when item removed
- `ItemsDroppedEvent.cs` - Event published when items dropped

**Files Modified:**
- `GameplayLifetimeScope.cs` - Registered SessionInventory and ItemDropper services

**Dependencies:**
- VContainer (for dependency injection)
- Easy.MessageHub (for event publishing)
- PlayerController (for player reference and balance events)


** CONTEXT FOR CONITNUATION: **
 Session Inventory System - Context Summary

  What Was Implemented

  A complete session inventory system for HeistNSeek that allows players to pick up items into a backpack, with
  items dropping and scattering when the player loses balance.

  ---
  File Structure Created

  Core Scripts (Assets/Scripts/Core/Inventory/SessionInventory/)

  1. InventoryItem.cs
    - Wrapper class for ItemData with stack count tracking
    - Handles stacking logic (AddToStack, RemoveFromStack, CanStackWith)
  2. SessionInventory.cs
    - Main inventory service (registered as Singleton in GameplayLifetimeScope)
    - Methods: AddItem(), RemoveItem(), GetItemCount(), HasItem(), DropItems()
    - Filters out weapons (only holds non-weapon items)
    - Publishes events via IMessageHub
  3. ItemPickup.cs
    - MonoBehaviour attached to scene items
    - Dual-collider system: Requires physics collider (non-trigger) + pickup trigger (trigger)
    - Auto-creates pickup trigger if missing
    - Disables items when picked up, re-enables when dropped
    - Can be injected via VContainer or uses static fallback
  4. ItemDropper.cs
    - Service that listens to BalanceLostEvent (registered as Singleton)
    - Calculates drop percentage based on impact speed:
        - Impact < 5: No drop
      - Impact 5-20: 10%-100% linear scaling
      - Impact > 20: 100% drop
    - Spawns items with physics scatter effect
    - Maintains static reference to SessionInventory for ItemPickup access

  Events (Assets/Scripts/Events/)

  1. ItemAddedEvent.cs - Published when item added to inventory
  2. ItemRemovedEvent.cs - Published when item removed
  3. ItemsDroppedEvent.cs - Published when items dropped due to balance loss

  Documentation

  SETUP_INSTRUCTIONS.md - Complete setup guide with troubleshooting

  ---
  System Architecture

  Dependency Injection (VContainer)

  GameplayLifetimeScope.cs registers:
  builder.Register<SessionInventory>(Lifetime.Singleton);
  builder.Register<ItemDropper>(Lifetime.Singleton);
  builder.Register<ItemPickup>(Lifetime.Transient);

  Dependencies:
  - SessionInventory → IMessageHub
  - ItemDropper → SessionInventory, IMessageHub, PlayerController
  - ItemPickup → SessionInventory (injected or via static reference)

  Event Flow

  Player collision → BalanceLostEvent published
                  ↓
              ItemDropper listens
                  ↓
      Calculates drop % based on impact
                  ↓
      SessionInventory.DropItems() called
                  ↓
      Items spawned with scatter physics
                  ↓
      ItemsDroppedEvent published

  Pickup Flow

  Player enters trigger → ItemPickup.OnTriggerEnter()
                       ↓
                TryPickup() called
                       ↓
      SessionInventory.AddItem() called
                       ↓
          Item visuals disabled
          Rigidbody made kinematic
          Colliders disabled
                       ↓
      ItemAddedEvent published

  ---
  Key Technical Details

  Dual-Collider System (CRITICAL)

  Items need TWO colliders:
  1. Physics collider (Is Trigger = UNCHECKED) - prevents falling through ground
  2. Pickup trigger (Is Trigger = CHECKED) - detects player proximity

  ItemPickup automatically creates pickup trigger if missing.

  Item States

  Active (in scene):
  - Visuals: Active
  - Rigidbody: Non-kinematic, detectCollisions = true
  - Colliders: Enabled

  Picked up:
  - Visuals: Disabled (GameObject stays active)
  - Rigidbody: Kinematic, detectCollisions = false
  - Colliders: Disabled

  Dropped:
  - Same as Active + scatter force applied

  Existing Models (from exploration)

  ItemData.cs (ScriptableObject) - Already exists:
  - Fields: itemName, description, icon, prefab, itemType, weight, price, rarity
  - Fields: isStackable, maxStackSize, canBeSold
  - Create via: Right-click → Create → Robbery → Item

  WeaponData.cs - Extends ItemData, excluded from SessionInventory

  ---
  Current State - Ready to Use

  ✅ Code Complete

  - All scripts created and integrated
  - DI registration configured
  - Event system connected
  - Balance system integration complete

  ⚠️ Requires Unity Editor Setup

  You need to do in Unity:

  1. Create ItemData assets (ScriptableObjects)
    - Right-click → Create → Robbery → Item
    - Configure properties (name, type, stackable, etc.)
  2. Create item prefabs:
  GameObject
  ├─ BoxCollider (Is Trigger: UNCHECKED) ← Add this!
  ├─ Rigidbody (Is Kinematic: UNCHECKED)
  ├─ MeshRenderer/Filter
  └─ ItemPickup component
  3. Link prefab to ItemData:
    - Assign prefab to ItemData's "Prefab" field
  4. Place items in Main scene:
    - Drag prefabs into scene
    - Position where you want them
  5. Verify player setup:
    - Tag = "Player"
    - Has PlayerController component
    - Has Collider (not trigger)

  ---
  Testing Checklist

  - Walk into item → should pick up automatically (if autoPickup = true)
  - Check Console for: [SessionInventory] Added 1x 'ItemName'
  - Lose balance at high speed → items should scatter
  - Check Console for: [ItemDropper] Dropped X items
  - Walk into dropped items → should pick up again
  - Items should NOT fall through ground

  ---
  Configuration Constants

  In ItemDropper.cs (line 22-25):
  private readonly float _minImpactForDrop = 5f;   // Min speed to drop
  private readonly float _maxImpactSpeed = 20f;     // Speed for 100% drop
  private readonly float _scatterRadius = 3f;       // Scatter radius
  private readonly float _scatterForce = 5f;        // Scatter force

  Adjust these if needed for gameplay balance.

  ---
  Integration Points

  Existing Systems Used:

  - PlayerController (Assets/Scripts/Core/Player/PlayerController.cs)
    - Uses CurrentBalanceInfo.IsBalanced
    - Publishes BalanceLostEvent with impact speed
  - CollisionDetector - Multiple on player for body parts
    - Each has configurable thresholds and multipliers
  - IMessageHub - Global singleton from BootstrapLifetimeScope
    - All inventory events published here

  Models Folder:

  - ItemData.cs - Base item ScriptableObject
  - WeaponData.cs - Weapon-specific (excluded from SessionInventory)
  - Enums.cs - ItemType, Rarity enums

  ---
  Next Steps (Future Enhancements)

  1. Create inventory UI:
    - Subscribe to ItemAddedEvent, ItemRemovedEvent
    - Display items, counts, icons
    - Show weight/capacity
  2. Add carrying capacity:
    - Limit by weight or item count
    - Slow player when overloaded
  3. Item usage system:
    - Use consumables
    - Tool interactions
  4. Save/load persistence:
    - Save inventory between sessions
    - Serialize item data
  5. Multiplayer sync:
    - Network item pickup/drop
    - Sync inventory state

  ---
  Common Issues & Solutions

  Items falling through ground:
  - Add NON-trigger collider to item prefab
  - ItemPickup will auto-create trigger

  Items not picking up:
  - Check player tag = "Player"
  - Check item has trigger collider
  - Check Console for warnings

  Items not dropping:
  - Need impact speed > 5.0
  - Check BalanceLostEvent is firing
  - Check you have items in inventory

  ---
  File Paths Reference

  Assets/Scripts/
  ├─ Core/
  │  ├─ Inventory/
  │  │  ├─ Models/
  │  │  │  ├─ ItemData.cs (existing)
  │  │  │  ├─ WeaponData.cs (existing)
  │  │  │  └─ Enums.cs (existing)
  │  │  └─ SessionInventory/
  │  │     ├─ SessionInventory.cs (NEW)
  │  │     ├─ InventoryItem.cs (NEW)
  │  │     ├─ ItemPickup.cs (NEW)
  │  │     ├─ ItemDropper.cs (NEW)
  │  │     └─ SETUP_INSTRUCTIONS.md (NEW)
  │  ├─ InjectionBase/
  │  │  └─ GameplayLifetimeScope.cs (MODIFIED)
  │  └─ Player/
  │     └─ PlayerController.cs (existing - used)
  └─ Events/
     ├─ ItemAddedEvent.cs (NEW)
     ├─ ItemRemovedEvent.cs (NEW)
     ├─ ItemsDroppedEvent.cs (NEW)
     └─ BalanceLostEvent.cs (existing - listened to)

  ---
  Quick Start Tomorrow

  1. Open Unity project
  2. Create 1-2 test ItemData assets
  3. Create matching prefabs with proper colliders
  4. Place in Main scene
  5. Hit Play and test pickup
  6. Test balance loss by running into walls

  Reference: SETUP_INSTRUCTIONS.md for detailed steps

  ---
  System Status: ✅ Fully implemented and ready for Unity Editor setup