# Resource Gathering — Setup Guide

This system implements the **Resource Gathering** section of the GDD: gravity-pulse cracks, the Resource Extractor tool, biome-weighted resource rolls, and Subnautica-style natural outcrops.

All of the new code lives under `Assets/Scripts/ResourceGathering/`. The editor data generator lives at `Assets/Editor/ResourceGatheringDataGenerator.cs`.

---

## 1. One-time scene & project setup

### 1a. Create the `Harvestable` layer
1. Open **Edit → Project Settings → Tags and Layers**.
2. Find an empty user layer slot and name it **`Harvestable`** (exact spelling, no quotes).
3. In the next slot, optionally add **`ResourceCrack`** as a layer for crack colliders only — used to keep the extractor raycast cheap. (You can skip this and just use the default layer; the system works either way.)

### 1b. Generate the BiomeSpawnTable
1. In Unity, open the menu **Gravitas → Generate Resource Gathering Data**.
2. This creates `Assets/data/ResourceGathering/BiomeSpawnTable.asset`, prefilled from the GDD Surface Spawn Distribution chart with default glow colors per resource.
3. Open the asset and tune `weight` and `glowColor` for each biome × resource as you like. Weights are relative — `12` is roughly twice as common as `6`.

### 1c. (Once you make item world prefabs) Wire `worldPrefab`
- Each raw-resource `ItemData` in `Assets/data/Basic Resources/` has a `worldPrefab` field.
- Drag the matching prefab from `Assets/Prefabs/Items/` into that slot.
- This is what `OutcropSpawner` instantiates for natural scattered spawns. The crack system does NOT need this — it only adds resources to the inventory directly.

---

## 2. Set up the BiomeManager

Drop a single empty GameObject into your scene named `BiomeManager` and add the **`BiomeManager`** component.
- **Default Biome** = the biome used when the player isn't inside any `BiomeZone`. For testing, set it to `Biome2` so cracks roll real minerals immediately.
- **Player Tag** = `Player` (default).

### 2a. (Bandaid) Place `BiomeZone` triggers
For each biome region in your scene:
1. Make an empty GameObject, add a **BoxCollider** (or any collider).
2. Add the **`BiomeZone`** component, set `biome` to `Biome1`, `Biome2`, `Biome3`, `Biome4`, or `Biome5`.
3. The collider's `isTrigger` is auto-flipped to `true` on Reset; verify it.
4. Position and resize the box around the biome's playable area.

`BiomeZone`s nest properly — when zones overlap, the smallest one wins. No zone covering a position = the BiomeManager's default biome.

### 2b. (Real fix, later) Plug in a world-gen biome provider
Once `ProceduralWorldGenerator` knows which biome each chunk is, write a class that implements `BiomeManager.IBiomeProvider`:

```csharp
public class WorldGenBiomeProvider : MonoBehaviour, BiomeManager.IBiomeProvider {
    [SerializeField] ProceduralWorldGenerator worldGen;
    public BiomeType GetBiomeAt(Vector3 worldPos) {
        // ask worldGen which chunk this is and what biome it baked
        return worldGen.GetBiomeAt(worldPos);
    }
    void Awake() { BiomeManager.Provider = this; }
}
```

Set `BiomeManager.Provider` on Awake. The provider is queried first; it can return `BiomeType.None` to defer to the trigger-zone system as a fallback. You can leave the `BiomeZone` triggers in place too — they're handy for hand-placed biome overrides like the crash site (always Biome 4).

---

## 3. Tag your harvestable terrain

Any rock, ground chunk, or platform that can yield resources needs ONE of:
- **Layer = `Harvestable`** (fast, applies to thousands of objects automatically), OR
- A **`HarvestableSurface`** component (lets you override the biome on a specific surface)

Use both together if you want hand-placed resource pockets in zones that wouldn't normally have them — set `overrideBiome = true` and pick the biome you want.

In `ProceduralWorldGenerator`, when you generate platforms/rocks meant to be harvestable, set `gameObject.layer = LayerMask.NameToLayer("Harvestable");` instead of (or in addition to) the existing layer.

---

## 4. CrackSpawner

Add a single empty GameObject named `CrackSpawner` and add the **`CrackSpawner`** component.

Inspector fields:
- **Spawn Table**: drag `BiomeSpawnTable.asset`.
- **Crack Prefab**: leave `None` for now — the system builds a procedural visual at runtime. (See section 7 for swapping in a custom prefab later.)
- **Harvestable Mask**: tick the `Harvestable` layer.
- **Accept Harvestable Component**: leave `true` so anything with a `HarvestableSurface` works regardless of layer.
- **Min Crack Spacing**: `2` (matches your spec).
- **Log Spawns**: turn on while testing.

The `GravityGun` already calls `CrackSpawner.Instance.TrySpawn(...)` from inside `TryFirePulse()` — no further wiring on the gun is needed.

---

## 5. Resource Extractor (the "Rock Pulverizer" rename)

Your existing `Rock Pulverizer Tier 1.asset` and `Rock Pulverizer T2.asset` ItemData assets ARE the Resource Extractor T1/T2 from the new GDD — same craft cost, same role. You don't have to recreate them. Two options:

**(Option A — keep existing assets, recommended)**
1. Open each asset, change the `Item Name` field to `Resource Extractor T1` / `Resource Extractor T2` for clarity. The recipe assets keep their references because they point by GUID.
2. Optionally rename the `.asset` filenames to match.

