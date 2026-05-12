using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Top-level save file. Serialized with Unity's JsonUtility, so all sub-types must be
/// [Serializable] classes/structs of plain fields and Lists. No Dictionaries.
/// </summary>
[Serializable]
public class GameSaveData
{
    /// <summary>Bumped whenever the schema changes incompatibly.</summary>
    public string version = "1";

    /// <summary>Wall-clock time the save was written, ISO-8601.</summary>
    public string savedAtUtc;

    /// <summary>Scene the player was in when saved. Used by Load() to know what to reload.</summary>
    public string currentSceneName;

    /// <summary>Player position/rotation/health/battery state.</summary>
    public PlayerSaveData player = new PlayerSaveData();

    /// <summary>Inventory contents (global, not per-scene).</summary>
    public InventorySaveData inventory = new InventorySaveData();

    /// <summary>Hotbar contents + selection (global).</summary>
    public HotbarSaveData hotbar = new HotbarSaveData();

    /// <summary>
    /// Per-scene state. We do not use Dictionary because JsonUtility doesn't support
    /// dictionaries. Looked up by sceneName at runtime.
    /// </summary>
    public List<SceneSaveData> scenes = new List<SceneSaveData>();

    /// <summary>Returns the SceneSaveData entry for the given scene, creating it if absent.</summary>
    public SceneSaveData GetOrCreateScene(string sceneName)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].sceneName == sceneName) return scenes[i];
        }
        var fresh = new SceneSaveData { sceneName = sceneName };
        scenes.Add(fresh);
        return fresh;
    }

    /// <summary>Returns the SceneSaveData entry for the given scene, or null if it has never been saved.</summary>
    public SceneSaveData FindScene(string sceneName)
    {
        for (int i = 0; i < scenes.Count; i++)
        {
            if (scenes[i].sceneName == sceneName) return scenes[i];
        }
        return null;
    }
}

[Serializable]
public class PlayerSaveData
{
    public bool hasData;
    public Vector3 position;
    public Quaternion rotation;
    public float health;
    public float batteryCharge;
    /// <summary>Asset name of the GravitonCell currently equipped. Looked up via Resources.Load on restore.</summary>
    public string batteryCellId;
}

[Serializable]
public class InventorySaveData
{
    public List<InventoryItemSaveData> items = new List<InventoryItemSaveData>();
}

[Serializable]
public class InventoryItemSaveData
{
    /// <summary>Slot 0..47.</summary>
    public int slotIndex;
    /// <summary>ItemData.SaveId.</summary>
    public string itemId;
    /// <summary>InventoryItem.uniqueInstanceId — preserved so the hotbar can re-link.</summary>
    public string instanceId;
}

[Serializable]
public class HotbarSaveData
{
    public int selectedIndex;
    /// <summary>5 instance IDs (or empty string for empty slot). Indices are hotbar slots 0..4.</summary>
    public List<string> hotbarInstanceIds = new List<string>();
}

/// <summary>
/// All persistent state for a single scene. Re-applied when the scene loads.
/// </summary>
[Serializable]
public class SceneSaveData
{
    public string sceneName;

    /// <summary>True once we've ever visited the scene. On first entry, we leave the
    /// scene's authored content alone. On re-entry, we apply the saved deltas.</summary>
    public bool hasBeenVisited;

    /// <summary>Cracks created by the player's gravity pulse.</summary>
    public List<CrackSaveData> cracks = new List<CrackSaveData>();

    /// <summary>WorldItems (dropped loot, picked-up state, etc.) currently lying in the scene.</summary>
    public List<WorldItemSaveData> worldItems = new List<WorldItemSaveData>();

    /// <summary>Crafters, future storage chests, anything the player placed at runtime.</summary>
    public List<PlacedObjectSaveData> placedObjects = new List<PlacedObjectSaveData>();

    /// <summary>Pre-placed scene objects with a SaveableEntity component (e.g., authored chests).</summary>
    public List<SceneEntitySaveData> sceneEntities = new List<SceneEntitySaveData>();

    /// <summary>
    /// GUIDs of scene-authored SaveableEntities that have been destroyed during play
    /// (e.g., a scene-placed WorldItem that the player picked up). These entities will
    /// be destroyed on scene load so they stay gone. Accumulates over time and is never
    /// cleared by CaptureScene.
    /// </summary>
    public List<string> removedAuthoredEntityGuids = new List<string>();
}

[Serializable]
public class CrackSaveData
{
    public Vector3 position;
    public Quaternion rotation;
    /// <summary>ItemData.SaveId of the resource the crack yields.</summary>
    public string resourceItemId;
    public Color glowColor;
    public BiomeType biome;
    public bool isActive;
    /// <summary>Seconds remaining on the regen timer when saved (0 if active).</summary>
    public float regenRemaining;
}

[Serializable]
public class WorldItemSaveData
{
    public Vector3 position;
    public Quaternion rotation;
    /// <summary>ItemData.SaveId — instantiated from ItemData.worldPrefab on load.</summary>
    public string itemId;
}

/// <summary>
/// A runtime-instantiated object (placed crafter, dropped chest, etc).
/// </summary>
[Serializable]
public class PlacedObjectSaveData
{
    /// <summary>Lookup key in PrefabRegistry.</summary>
    public string prefabId;
    /// <summary>Stable per-instance GUID. Preserved across saves so persistent state stays attached.</summary>
    public string instanceGuid;
    public Vector3 position;
    public Quaternion rotation;
    /// <summary>Free-form JSON blob written by the prefab's ISaveable component (e.g., chest contents).</summary>
    public string customJson;
}

/// <summary>
/// State for a SaveableEntity that lives in the scene from authoring (not instantiated at runtime).
/// Identified by its scene-baked GUID rather than instantiated by prefab ID.
/// </summary>
[Serializable]
public class SceneEntitySaveData
{
    public string entityGuid;
    public string customJson;
}
