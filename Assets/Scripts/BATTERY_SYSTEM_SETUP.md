# Battery System Setup Guide

## Overview
This document provides step-by-step instructions to complete the Graviton Cell Battery System setup in the Unity editor.

---

## Part 1: Create Graviton Cell ScriptableObject Assets

The `GravitonCell` ScriptableObject has been marked with `[CreateAssetMenu]`, so you can create instances directly in the editor.

### Creating Cell Tier 1 — Salvage Cell:
1. Right-click in the **Project window** (under `Assets/Resources/` or a designated `Battery/` folder)
2. Select **Create > Gravitas > Graviton Cell**
3. Name it: `SalvageCell`
4. In the Inspector, set:
   - **Cell Name:** `Salvage Cell`
   - **Max Charge:** `100`
   - **Passive Recharge Rate:** `2`
   - **Icon:** (optional, assign a sprite)
   - **Tier:** `1`

### Creating Cell Tier 2 — Refined Cell:
1. Right-click and create another Graviton Cell
2. Name it: `RefinedCell`
3. In the Inspector, set:
   - **Cell Name:** `Refined Cell`
   - **Max Charge:** `250`
   - **Passive Recharge Rate:** `4`
   - **Icon:** (optional)
   - **Tier:** `2`

### Creating Cell Tier 3 — Architect Cell:
1. Right-click and create another Graviton Cell
2. Name it: `ArchitectCell`
3. In the Inspector, set:
   - **Cell Name:** `Architect Cell`
   - **Max Charge:** `500`
   - **Passive Recharge Rate:** `7`
   - **Icon:** (optional)
   - **Tier:** `3`

### Creating Cell Tier 4 — Core Cell:
1. Right-click and create another Graviton Cell
2. Name it: `CoreCell`
3. In the Inspector, set:
   - **Cell Name:** `Core Cell`
   - **Max Charge:** `1000`
   - **Passive Recharge Rate:** `12`
   - **Icon:** (optional)
   - **Tier:** `4`

---

## Part 2: Set Up GunBatterySystem on the Gun

1. **Find the Gun GameObject** in your scene (likely named "GravityGun" or similar, wherever you have the GravityGun.cs component)
2. **Add Component:** Click **Add Component** in the Inspector → Search for `GunBatterySystem` and add it
3. **Assign Default Cell:**
   - In the **GunBatterySystem** component, find the **Default Cell** field
   - Drag-and-drop your **SalvageCell** (Tier 1) asset into this field
   - This cell will load when the game starts

---

## Part 3: Wire Up GravityGun Battery References

1. **Select the Gun GameObject** (same one with GravityGun.cs and GunBatterySystem.cs)
2. In the **GravityGun** component Inspector, find the **Battery** section:
   - **Battery System:** Drag the same GameObject into this field (or leave empty — it will auto-find GunBatterySystem)
   - **Out Of Charge Clip:** (optional) Assign an AudioClip that plays when the gun is out of charge. For now, you can leave this empty.

---

## Part 4: Create and Wire Up the Battery HUD

### Step 1: Create a Canvas
1. In the Hierarchy, right-click → **UI > Canvas**
2. Name it: `BatteryHUD_Canvas`
3. Select it and in the Inspector, set:
   - **Render Mode:** `Screen Space - Overlay`
   - (This makes it render on top of the game world)

### Step 2: Create the Charge Bar
1. Right-click on the Canvas → **UI > Image**
2. Name it: `ChargeBarBackground`
3. Position and size:
   - **Anchor:** Bottom-Left
   - **Pos X:** `60`, **Pos Y:** `30`
   - **Width:** `200`, **Height:** `30`
4. Set background color (dark grey or dark blue) in the **Image** component
5. Add a child Image for the fill:
   - Right-click **ChargeBarBackground** → **UI > Image**
   - Name it: `ChargeBarFill`
   - Set **Image Type** to **Filled** and **Fill Method** to **Horizontal**
   - Position it to cover the entire parent (same size, no offset)
   - Color: Leave default or customize

### Step 3: Create Text Labels
1. Add a TextMeshPro text element for the cell name:
   - Right-click Canvas → **UI > TextMeshPro - Text**
   - Name it: `CellNameText`
   - **Pos X:** `260`, **Pos Y:** `35`
   - **Text:** `SALVAGE CELL`
   - Font size: `20`

2. Add another TextMeshPro for the percentage:
   - Right-click Canvas → **UI > TextMeshPro - Text**
   - Name it: `ChargePercentText`
   - **Pos X:** `260`, **Pos Y:** `15`
   - **Text:** `100%`
   - Font size: `16`

### Step 4: Add BatteryHUD Component
1. Create a new GameObject as a child of the Canvas:
   - Right-click Canvas → **Create Empty**
   - Name it: `BatteryHUD_Manager`
2. Add the **BatteryHUD** component:
   - Select **BatteryHUD_Manager** → **Add Component** → `BatteryHUD`
3. In the Inspector, assign the references:
   - **Battery System:** Drag the gun GameObject (with GunBatterySystem) into this field
   - **Charge Bar Fill:** Drag the **ChargeBarFill** Image into this field
   - **Cell Name Text:** Drag the **CellNameText** TextMeshPro into this field
   - **Charge Percent Text:** Drag the **ChargePercentText** TextMeshPro into this field
4. (Optional) Customize colors:
   - **Normal Color:** Light blue/teal (0.2, 0.8, 1.0)
   - **Warning Color:** Amber/yellow (1.0, 0.8, 0.2)
   - **Critical Color:** Red (1.0, 0.2, 0.2)

---

## Part 5: Test the System

1. **Add a GravityBody** to a test object (if you haven't already)
2. **Play Mode:**
   - Verify the charge bar appears at bottom-left with the cell name and percentage
   - Try switching to Placement Mode and applying gravity to objects
   - Watch your charge decrease by 15 (apply) or 8 (remove)
   - If you select the player, applying gravity should cost 15 + 25 = 40 total
3. **Passive Recharge:**
   - Stop applying gravity and watch the charge bar slowly refill over time
4. **Out of Charge:**
   - Once charge hits 0, all gun functionality should be disabled (no selection or placement)
   - If you assigned an audio clip, you should hear it when trying to use the gun at 0 charge

---

## Inspector Assignment Quick Reference

### GunBatterySystem
- **Default Cell:** SalvageCell (or whichever tier you want to start with)

### GravityGun
- **Battery System:** (auto-finds, or assign the gun GameObject)
- **Out Of Charge Clip:** (optional, assign an audio file)

### BatteryHUD
- **Battery System:** Gun GameObject
- **Charge Bar Fill:** The fill Image
- **Cell Name Text:** TextMeshPro for name
- **Charge Percent Text:** TextMeshPro for percentage
- **Normal Color:** Light blue/teal
- **Warning Color:** Yellow/amber
- **Critical Color:** Red

---

## Notes

- **Charge costs are defined as constants in GravityGun.cs:**
  - `COST_GRAVITY_APPLY = 15f` (base cost to apply gravity)
  - `COST_GRAVITY_REMOVE = 8f` (cost to remove gravity)
  - `COST_PLAYER_SELF = 25f` (additional cost if player is in selection)
  
- **Future ability costs are already defined but not yet implemented:**
  - `COST_GRAVITY_PULSE = 30f`
  - `COST_GRAVITY_LOCK = 20f`
  - `COST_CORE_RESONATOR = 60f`

- Selection mode is always FREE — no charge cost.
- Passive recharge happens automatically in the background via GunBatterySystem.Update().
- The UI updates in real-time via the OnChargeChanged event.