**(Option B — leave them named "Rock Pulverizer")**
- Skip step 1. Everything still works; it's just a label mismatch with the GDD.

### 5a. Wire the extractor onto the player
Add the extractor as a child of your player rig (same way the GravityGun is set up):

1. Make an empty GameObject under the player, named `ResourceExtractorT1`.
2. Add **`ResourceExtractor`** component.
   - **Extractor Tier**: `1`
   - **Extract Time**: `2`
   - **Yield Min**: `1`, **Yield Max**: `2`
   - **Max Range**: `8` (or match your gun)
   - **Crack Layer Mask**: include `ResourceCrack` (or just leave as `Everything`)
   - **Extract Action**: drag in the **Shoot** input action reference (reuses gun's left-click). You can also create a dedicated action.
   - **Tool Visual**: optional child mesh that turns on/off with equip state.
3. Add **`ResourceExtractorEquipSystem`** component.
   - **Extractor**: drag the ResourceExtractor component you just added.
   - **Extractor Item Data**: drag the `Rock Pulverizer Tier 1.asset` (or your renamed `Resource Extractor T1.asset`).

Repeat for T2 on a sibling GameObject:
- **Extractor Tier**: `2`
- **Extract Time**: `1` (faster)
- **Yield Min**: `2`, **Yield Max**: `3` (more per pull)
- **Extractor Item Data**: `Rock Pulverizer T2.asset`.

### 5b. Progress bar HUD
1. In your player HUD canvas, make a small panel for the extraction progress bar.
2. Add a CanvasGroup, a child Image with `Image Type = Filled, Horizontal`, and an optional TMP_Text label.
3. Add the **`ExtractorProgressUI`** component to the panel root.
4. Drag in the ResourceExtractor (T1 or T2 — whichever is currently active; you can have one UI per tier or one shared UI that points at either).

---

## 6. Outcrop spawner (natural Subnautica spawns)

Drop a `OutcropSpawner` GameObject into each biome region:
1. New empty GameObject, position it at the biome center.
2. Add **`OutcropSpawner`** component.
3. **Spawn Table**: drag `BiomeSpawnTable.asset`.
4. **Biome**: pick the biome (`Biome1`–`Biome5`).
5. **Half Extents**: size of the biome's volume (e.g., `(40, 20, 40)`).
6. **Outcrop Count**: how many to scatter (e.g., `30` for Biome2, `15` for Biome5).
7. **Surface Layer Mask**: tick whatever ground/platform layer your terrain uses.
8. **Spawn On Start**: `true` for a one-shot scatter. Disable and call `Spawn()` from your world gen if you want to control timing.

> Outcrops do NOT regenerate. They're the "limited supply" path. Cracks are the renewable main path.

---

## 7. (Optional) Swap in a custom crack prefab

The default crack visual is a procedural transparent quad — generated at runtime, no asset imports. To replace it:

1. Make a new empty prefab `Assets/Prefabs/ResourceCrack.prefab`.
2. Add a **`ResourceCrack`** component.
3. Add a `MeshFilter`, `MeshRenderer`, and a flat plane/decal mesh you like.
4. Assign your custom material to the `Override Material` slot on `ResourceCrack`. The component will tint that material's `_BaseColor` (or `_Color`) per resource roll.
5. Add a `BoxCollider` set to `isTrigger = true` so the extractor raycast can hit it.
6. Drag the prefab into `CrackSpawner`'s `Crack Prefab` field.

---

## 8. Smoke test

1. Press play.
2. Equip the Gravity Gun, install a Refined Cell (Tier 2) so Pulse works.
3. Press your Pulse hotkey, then left-click at a piece of `Harvestable` terrain. A glowing crack should appear.
4. Switch to Resource Extractor on the hotbar.
5. Hold left-click while looking at the crack. The progress bar fills, then the resource lands in your inventory and the crack dims.
6. Wait 30 seconds. The crack glows again — extract once more.

If anything fails:
- **Crack doesn't spawn** → check your terrain is on the `Harvestable` layer or has a `HarvestableSurface` component, AND `CrackSpawner.Harvestable Mask` matches that layer.
- **Crack spawns but no resource is rolled** → the BiomeSpawnTable for that biome is empty (Biome1 is empty by default — that's intentional; it's plant territory).
- **Extractor doesn't fire** → make sure the equipped hotbar item's ItemData matches `ResourceExtractorEquipSystem.Extractor Item Data`. Watch the console; the system logs equip state.
- **Extractor logs "no target"** → your raycast layer mask doesn't include the layer the crack collider is on. Set the mask to `Everything` while debugging.

---

## 9. Future / not done in this pass (per your call)

- **Plant gathering** (knife on bushes, axe on trees, score-and-wait for resin/rubber juice). The system is designed so the same `WorldItem` + `InventorySystem` pipeline can hook these in cleanly. Note: `CrackVisuals` and `CrackSpawner` are minerals-only by intent.
- **Real biome provider** (see 2b) — replaces the trigger-zone bandaid once `ProceduralWorldGenerator` bakes biome data.
- **Tool models** — the extractor has a `Tool Visual` slot that takes any child GameObject; drop your model in there.
- **VFX/SFX** — `CrackSpawner.TrySpawn` is the single hook point to add a screen flash / impact particle. `ResourceExtractor.OnExtractCompleted` for the success cue.
