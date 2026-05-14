# Scout Drone & Equipment Slots — Unity Setup Guide

## Overview of new scripts

| Script | Purpose |
|---|---|
| `EquipmentSystem` | Singleton managing the 3 upgrade slots |
| `EquipmentSlotUI` | One slot visual (drag/drop target) |
| `EquipmentPanelUI` | The 3-slot panel inside the inventory screen |
| `ScoutDrone` | Drone that orbits player and scans for targets |
| `ScoutDroneEquipSystem` | Activates the drone when its item is slotted |
| `MinimapUI` | Circular radar HUD |

---

## Step 1 — EquipmentSystem singleton

1. On your persistent **GameManager / Player** GameObject (the one that's `DontDestroyOnLoad`), add the **EquipmentSystem** component.
   - That's it — no fields to assign.

---

## Step 2 — Equipment Panel UI (inside inventory)

### 2a — Create the EquipmentSlot prefab

1. In the Hierarchy, create: **UI → Image** and name it `EquipmentSlotPrefab`.
2. Set its size to something like **64×64**.
3. Give it a background color (e.g. dark grey semi-transparent: `#1A1A1AAA`).
4. Add a **child Image** named `Icon` — this is where the item icon will render.
   - Size: **56×56**, centered.
5. Add the **EquipmentSlotUI** component to the root `EquipmentSlotPrefab` object.
6. Wire `Icon Image` → `Icon Image` field on EquipmentSlotUI.
7. Save as a prefab in `Assets/Prefabs/UI/`.

### 2b — Create the Equipment Panel in the inventory screen

1. Inside your **InventoryPanel** GameObject, create a child **UI → Panel** named `EquipmentPanel`.
2. Position it somewhere visible — suggested: a column to the right of the inventory grid, labelled "UPGRADES".
   - Add a **TextMeshPro - Text (UI)** as a header label reading "UPGRADES".
3. Create a child **UI → Empty** named `SlotContainer` inside `EquipmentPanel`.
   - Set its layout: add a **Horizontal Layout Group** (or Vertical — your choice) with spacing ~8.
4. Add the **EquipmentPanelUI** component to `EquipmentPanel`.
5. Wire fields:
   - `Slot Prefab` → the `EquipmentSlotPrefab` you just made.
   - `Slot Container` → the `SlotContainer` transform.

> The panel will automatically show/hide with the inventory panel since it's a child of it.

---

## Step 3 — Scout Drone item

1. **Create/Assets/Gravitas/Items** → right-click → **Create → Gravitas → Items → ItemData**.
2. Name it `Scout Drone`.
3. Fill in:
   - **Item Name**: `Scout Drone`
   - **Item Type**: `Equipment`  ← critical
   - **Description**: `A compact reconnaissance drone. Orbits above you and paints enemies, resources, and points of interest on a radar display.`
   - **Icon**: assign a drone/tech icon sprite.
   - **Is Unique**: ✓ (only one at a time)
   - **Save Id**: `scout_drone`

---

## Step 4 — Scout Drone GameObject

1. In the scene hierarchy under your **Player** root, create an empty GameObject named `ScoutDroneObject`.
2. Add the **ScoutDrone** component to it.
   - It auto-generates a sphere mesh in `Awake` as a placeholder.
   - Tune **Orbit Radius** (default 1.5), **Orbit Height** (default 1.8), **Orbit Speed** (default 60°/s), **Scan Radius** (default 75), **Scan Interval** (default 2s).
3. **Set the GameObject INACTIVE** in the Inspector — `ScoutDroneEquipSystem` activates it.

---

## Step 5 — ScoutDroneEquipSystem

1. On the Player root (or wherever is convenient), add **ScoutDroneEquipSystem**.
2. Wire fields:
   - `Scout Drone Game Object` → the `ScoutDroneObject` you just made.
   - `Scout Drone Item Data` → the `Scout Drone` ItemData asset.

---

## Step 6 — Minimap UI

### 6a — Create the Minimap panel

1. On your persistent **HUD Canvas**, create a child **UI → Panel** named `MinimapRoot`.
2. Anchor it to **top-right** of the screen.
3. Set size to roughly **160×160**.
4. Give it a **circular background** sprite:
   - Import a circle/disc PNG, set **Texture Type → Sprite (2D and UI)**.
   - Assign to the panel's **Image → Source Image**. Set **Color** to a semi-transparent dark (`#00000099`).
5. Add a **Mask** component to `MinimapRoot` (check **Show Mask Graphic**).
6. Create a child **UI → Empty** named `DotContainer` — this holds the blip dots.
7. Add the **MinimapUI** component to `MinimapRoot`.
8. Wire fields:
   - `Dot Container` → the `DotContainer` transform.
   - `Minimap Radius` → **70** (half your panel width, so 80 if panel is 160px).
   - `World Radius` → **75** (matches ScoutDrone scan radius).
   - Colours: Enemy=Red, Resource=Yellow, POI=Cyan, Player=White.
   - `Dot Size` → **8**.
9. **Set `MinimapRoot` INACTIVE** in the Inspector — `ScoutDroneEquipSystem` activates it.

> The **Mask** on MinimapRoot will clip any blip that drifts to the edge — this gives the clean circular border.

---

## Step 7 — Tag setup

The drone detects objects by Unity **Tags**. Tag any GameObjects you want to show on the radar:

| Tag | Shows as |
|---|---|
| `Enemy` | Red dot |
| `WorldItem` | Yellow dot |
| `POI` | Cyan dot |

Go to **Edit → Project Settings → Tags and Layers** and add any missing tags. Then set the Tag on relevant prefabs.

---

## Step 8 — Give yourself a Scout Drone to test

Temporarily add this to a MonoBehaviour's `Start()` or use a debug button:

```csharp
var droneItem = new InventoryItem(scoutDroneItemData); // reference your ItemData
InventorySystem.Instance.TryAddItem(droneItem);
```

Then open inventory (Tab), drag the Scout Drone from your bag into one of the 3 upgrade slots. The drone should appear and orbit. The minimap panel should appear in the top-right.

---

## Interaction summary

| Action | Result |
|---|---|
| Drag Equipment item → upgrade slot | Equips it (removed from bag) |
| Left-click occupied upgrade slot | Picks item up into drag cursor |
| Drop dragged item onto inventory slot | Returns to that slot (or first free) |
| Right-click occupied upgrade slot | Instantly returns to bag |
| Close inventory while dragging from equip slot | Item returned to bag |

---

## Later: adding the T2 upgrade

When you're ready to add the T2 Scout Drone upgrade ability:
1. Create a separate `ScoutDroneT2` ItemData with a flag or a sub-type.
2. Extend `ScoutDroneEquipSystem` (or add a new component) to check for the T2 item and unlock the extra ability.
3. Optionally add a second behaviour script on `ScoutDroneObject` that the equip system enables conditionally.
