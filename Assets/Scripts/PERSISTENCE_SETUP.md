# Cross-Scene Persistence Setup

Code is in. Now set this up in the editor so the player and core systems survive scene transitions.

## What changed in code

Self-DDOL'ing singletons (each calls `DontDestroyOnLoad` on its own GameObject in `Awake`):

- `InventorySystem`
- `HotbarSystem`
- `CraftingSystem`
- `InventoryUI`
- `CraftingUI`
- `PickupPromptUI`
- `ScreenFade`
- `SaveManager` (already was)
- `GravityController` (already was)
- `GravityZoneManager` (already was)

New components:

- `PlayerPersistence` — attach to the Player root. Makes the player DDOL, destroys duplicates in destination scenes, and teleports to a spawn point.
- `PersistentRoot` — generic helper for the HUD Canvas (or any non-singleton root you want to persist). Set a unique `id` per root.

Updated:

- `SceneTransitionTrigger` now has a `targetSpawnPoint` field. Set it to the name of the empty GameObject in the destination scene where the player should land.
- `SaveManager.ClearSaveAndReset()` exists now (the new "Clear Save File" button in the inspector).
- `SaveManager.IsRestoringFromDisk` — `PlayerPersistence` checks this so SaveManager wins the player position fight when loading from a save file.

Per-scene singletons (NOT DDOL — one per biome scene):

- `BiomeManager` (each biome has its own default biome)
- `CrackSpawner` (cracks are scene-specific)

## Two scenes you'll need

### A. The "Bootstrap" scene (a.k.a. game-start scene)

This is the scene that loads first when the game launches. Everything DDOL-able lives here. After moving from this scene to a biome scene, all of these survive.

Create a new empty scene called something like `Bootstrap.unity` and put these GameObjects in it as scene-roots:

| GameObject name | Components | Notes |
|---|---|---|
| `[Persistent] Player` | All your existing player components + **`PlayerPersistence`** | The player root. Tagged `"Player"`. Child contains the Camera (already wired by `PlayerMovement.Awake`). |
| `[Persistent] InventorySystem` | `InventorySystem` | Empty GameObject; data-only. |
| `[Persistent] HotbarSystem` | `HotbarSystem` | Empty. |
| `[Persistent] CraftingSystem` | `CraftingSystem` | Drag your `CraftingRecipe` assets into the `allRecipes` field. |
| `[Persistent] GravityController` | `GravityController` | Empty. |
| `[Persistent] GravityZoneManager` | `GravityZoneManager` | Empty. |
| `[Persistent] SaveManager` | `SaveManager` + `SaveHotkeys` | Empty. |
| `[Persistent] ScreenFade` | `ScreenFade` | Creates its own canvas at runtime. |
| `[Persistent] HUD Canvas` | Canvas + CanvasScaler + GraphicRaycaster + **`PersistentRoot`** (id: `"HUD"`) | Children: `HealthHUD`, `BatteryHUD`, `GravityGunHUD`, `HotbarUI` panels. |
| `[Persistent] Inventory UI` | Canvas + `InventoryUI` | The big inventory grid. (Already a singleton, will DDOL itself.) |
| `[Persistent] Crafting UI` | UIDocument + `CraftingUI` | (Already a singleton.) |
| `[Persistent] Pickup Prompt` | Canvas (or part of HUD) + `PickupPromptUI` | (Already a singleton.) |
| `[Persistent] Item Pickup Handler` | `ItemPickupHandler` | If on its own GameObject. (If on the player, it persists through the player DDOL.) |
| `[Persistent] Gravity Gun Equip` | `GravityGunEquipSystem` | Same — usually on the player. |

Then add a single `SceneTransitionTrigger` (or just call `SceneManager.LoadScene("Biome 1")` from a script/button) so that after Bootstrap finishes initializing, you transition into Biome 1.

> **Quick start without a Bootstrap scene:** if you'd rather not split the scene apart yet, just leave all these in `Biome 1` *and never go back to Biome 1 from another scene*. The first time you load Biome 1, the singletons grab their `Instance` and DDOL themselves — they survive the rest of the game. But if you re-enter Biome 1 later, the scene-baked duplicates will collide with the surviving DDOL'd ones. The duplicates auto-destroy themselves, but it's noisy. Splitting Bootstrap out is cleaner.

### B. Each biome scene (`Biome 1`, `Biome 2`, … `Biome 5`)

Each biome scene needs only the *scene-specific* stuff — not the player, not the UI, not the inventory.

