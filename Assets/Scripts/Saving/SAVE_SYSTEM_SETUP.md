# Save System Setup

A scene-aware save system for Gravitation. Persists everything that matters across scene transitions and across game sessions: player state, inventory, hotbar, cracks the player makes, world items on the ground, and runtime-placed objects like crafters (and future storage chests).

## Architecture at a glance

| File | Responsibility |
|---|---|
| `SaveManager.cs` | Singleton orchestrator. Owns the in-memory `GameSaveData`, reads/writes JSON, hooks scene events. |
| `SaveData.cs` | All `[Serializable]` data classes for the save file. |
| `ItemDatabase.cs` | Resolves `ItemData` ScriptableObjects by stable string ID at runtime. |
| `PrefabRegistry.cs` | Resolves prefabs (crafters, chests) by stable string ID at runtime. |
| `SaveableEntity.cs` | MonoBehaviour added to any object that needs to persist. Holds a stable GUID and routes save/load to all `ISaveable` components on the same object. |
| `ISaveable.cs` | Interface for components that contribute custom JSON state (e.g., a future StorageChest). |
| `SaveHotkeys.cs` | F5 = save, F9 = load, Shift+Delete = wipe save file. |

## One-time scene setup

1. **Create the SaveManager.** In your bootstrap scene (or first gameplay scene), make an empty GameObject named `SaveManager`. Add the `SaveManager` and `SaveHotkeys` components.

2. **Create the ItemDatabase asset.** `Assets > Create > Gravitas > Save System > Item Database`, save it as `Assets/Resources/ItemDatabase.asset`. Click `⋮ > Refresh from Project` on the asset to auto-populate it with every `ItemData` in the project.

3. **Create the PrefabRegistry asset.** `Assets > Create > Gravitas > Save System > Prefab Registry`, save it as `Assets/Resources/PrefabRegistry.asset`. For each placed-object prefab (crafters, future chests), add an entry: a stable string ID (`"crafter_t1"`, `"chest_basic"`) and the prefab.

4. **Tag your player.** The save system finds the player via `GameObject.FindGameObjectWithTag("Player")`, which is already how `WorldItem` and `CrafterInteractable` find it.

The save file itself lives at `Application.persistentDataPath/save.json`. The exact path is logged by `SaveManager` on Awake.

## What gets saved automatically

Out of the box, no extra component changes are needed — these are saved by scanning the scene:

- **Cracks**: All live `ResourceCrack` instances spawned by the gravity pulse. Position, resource, glow color, biome, and remaining regen time are preserved.
- **WorldItems on the ground**: Anything with a `WorldItem` component and an assigned `ItemData`. These are destroyed and re-instantiated from the item's `worldPrefab` when the scene reloads.
- **Inventory + Hotbar**: All slots and the selected hotbar slot.
- **Player state**: Position, rotation, current health, battery charge, and equipped GravitonCell.

## Persisting placed crafters / chests

Any prefab the player can place at runtime needs three things:

1. A `SaveableEntity` component on the prefab root.
2. The `prefabId` field on that `SaveableEntity` set to a string that exists in the `PrefabRegistry`.
3. The placement code calls `SaveManager.Instance.RegisterRuntimePlaced(go)` (or, simpler, uses `SaveManager.Instance.SpawnPersistent(prefabId, pos, rot)` in place of `Instantiate`).

That's it for things with no internal state. For storage chests with contents, see the next section.

## Building a future StorageChest with persistent contents

When you add storage chests, this is the recipe:

```csharp
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SaveableEntity))]
public class StorageChest : MonoBehaviour, ISaveable
{
    private List<InventoryItem> contents = new List<InventoryItem>();

    public bool TryAdd(InventoryItem item) { contents.Add(item); return true; }
    // ... your chest UI logic ...

    [System.Serializable]
    private class ChestSaveBlob
    {
        public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();
    }

    public string SaveState()
    {
        var blob = new ChestSaveBlob();
        for (int i = 0; i < contents.Count; i++)
        {
            blob.items.Add(new InventoryItemSaveData
            {
                slotIndex = i,
                itemId = contents[i].data.SaveId,
                instanceId = contents[i].uniqueInstanceId,
            });
        }
        return JsonUtility.ToJson(blob);
    }

    public void LoadState(string state)
    {
        contents.Clear();
        if (string.IsNullOrEmpty(state)) return;
        var blob = JsonUtility.FromJson<ChestSaveBlob>(state);
        foreach (var entry in blob.items)
        {
            var data = ItemDatabase.Instance.GetById(entry.itemId);
            if (data == null) continue;
            var it = new InventoryItem(data);
            it.uniqueInstanceId = entry.instanceId;
            contents.Add(it);
        }
    }
}
```

Make a `StorageChest` prefab. Add `SaveableEntity` to its root and set `prefabId = "chest_basic"`. Add `"chest_basic" → StorageChest prefab` to the `PrefabRegistry`. Spawn via `SaveManager.Instance.SpawnPersistent("chest_basic", pos, rot)`. Done — the contents persist forever.

## Persisting authored (pre-placed) scene objects

If you want a chest or crafter that lives directly in a scene (not spawned at runtime):

1. Add `SaveableEntity` to the GameObject in the scene. Leave `prefabId` empty.
2. In the inspector, click `⋮ > Generate Stable GUID`. This bakes a unique GUID into the scene file.
3. Add any `ISaveable` component (e.g., `StorageChest` from above) to the same GameObject.

The save system identifies authored entities by their baked GUID and reapplies their state, but never destroys/recreates them — the scene file keeps providing the GameObject.

## When saves happen

- **Autosave on scene change** — when `SceneManager.LoadScene(...)` or its async variants is called, the leaving scene's state is captured.
- **Autosave on application quit** (PC builds + editor stop).
- **Autosave on application pause** (mobile background, alt-tab on some platforms).
- **Manual save**: `F5` (configurable in `SaveHotkeys`), or call `SaveManager.Instance.SaveNow()` from your pause menu.
- **Manual load**: `F9`, or `SaveManager.Instance.LoadFromDisk()`.

## Wiping a save during testing

- Hold `Shift` and press `Delete` in-game.
- Or call `SaveManager.Instance.DeleteSave()`.
- Or just delete the file at `Application.persistentDataPath/save.json` (the exact path is logged at startup).

## Notes / gotchas

- `BiomeType` is serialized as its integer value. Reordering the enum will break old saves.
- `ItemData.SaveId` defaults to the asset's name. Set the explicit `saveId` field in the inspector before renaming an asset if you want save compatibility.
- `JsonUtility` does not support `Dictionary` — that's why every map in `SaveData` is a `List` keyed by name.
- The save system only cares about objects in the *active* scene. If you use additive scene loading, only the active scene is captured.
- The first time a scene is entered with a save loaded, no destruction happens — authored content runs as designed. Once the scene is saved (i.e., on its first scene-change event), subsequent re-entries will be fully restored from the save.
