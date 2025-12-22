# Networked Inventory System - Editor Setup Guide

This guide explains how to set up the networked inventory system so that:
- Each player has their own inventory
- Items dropped by one player appear for all players
- Items picked up by one player disappear for all players
- All inventory state is server-authoritative

## Overview

The system consists of:
1. **NetworkedPlayerInventory** - Per-player inventory component (on player prefab)
2. **NetworkedItemDropper** - Drops items when player loses balance (on player prefab)
3. **NetworkedItemSpawnManager** - Server singleton that spawns/despawns world items
4. **NetworkedItemPickup** - World item that can be picked up (on item prefab)
5. **ItemRegistry** - Maps item IDs to ItemDataSO (for network serialization)
6. **ItemRegistryInitializer** - Initializes the registry at scene start

---

## Step 1: Create the Networked Item Prefab

This prefab will be used for ALL items spawned in the world.

### 1.1 Create a new empty GameObject
1. In Unity, go to **GameObject > Create Empty**
2. Name it `NetworkedWorldItem`

### 1.2 Add Required Components
Add these components to the GameObject:

1. **NetworkObject** (required for networking)
   - Component > Netcode > NetworkObject

2. **NetworkedItemPickup** (our script)
   - Component > Scripts > HeistNSeek.Core.Inventory.NetworkedInventory > NetworkedItemPickup

3. **Rigidbody** (for physics)
   - Use Gravity: ✓
   - Is Kinematic: ✗
   - Collision Detection: Continuous (recommended)

4. **Collider** (for physics - NOT trigger)
   - Add a **Box Collider** or **Sphere Collider**
   - Is Trigger: ✗ (unchecked - this is for physics collision)

5. **MeshFilter** and **MeshRenderer** (for visuals)
   - Add default cube mesh or leave empty (will be replaced at runtime)

### 1.3 Create Pickup Trigger (Child Object)
1. Create a child GameObject: Right-click on `NetworkedWorldItem` > Create Empty
2. Name it `PickupTrigger`
3. Add a **Sphere Collider** to it:
   - Is Trigger: ✓ (checked)
   - Radius: 1.0 (or desired pickup range)

