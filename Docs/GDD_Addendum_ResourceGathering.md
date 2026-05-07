# GDD Addendum — Resource Gathering Implementation

> Paste/merge this section into the GDD under **Resource Gathering** so the design doc reflects what's actually built.

---

## Resource Gathering — System Spec

### Overview

There are two ways to gather mineral resources, both biome-weighted by the **Surface Spawn Distribution** chart.

1. **Cracks** — main, renewable path. Unlocked at Gravity Gun Tier 2.
2. **Natural outcrops** — limited, scattered loose resources. Available from the start. Subnautica-style.

Plant resources (fibers, resin, wood, rubber juice) are a separate system, not implemented yet.

### Crack loop

1. Player equips the Gravity Gun, installs a **Refined Cell (Tier 2)** or higher to unlock **Gravity Pulse**.
2. Pulse fired at terrain on the **`Harvestable`** layer creates a **glowing crack** at the impact point.
   - Crack glows a color determined by the **resource** rolled, which is determined by the **biome** at the impact position.
   - Crack does not spawn on non-harvestable surfaces (regular ground, walls without the layer).
   - Cracks cannot stack: pulse spam does nothing within **2 m** of an existing crack.
3. Player switches to the **Resource Extractor** tool.
4. Aim + hold the extract input on the crack. A **progress bar** fills over the extractor's `extractTime`.
5. On completion, the rolled resource is added to inventory in the configured yield range.
6. Crack visually dims and stops glowing. After **30 s** (default, configurable) the crack relights and can be extracted again. **The same resource respawns** — cracks do not re-roll.

### Resource Extractor tiers

| Tier | Extract Time | Yield per Extraction | Notes |
|------|--------------|----------------------|-------|
| T1   | 2.0 s        | 1–2 (avg ~1.5)        | Crafted with `Gravity crystal x1; metal ingot x2; glue x1; wiring kit x1` |
| T2   | 1.0 s        | 2–3 (avg ~2.5)        | Crafted from T1 + `wiring kit x1; quantum computing chip x1`. Faster, more material per pull. No new material gating — same resource pool. |

(Extract Time and yield ranges are inspector values on the `ResourceExtractor` component — easy to tune.)

### Biome resource weights

Per the existing **Surface Spawn Distribution** chart. Rarity → roll weight:

| Rarity   | Weight |
|----------|--------|
| None     | 0      |
| Very Rare| 1      |
| Rare     | 3      |
| Uncommon | 6      |
| Common   | 12     |

Defaults from the chart are baked into the editor menu **Gravitas → Generate Resource Gathering Data**, which writes to `Assets/data/ResourceGathering/BiomeSpawnTable.asset`. Adjust there.

### Glow colors (default)

| Resource         | Approximate Color |
|------------------|-------------------|
| Copper deposit   | Orange            |
| Silver deposit   | Pale silver-blue  |
| Quartz           | Light cyan        |
| Gravity Crystal  | Bright teal       |
| Lithium          | Violet            |
| Metal Scrap      | Orange-yellow     |
| Alien Tech (later) | Cyan-aqua HDR   |

All colors are HDR with intensity multipliers so bloom in the URP volume makes them shine.

### Natural outcrops

- Spawned at scene-start by `OutcropSpawner` components, one per biome region.
- Use the same biome weights as cracks but instantiate the `worldPrefab` defined on each `ItemData` directly into the world.
- Limited supply — outcrops do NOT respawn. Players burn through them quickly; cracks are the long-term economy.
- Spawner enforces a minimum 2.5 m spacing between outcrops and a max slope angle of 45° to keep them on stable ground.

### Biomes

The system queries `BiomeManager.GetBiomeAt(worldPos)` whenever a crack or outcrop is rolled. Biomes are referenced by number only (`Biome1`–`Biome5`) in code and data.

- Until the procedural world generator bakes biome data per chunk, biomes are defined by **`BiomeZone` trigger volumes** placed by hand. Smaller zones override larger ones.
- When the world generator is ready, implement `BiomeManager.IBiomeProvider` and assign `BiomeManager.Provider` — the trigger zones become an optional fallback for hand-placed overrides.

### What blocks what

- Pulse mode without Tier 2 cell → no cracks (existing tier-gating, unchanged).
- Crack inside an inventory-full state → extractor stops at the first failed `TryAddItem`, partial yield is added, the crack still goes into regen.
- No `BiomeSpawnTable` assigned to `CrackSpawner` → no crack spawns, warning logged.
- Hit surface not on `Harvestable` layer and no `HarvestableSurface` component → pulse pushes physics but no crack.

### Naming change vs. previous GDD

- **"Rock Pulverizer Tier 1"** is now **"Resource Extractor T1"**.
- **"Rock Pulverizer T2"** is now **"Resource Extractor T2"**.
- Existing `Rock Pulverizer*.asset` files in the project still function — rename in-place to `Resource Extractor` for consistency, or leave for now.

---

## Open / Future

- Plant gathering loop (knife/axe — fibers, resin, wood, rubber juice).
- Crack VFX (impact flash, particles, audio cue on extract).
- Tool model swap.
- World-gen-aware biome lookup replacing hand-placed `BiomeZone` triggers.
