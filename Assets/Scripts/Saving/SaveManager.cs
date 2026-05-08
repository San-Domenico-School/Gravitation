using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that owns the in-memory <see cref="GameSaveData"/>, persists it to JSON on
/// <see cref="Application.persistentDataPath"/>, and hooks into Unity's scene lifecycle
/// to autosave on scene change.
///
/// Add ONE of these components to a bootstrap scene (or place it on a GameObject in your
/// first scene). It marks itself <c>DontDestroyOnLoad</c> and survives across scene loads.
///
/// Public API:
///   <c>SaveManager.Instance.SaveNow()</c> — capture current state and write to disk.
///   <c>SaveManager.Instance.LoadFromDisk()</c> — read disk file into memory and restore the saved scene.
///   <c>SaveManager.Instance.DeleteSave()</c> — wipe the save file (fresh start next launch).
///   <c>SaveManager.Instance.RegisterRuntimePlaced(GameObject)</c> — call after instantiating a runtime saveable so we know to track it.
/// </summary>
[DefaultExecutionOrder(-1000)] // run before most other scripts
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    [Header("File")]
    [Tooltip("Filename inside Application.persistentDataPath. Final path is logged on Awake.")]
    [SerializeField] private string saveFileName = "save.json";

    [Header("Behavior")]
    [Tooltip("Save when transitioning between scenes.")]
    [SerializeField] private bool autosaveOnSceneChange = true;
    [Tooltip("Save on application quit (PC builds + editor stop).")]
    [SerializeField] private bool autosaveOnQuit = true;
    [Tooltip("Save on application pause (mobile background, alt-tab on some platforms).")]
    [SerializeField] private bool autosaveOnPause = true;
    [Tooltip("If true, restoring a previously-visited scene will destroy and re-create all cracks/world-items/runtime-placed objects in that scene to match the save. Disable only if you have a reason.")]
    [SerializeField] private bool restoreSceneOnEntry = true;
    [Tooltip("Verbose logging.")]
    [SerializeField] private bool verbose = false;

    private GameSaveData current;
    private string saveFilePath;

    /// <summary>
    /// When true, the next sceneLoaded event will fully restore player position/inventory etc
    /// (called by LoadFromDisk). For normal scene transitions it stays false — the world
    /// state in the entered scene is restored, but the player keeps moving as the game intends.
    /// </summary>
    private bool pendingFreshLoad;

    /// <summary>Runtime-instantiated SaveableEntities tracked for persistence in the active scene.</summary>
    private readonly HashSet<SaveableEntity> trackedRuntimeEntities = new HashSet<SaveableEntity>();

    public string SaveFilePath => saveFilePath;
    public bool HasSaveFile => File.Exists(saveFilePath);
    public GameSaveData CurrentData => current;

    // ---------------------------------------------------------------------- lifecycle

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, saveFileName);
        Debug.Log($"[SaveManager] Save file: {saveFilePath}");

        // Load whatever is on disk into memory at startup. If nothing exists, start fresh.
        current = LoadDataFromDisk() ?? new GameSaveData();

        SceneManager.sceneLoaded += HandleSceneLoaded;
        SceneManager.sceneUnloaded += HandleSceneUnloaded;
    }

    private void Start()
    {
        // The very first scene's `sceneLoaded` event fires before this script's Awake, so
        // we won't have heard about it via HandleSceneLoaded. If we have a save AND the
        // user is launching directly into the scene the save was last in, apply the state
        // now (after all other Awakes have completed).
        if (current == null) return;
        string active = SceneManager.GetActiveScene().name;
        bool sameScene = !string.IsNullOrEmpty(current.currentSceneName) && current.currentSceneName == active;
        if (sameScene && HasSaveFile)
        {
            if (verbose) Debug.Log($"[SaveManager] Auto-restoring state on game start (scene '{active}' matches save).");
            if (restoreSceneOnEntry) ApplySceneState(active);
            ApplyGlobals();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneUnloaded -= HandleSceneUnloaded;
            Instance = null;
        }
    }

    private void OnApplicationQuit()
    {
        if (autosaveOnQuit) SaveNow();
    }

    private void OnApplicationPause(bool paused)
    {
        if (paused && autosaveOnPause) SaveNow();
    }

    // ---------------------------------------------------------------------- public API

    /// <summary>Capture all current state into <see cref="CurrentData"/> and write it to disk.</summary>
    public void SaveNow()
    {
        try
        {
            CapturePlayer();
            CaptureInventory();
            CaptureHotbar();
            CaptureScene(SceneManager.GetActiveScene().name);
            current.currentSceneName = SceneManager.GetActiveScene().name;
            current.savedAtUtc = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(current, prettyPrint: true);
            File.WriteAllText(saveFilePath, json);

            if (verbose) Debug.Log($"[SaveManager] Saved {json.Length} bytes to {saveFilePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] SaveNow failed: {e}");
        }
    }

    /// <summary>
    /// Read the save file from disk and load the saved scene. Player position/inventory/etc.
    /// will be restored once the scene finishes loading.
    /// </summary>
    public void LoadFromDisk()
    {
        var loaded = LoadDataFromDisk();
        if (loaded == null)
        {
            Debug.LogWarning("[SaveManager] LoadFromDisk: no save file found.");
            return;
        }
        current = loaded;
        pendingFreshLoad = true;

        string targetScene = current.currentSceneName;
        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogWarning("[SaveManager] LoadFromDisk: save has no currentSceneName — restoring into the active scene.");
            // Apply state to the current scene immediately.
            ApplySceneState(SceneManager.GetActiveScene().name);
            ApplyGlobals();
            pendingFreshLoad = false;
            return;
        }

        if (SceneManager.GetActiveScene().name == targetScene)
        {
            // Already in the right scene — just re-apply state.
            ApplySceneState(targetScene);
            ApplyGlobals();
            pendingFreshLoad = false;
        }
        else
        {
            SceneManager.LoadScene(targetScene);
        }
    }

    /// <summary>Wipe the on-disk save file. Does not clear in-memory state.</summary>
    public void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log($"[SaveManager] Deleted save file at {saveFilePath}");
        }
    }

    /// <summary>
    /// Wipe the on-disk save file AND reset the in-memory state to a blank slate.
    /// Useful from editor tools or a debug menu to guarantee a completely fresh start
    /// without restarting the editor / build.
    /// </summary>
    public void ClearSaveAndReset()
    {
        DeleteSave();
        current = new GameSaveData();
        Debug.Log("[SaveManager] In-memory save data cleared. Fresh start on next scene load.");
    }

    /// <summary>
    /// Call this immediately after instantiating a prefab that has a SaveableEntity with a
    /// <c>prefabId</c>. SaveManager will track it and persist its position + ISaveable state.
    /// </summary>
    public void RegisterRuntimePlaced(GameObject obj)
    {
        if (obj == null) return;
        var entity = obj.GetComponent<SaveableEntity>();
        if (entity == null)
        {
            Debug.LogWarning($"[SaveManager] RegisterRuntimePlaced: {obj.name} has no SaveableEntity component.");
            return;
        }
        if (string.IsNullOrEmpty(entity.PrefabId))
        {
            Debug.LogWarning($"[SaveManager] RegisterRuntimePlaced: {obj.name}'s SaveableEntity has no prefabId. Set it on the prefab.");
            return;
        }
        if (string.IsNullOrEmpty(entity.EntityGuid))
        {
            entity.AssignRuntimeGuid(Guid.NewGuid().ToString());
        }
        trackedRuntimeEntities.Add(entity);
    }

    /// <summary>
    /// Helper: instantiate from PrefabRegistry and register in one call.
    /// Returns the new GameObject or null if the prefab id is unknown.
    /// </summary>
    public GameObject SpawnPersistent(string prefabId, Vector3 position, Quaternion rotation, Transform parent = null)
    {
        var prefab = PrefabRegistry.Instance.GetPrefab(prefabId);
        if (prefab == null)
        {
            Debug.LogWarning($"[SaveManager] SpawnPersistent: prefab id '{prefabId}' not in PrefabRegistry.");
            return null;
        }
        var go = Instantiate(prefab, position, rotation, parent);
        RegisterRuntimePlaced(go);
        return go;
    }

    // ---------------------------------------------------------------------- scene events

    private void HandleSceneUnloaded(Scene scene)
    {
        if (!autosaveOnSceneChange) return;
        // Save the scene we're leaving. Player/inventory/hotbar are global and captured too.
        try
        {
            CapturePlayer();
            CaptureInventory();
            CaptureHotbar();
            CaptureScene(scene.name);
            current.currentSceneName = scene.name;
            current.savedAtUtc = DateTime.UtcNow.ToString("o");

            string json = JsonUtility.ToJson(current, prettyPrint: true);
            File.WriteAllText(saveFilePath, json);
            if (verbose) Debug.Log($"[SaveManager] Autosaved on unload of '{scene.name}'.");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Autosave on scene unload failed: {e}");
        }
        // Clear runtime-entity tracking — the next scene starts fresh.
        trackedRuntimeEntities.Clear();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Additive) return; // only single-scene loads

        // Restore scene state if we've previously visited.
        if (restoreSceneOnEntry) ApplySceneState(scene.name);

        if (pendingFreshLoad)
        {
            ApplyGlobals();
            pendingFreshLoad = false;
        }
    }

    // ---------------------------------------------------------------------- capture

    private void CapturePlayer()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            current.player.hasData = false;
            return;
        }

        current.player.hasData = true;
        current.player.position = player.transform.position;
        current.player.rotation = player.transform.rotation;

        var health = player.GetComponent<PlayerHealth>();
        if (health != null) current.player.health = health.CurrentHealth;

        var battery = player.GetComponentInChildren<GunBatterySystem>();
        if (battery != null)
        {
            current.player.batteryCharge = battery.CurrentCharge;
            current.player.batteryCellId = battery.CurrentCell != null ? battery.CurrentCell.name : "";
        }
    }

    private void CaptureInventory()
    {
        var inv = current.inventory.items;
        inv.Clear();
        if (InventorySystem.Instance == null) return;

        var all = InventorySystem.Instance.GetAllItems();
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null) continue;
            inv.Add(new InventoryItemSaveData
            {
                slotIndex = i,
                itemId = all[i].data != null ? all[i].data.SaveId : "",
                instanceId = all[i].uniqueInstanceId
            });
        }
    }

    private void CaptureHotbar()
    {
        var hotbar = HotbarSystem.Instance;
        current.hotbar.hotbarInstanceIds.Clear();
        if (hotbar == null) return;

        for (int i = 0; i < hotbar.SlotCount; i++)
        {
            var item = hotbar.GetHotbarItem(i);
            current.hotbar.hotbarInstanceIds.Add(item != null ? item.uniqueInstanceId : "");
        }
        current.hotbar.selectedIndex = hotbar.SelectedIndex;
    }

    private void CaptureScene(string sceneName)
    {
        var sd = current.GetOrCreateScene(sceneName);
        sd.hasBeenVisited = true;
        sd.cracks.Clear();
        sd.worldItems.Clear();
        sd.placedObjects.Clear();
        sd.sceneEntities.Clear();

        // Cracks
        if (CrackSpawner.Instance != null)
        {
            foreach (var c in CrackSpawner.Instance.LiveCracks)
            {
                if (c == null) continue;
                sd.cracks.Add(new CrackSaveData
                {
                    position = c.transform.position,
                    rotation = c.transform.rotation,
                    resourceItemId = c.Resource != null ? c.Resource.SaveId : "",
                    glowColor = c.GlowColor,
                    biome = c.Biome,
                    isActive = c.IsActive,
                    regenRemaining = c.RegenRemaining
                });
            }
        }

        // WorldItems on the ground (but skip ones inside the player's pickup range that are
        // attached to a SaveableEntity — those are saved through the entity path).
        foreach (var wi in UnityEngine.Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None))
        {
            if (wi == null || wi.itemData == null) continue;
            if (wi.GetComponent<SaveableEntity>() != null) continue; // handled by the entity path
            sd.worldItems.Add(new WorldItemSaveData
            {
                position = wi.transform.position,
                rotation = wi.transform.rotation,
                itemId = wi.itemData.SaveId
            });
        }

        // SaveableEntities — split into runtime-spawned (placedObjects) and scene-baked (sceneEntities).
        foreach (var ent in UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None))
        {
            if (ent == null || ent.markedDestroyed) continue;
            if (ent.IsRuntimeInstance)
            {
                sd.placedObjects.Add(new PlacedObjectSaveData
                {
                    prefabId = ent.PrefabId,
                    instanceGuid = ent.EntityGuid,
                    position = ent.transform.position,
                    rotation = ent.transform.rotation,
                    customJson = ent.CaptureState()
                });
            }
            else
            {
                if (string.IsNullOrEmpty(ent.EntityGuid)) continue; // skip unconfigured scene entities
                sd.sceneEntities.Add(new SceneEntitySaveData
                {
                    entityGuid = ent.EntityGuid,
                    customJson = ent.CaptureState()
                });
            }
        }
    }

    // ---------------------------------------------------------------------- restore

    /// <summary>
    /// Apply per-scene saved state to the active scene. Safe to call even if the scene
    /// has never been saved (it does nothing in that case).
    /// </summary>
    public void ApplySceneState(string sceneName)
    {
        var sd = current.FindScene(sceneName);
        if (sd == null || !sd.hasBeenVisited) return; // first visit — leave authored content alone

        // Wipe live respawnables, then apply saved state.

        // Cracks
        if (CrackSpawner.Instance != null)
        {
            CrackSpawner.Instance.DestroyAllLiveCracks();
            foreach (var cs in sd.cracks)
            {
                ItemData resource = ItemDatabase.Instance.GetById(cs.resourceItemId);
                CrackSpawner.Instance.SpawnFromSave(cs.position, cs.rotation, resource, cs.glowColor, cs.biome, cs.isActive, cs.regenRemaining);
            }
        }

        // WorldItems
        foreach (var wi in UnityEngine.Object.FindObjectsByType<WorldItem>(FindObjectsSortMode.None))
        {
            if (wi == null) continue;
            if (wi.GetComponent<SaveableEntity>() != null) continue; // entity-managed
            UnityEngine.Object.Destroy(wi.gameObject);
        }
        foreach (var wis in sd.worldItems)
        {
            ItemData itemData = ItemDatabase.Instance.GetById(wis.itemId);
            if (itemData == null || itemData.worldPrefab == null) continue;
            var go = UnityEngine.Object.Instantiate(itemData.worldPrefab, wis.position, wis.rotation);
            var wi = go.GetComponent<WorldItem>();
            if (wi != null) wi.itemData = itemData;
        }

        // SaveableEntities — handle runtime-spawned and scene-baked separately.
        var allEntities = UnityEngine.Object.FindObjectsByType<SaveableEntity>(FindObjectsSortMode.None);

        // 1. Destroy all runtime-spawned entities currently in the scene; we'll respawn from save.
        foreach (var ent in allEntities)
        {
            if (ent != null && ent.IsRuntimeInstance) UnityEngine.Object.Destroy(ent.gameObject);
        }
        trackedRuntimeEntities.Clear();
        foreach (var po in sd.placedObjects)
        {
            var prefab = PrefabRegistry.Instance.GetPrefab(po.prefabId);
            if (prefab == null)
            {
                Debug.LogWarning($"[SaveManager] Cannot restore placed object: prefab id '{po.prefabId}' missing from PrefabRegistry.");
                continue;
            }
            var go = UnityEngine.Object.Instantiate(prefab, po.position, po.rotation);
            var ent = go.GetComponent<SaveableEntity>();
            if (ent != null)
            {
                ent.AssignRuntimeGuid(po.instanceGuid);
                trackedRuntimeEntities.Add(ent);
                ent.RestoreState(po.customJson);
            }
        }

        // 2. Scene-baked entities — match by GUID and restore state.
        foreach (var ent in allEntities)
        {
            if (ent == null || ent.IsRuntimeInstance) continue;
            if (string.IsNullOrEmpty(ent.EntityGuid)) continue;
            string saved = FindSceneEntityState(sd, ent.EntityGuid);
            if (saved != null) ent.RestoreState(saved);
        }
    }

    private static string FindSceneEntityState(SceneSaveData sd, string guid)
    {
        for (int i = 0; i < sd.sceneEntities.Count; i++)
        {
            if (sd.sceneEntities[i].entityGuid == guid) return sd.sceneEntities[i].customJson;
        }
        return null;
    }

    /// <summary>Apply the global (non-scene) bits of the save: player pose/health/battery, inventory, hotbar.</summary>
    public void ApplyGlobals()
    {
        // Inventory
        if (InventorySystem.Instance != null)
        {
            var snapshot = new InventoryItem[InventorySystem.Instance.SlotCount];
            foreach (var entry in current.inventory.items)
            {
                if (entry.slotIndex < 0 || entry.slotIndex >= snapshot.Length) continue;
                var data = ItemDatabase.Instance.GetById(entry.itemId);
                if (data == null) continue;
                var item = new InventoryItem(data);
                item.uniqueInstanceId = entry.instanceId;
                snapshot[entry.slotIndex] = item;
            }
            InventorySystem.Instance.RestoreFromSnapshot(snapshot);
        }

        // Hotbar — must run after inventory restore so we can re-link by instance ID.
        if (HotbarSystem.Instance != null && InventorySystem.Instance != null)
        {
            var hb = new InventoryItem[HotbarSystem.Instance.SlotCount];
            var savedIds = current.hotbar.hotbarInstanceIds;
            for (int i = 0; i < hb.Length && i < savedIds.Count; i++)
            {
                var id = savedIds[i];
                if (string.IsNullOrEmpty(id)) continue;
                hb[i] = FindInventoryItemByInstanceId(id);
            }
            HotbarSystem.Instance.RestoreFromSnapshot(hb, current.hotbar.selectedIndex);
        }

        // Player pose / health / battery
        if (current.player.hasData)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.SetPositionAndRotation(current.player.position, current.player.rotation);
                var rb = player.GetComponent<Rigidbody>();
                if (rb != null) { rb.linearVelocity = Vector3.zero; rb.angularVelocity = Vector3.zero; }

                var hp = player.GetComponent<PlayerHealth>();
                if (hp != null) hp.SetCurrentHealth(current.player.health);

                var battery = player.GetComponentInChildren<GunBatterySystem>();
                if (battery != null)
                {
                    if (!string.IsNullOrEmpty(current.player.batteryCellId))
                    {
                        var cell = Resources.Load<GravitonCell>(current.player.batteryCellId);
                        if (cell != null) battery.SwapCell(cell);
                    }
                    battery.SetCharge(current.player.batteryCharge);
                }
            }
        }
    }

    private static InventoryItem FindInventoryItemByInstanceId(string instanceId)
    {
        var all = InventorySystem.Instance.GetAllItems();
        foreach (var it in all)
        {
            if (it != null && it.uniqueInstanceId == instanceId) return it;
        }
        return null;
    }

    // ---------------------------------------------------------------------- disk

    private GameSaveData LoadDataFromDisk()
    {
        if (!File.Exists(saveFilePath)) return null;
        try
        {
            string json = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<GameSaveData>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Failed to read save file: {e}");
            return null;
        }
    }
}