### 1.4 Configure NetworkedItemPickup Component
In the Inspector for NetworkedItemPickup:
- **Visual Root**: Assign the main GameObject (or specific visual child)
- **Mesh Renderer**: Assign the MeshRenderer component
- **Mesh Filter**: Assign the MeshFilter component
- **Player Tag**: "Player" (must match your player's tag)
- **Auto Pickup**: ✓ (or ✗ if you want manual pickup with E key)
- **Pickup Key**: E (if auto pickup is off)

### 1.5 Save as Prefab
1. Drag the `NetworkedWorldItem` from Hierarchy to your **Prefabs** folder
2. Delete the instance from the scene

### 1.6 Register in NetworkManager
**IMPORTANT**: This prefab must be registered in NetworkManager's Network Prefabs list.

1. Select your **NetworkManager** GameObject in the scene
2. In the Inspector, find **Network Prefabs** list
3. Click **+** to add a new entry
4. Drag your `NetworkedWorldItem` prefab into the slot

---

## Step 2: Set Up Player Prefab

Add the networked inventory components to your existing player prefab.

### 2.1 Open Player Prefab
1. Navigate to your player prefab (e.g., `PlayerCapsule.prefab`)
2. Double-click to open in Prefab Mode

### 2.2 Add NetworkedPlayerInventory
1. Select the root GameObject of the player prefab
2. Add Component > Scripts > HeistNSeek.Core.Inventory.NetworkedInventory > **NetworkedPlayerInventory**
3. Configure:
   - **Max Slots**: 20 (or desired inventory size)

### 2.3 Add NetworkedItemDropper
1. On the same root GameObject
2. Add Component > Scripts > HeistNSeek.Core.Inventory.NetworkedInventory > **NetworkedItemDropper**

### 2.4 Ensure Player Has Tag
1. Make sure your player has the **"Player"** tag set
2. This is used by NetworkedItemPickup to detect players

### 2.5 Save Prefab
1. Press Ctrl+S or click **Save** in the Prefab toolbar

---

## Step 3: Create NetworkedItemSpawnManager in Scene

This singleton manages all item spawning on the server.

### 3.1 Create Manager GameObject
1. In your Main/Gameplay scene, create an empty GameObject
2. Name it `NetworkedItemSpawnManager`

### 3.2 Add Components
1. Add **NetworkObject** component
2. Add **NetworkedItemSpawnManager** script

### 3.3 Configure NetworkedItemSpawnManager
In the Inspector:
- **Scatter Radius**: 1.5 (how far items spread when dropped)
- **Scatter Force**: 3.0 (impulse force applied to items)
- **Spawn Height Offset**: 0.5 (how high above ground to spawn)
- **Networked Item Prefab**: Drag your `NetworkedWorldItem` prefab here

### 3.4 Alternative: Spawn via Script
If you prefer to spawn the manager dynamically, you can instantiate it from your GameplayEntryPoint or similar. Make sure to:
1. Instantiate on server only
2. Call `NetworkObject.Spawn()` after instantiation

---

## Step 4: Set Up Item Registry Initializer

The registry maps item IDs to ItemDataSO for network serialization.

### 4.1 Create Initializer GameObject
1. In your **Bootstrap** scene (or Main scene), create an empty GameObject
2. Name it `ItemRegistryInitializer`

### 4.2 Add Component
1. Add **ItemRegistryInitializer** script

### 4.3 Configure Items
Option A - Manual Assignment:
1. Expand the **Items** array in Inspector
2. Drag all your **ItemDataSO** assets into the array
3. Right-click the component > **Find All Items in Project** (auto-finds all)

Option B - Resources Folder:
1. Check **Load From Resources**
2. Set **Resources Path** to folder name (default: "Items")
3. Move your ItemDataSO files to `Assets/Resources/Items/`

### 4.4 Settings
- **Dont Destroy On Load**: ✓ (keeps registry across scenes)

---

## Step 5: Update Network Manager Prefabs List

Ensure all networked prefabs are registered.

### 5.1 Required Prefabs
Add these to NetworkManager's **Network Prefabs** list:
1. `NetworkedWorldItem` (the world item prefab)
2. `PlayerPrefab` (your player prefab - should already be there)
3. `PlayerSpawner` (if not already added)
4. `NetworkedItemSpawnManager` (if spawning dynamically as prefab)

### 5.2 How to Add
1. Select NetworkManager in scene
2. Find **Network Prefabs** in Inspector
3. Click **+** to add entries
4. Drag prefabs into slots

---

## Step 6: Configure Existing Items

Update your existing ItemDataSO assets.

### 6.1 Ensure Unique Names
- Each ItemDataSO asset **must have a unique name**
- The asset name (filename without .asset) is used as the network ID
- Example: `GoldCoin.asset`, `HealthPotion.asset`, etc.

### 6.2 Prefab Reference (Optional)
- The `prefab` field in ItemDataSO is optional
- If set, NetworkedItemPickup will use its mesh/materials for visuals
- If not set, items will use default visuals

---

## Complete Setup Checklist

### Scene Hierarchy (Main Scene)
```
Main Scene
├── NetworkManager (with network prefabs list)
├── NetworkedItemSpawnManager
│   ├── NetworkObject component
│   └── NetworkedItemSpawnManager script (with prefab reference)
├── ItemRegistryInitializer (if not in Bootstrap)
│   └── ItemRegistryInitializer script (with all items assigned)
├── GameplayLifetimeScope
└── ... other scene objects
```

### Player Prefab Components
```
PlayerCapsule (or your player root)
├── NetworkObject
├── PlayerController
├── NetworkedPlayerInventory ← NEW
├── NetworkedItemDropper ← NEW
└── ... other player components
```

### NetworkedWorldItem Prefab Components
```
NetworkedWorldItem
├── NetworkObject
├── NetworkedItemPickup
├── Rigidbody
├── Collider (non-trigger, for physics)
├── MeshFilter
├── MeshRenderer
└── PickupTrigger (child)
    └── SphereCollider (trigger)
```

### Network Manager Prefabs List
```
Network Prefabs:
├── PlayerPrefab
├── PlayerSpawner
├── NetworkedWorldItem ← NEW
└── NetworkedItemSpawnManager (if prefab)
```

---

## Testing the System

### Test 1: Single Player
1. Start as Host
2. Pick up an item → Should add to inventory
3. Trigger ragdoll (lose balance) → Items should drop and appear in world

### Test 2: Two Players
1. Start Host on one instance
2. Start Client on another instance
3. Host drops items → Client should see items appear
4. Client picks up item → Item should disappear for both
5. Client drops items → Host should see items appear

### Test 3: Inventory Sync
1. Both players pick up items
2. Each player should see only their own inventory count
3. Dropping items should only affect that player's inventory

---

## Troubleshooting

### Items Not Spawning
- Check NetworkedItemSpawnManager has prefab assigned
- Check prefab is in NetworkManager's Network Prefabs list
- Check ItemRegistry is initialized (look for log messages)
- Verify you're running as Server/Host

### Items Not Visible
- Check ItemRegistry has the item registered
- Check ItemDataSO asset name matches expected ID
- Check MeshFilter/MeshRenderer are assigned

### Pickup Not Working
- Verify player has "Player" tag
- Check trigger collider exists and is set to trigger
- Ensure NetworkedItemSpawnManager.Instance is not null
- Check logs for pickup request messages

### Inventory Not Syncing
- Verify NetworkedPlayerInventory is on player prefab
- Check NetworkObject component exists
- Ensure player is spawned via NetworkManager

### "Item not found in registry" Error
- Add item to ItemRegistryInitializer's items array
- Or move ItemDataSO to Resources/Items folder
- Restart play mode after changes

---

## Migration from Old System

If you're migrating from the old SessionInventory system:

1. **Keep SessionInventory** - Can be used for single-player or local-only inventory
2. **Add NetworkedPlayerInventory** - For multiplayer inventory
3. **Update ItemDropper calls** - Replace with NetworkedItemDropper
4. **Update ItemPickup prefabs** - Replace with NetworkedWorldItem prefab

The old system (`SessionInventory`, `ItemDropper`, `ItemPickup`) can coexist with the new networked system. You can gradually migrate or use both for different purposes.

---

## Code Integration Example

### Getting Player's Inventory
```csharp
// On player object
var inventory = GetComponent<NetworkedPlayerInventory>();

// Check item count
int goldCount = inventory.GetItemCount("GoldCoin");

// Add item (server processes this)
inventory.RequestAddItem("GoldCoin", 5);

// Listen for changes
inventory.OnInventoryChanged += () => {
    Debug.Log("Inventory updated!");
};
```

### Spawning Items from Code
```csharp
// On server only
if (NetworkedItemSpawnManager.Instance != null)
{
    NetworkedItemSpawnManager.Instance.SpawnItemInWorld(
        "GoldCoin",
        transform.position,
        impactSpeed: 2f
    );
}
```

### Getting All Player Items for UI
```csharp
var inventory = GetComponent<NetworkedPlayerInventory>();

foreach (var (itemId, amount) in inventory.GetAllItems())
{
    var itemData = ItemRegistry.GetItemData(itemId);
    Debug.Log($"{itemData.itemName}: {amount}");
}
```

---

## File Locations

New files created:
```
Assets/Scripts/Core/Inventory/NetworkedInventory/
├── NetworkedPlayerInventory.cs    - Per-player inventory
├── NetworkedItemDropper.cs        - Drops items on balance loss
├── NetworkedItemPickup.cs         - World item pickup
├── NetworkedItemSpawnManager.cs   - Server item spawning
├── ItemRegistry.cs                - Item ID to data mapping
└── ItemRegistryInitializer.cs     - Registry setup
```

---

## Summary

1. Create `NetworkedWorldItem` prefab with NetworkObject + NetworkedItemPickup
2. Add `NetworkedPlayerInventory` + `NetworkedItemDropper` to player prefab
3. Add `NetworkedItemSpawnManager` to scene with prefab reference
4. Add `ItemRegistryInitializer` with all items assigned
5. Register all networked prefabs in NetworkManager
6. Test with Host + Client

The system is now server-authoritative: all inventory changes and item spawning happen on the server and sync to clients automatically.