| GameObject | Components | Notes |
|---|---|---|
| `PlayerSpawnpoint` | (empty Transform) | The persistent player teleports here on scene entry. Required. Name it exactly this — or override per-trigger via `SceneTransitionTrigger.targetSpawnPoint`. |
| `BiomeManager` | `BiomeManager` | Set `defaultBiome` to this scene's biome (Biome1, Biome2, etc.). |
| `CrackSpawner` | `CrackSpawner` | Assign the spawn table + harvestable layer. |
| `BiomeZone` (any) | Collider trigger + `BiomeZone` | Optional, for sub-regions inside a biome. |
| `GravityZone` (any) | SphereCollider + `GravityZone` | Optional. |
| Doors/portals to other biomes | Collider + `SceneTransitionTrigger` | Set `targetScene` and (optional) `targetSpawnPoint`. |
| World geometry, lighting, audio | … | The actual scene content. |

What each biome scene should **not** contain (delete if present):

- Player GameObject (it's coming with you from Bootstrap).
- Camera — the player owns its camera.
- InventorySystem, HotbarSystem, CraftingSystem, InventoryUI, CraftingUI, PickupPromptUI, ScreenFade, SaveManager, HUD Canvas — all DDOL'd from Bootstrap.

If duplicates do exist in a biome scene, the singleton checks will destroy them automatically — this is a safety net, not the recommended pattern.

## Setup steps right now

You said your current scene is partway through. Here's the order:

1. **Open `Biome 1`.**
2. In the Inspector for the Player GameObject, **add the `PlayerPersistence` component**. Default values are fine.
3. **Add an empty GameObject named `PlayerSpawnpoint`** at the position you want the player to land when entering this scene. Match its rotation to the player's preferred starting orientation.
4. **Create `Bootstrap.unity`**: `File > New Scene > Empty`. Save as `Assets/Scenes/Bootstrap.unity`.
5. In Bootstrap, move (or recreate) the GameObjects from Section A above. Easiest path: in `Biome 1`, select the singletons + Player + UI canvases → cut → switch to `Bootstrap` → paste.
6. In Bootstrap, add a temporary GameObject with this script to kick you into Biome 1:

   ```csharp
   using UnityEngine;
   using UnityEngine.SceneManagement;
   public class GoToFirstScene : MonoBehaviour {
       [SerializeField] string firstScene = "Biome 1";
       void Start() { SceneManager.LoadScene(firstScene); }
   }
   ```

7. **`File > Build Profiles` (or Build Settings)**: add `Bootstrap` as the first scene, then `Biome 1`–`Biome 5`. Currently only `Biome 1` and `Grav gun test` are enabled — add the rest. Drag `Bootstrap` to index 0 so it loads first.
8. On your existing `SceneTransitionTrigger` cubes between biomes, set `targetScene` and (if needed) `targetSpawnPoint` to the spawn point name in the destination scene.
9. Press Play in `Bootstrap`. You should see:
   - `[SaveManager] Save file: …/save.json` log
   - `[Persistent] Player` move into Biome 1's spawn point
   - Walk into a transition cube → cleanly switch to next biome → player still has inventory/hotbar/health/battery, cracks you made still glow.

## Per-scene checklist (printable)

For each biome scene:

- [ ] Has exactly one `PlayerSpawnpoint` empty GameObject (or whatever spawn point name your transitions reference)
- [ ] Has a `BiomeManager` with the right `defaultBiome`
- [ ] Has a `CrackSpawner` with the right spawn table + layer mask
- [ ] Has the world geometry tagged correctly (Harvestable layer for resource cracks, Ground layer for player groundcheck)
- [ ] Has all `SceneTransitionTrigger` cubes with `targetScene` filled in
- [ ] Does **not** contain a Player, Camera, InventorySystem, HotbarSystem, CraftingSystem, InventoryUI, CraftingUI, PickupPromptUI, ScreenFade, SaveManager, GravityController, or GravityZoneManager
- [ ] Is added and enabled in Build Settings

## Verification (5-minute sanity test)

1. Load Bootstrap → Biome 1.
2. Mine a couple of cracks. Pick up an item. Equip it on the hotbar.
3. Press F5 (manual save).
4. Walk through a `SceneTransitionTrigger` to Biome 2.
5. Verify: same player, same camera, same inventory, same hotbar selection, same health/battery, no errors in console.
6. Walk back to Biome 1 (a return trigger). The cracks you made should still glow exactly where you left them, and any items you dropped should still be on the ground.
7. Quit. Restart Unity. Play from Bootstrap. The save file at `Application.persistentDataPath/save.json` should auto-load you back into the scene you were in with everything restored.
