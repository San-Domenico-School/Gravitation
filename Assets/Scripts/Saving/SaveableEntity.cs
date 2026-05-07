using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Attach to any GameObject that needs to persist across saves.
///
/// Two flavors of usage:
///   1. SCENE-BAKED: drop the component on a GameObject in the scene. Click "Generate
///      Stable GUID" in the inspector once. The GUID is baked into the scene file and
///      identifies this exact object in the save file.
///
///   2. RUNTIME-INSTANTIATED: spawned from a prefab via <see cref="PrefabRegistry"/>.
///      Set <c>prefabId</c> on the prefab. SaveManager assigns a fresh runtime GUID
///      when it's first registered and uses it to track the instance across saves.
///
/// On the same GameObject, attach any number of components implementing <see cref="ISaveable"/>.
/// SaveableEntity collects their blobs into a single string for the save file.
/// </summary>
[DisallowMultipleComponent]
public class SaveableEntity : MonoBehaviour
{
    [Tooltip("Stable GUID for this instance. For scene-baked entities, click 'Generate Stable GUID' in the inspector once. For runtime-instantiated objects, leave empty — SaveManager assigns one.")]
    [SerializeField] private string entityGuid;

    [Tooltip("If this entity is spawned from a prefab at runtime, set this to the prefab's PrefabRegistry ID. Leave empty for scene-baked entities.")]
    [SerializeField] private string prefabId;

    public string EntityGuid => entityGuid;
    public string PrefabId => prefabId;
    public bool IsRuntimeInstance => !string.IsNullOrEmpty(prefabId);

    /// <summary>True if this entity has been removed/picked up/destroyed and should not be respawned.</summary>
    [HideInInspector] public bool markedDestroyed;

    /// <summary>Assign a fresh runtime GUID. Called by SaveManager when spawning from PrefabRegistry.</summary>
    public void AssignRuntimeGuid(string guid)
    {
        entityGuid = guid;
    }

    /// <summary>Snapshot all ISaveable components on this object into a single JSON blob.</summary>
    public string CaptureState()
    {
        var snapshot = new EntityStateSnapshot();
        foreach (var saveable in GetComponents<ISaveable>())
        {
            string s = saveable.SaveState();
            if (s == null) s = string.Empty;
            snapshot.entries.Add(new EntityStateSnapshot.Entry
            {
                typeName = saveable.GetType().FullName,
                blob = s
            });
        }
        return JsonUtility.ToJson(snapshot);
    }

    /// <summary>Restore all ISaveable components from a JSON blob produced by CaptureState.</summary>
    public void RestoreState(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        EntityStateSnapshot snapshot;
        try { snapshot = JsonUtility.FromJson<EntityStateSnapshot>(json); }
        catch (Exception e) { Debug.LogWarning($"[SaveableEntity {name}] Failed to parse state: {e.Message}"); return; }
        if (snapshot == null) return;

        var components = GetComponents<ISaveable>();
        foreach (var entry in snapshot.entries)
        {
            foreach (var c in components)
            {
                if (c.GetType().FullName == entry.typeName)
                {
                    try { c.LoadState(entry.blob); }
                    catch (Exception e) { Debug.LogWarning($"[SaveableEntity {name}] {c.GetType().Name}.LoadState threw: {e.Message}"); }
                    break;
                }
            }
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Generate Stable GUID")]
    private void GenerateStableGuid()
    {
        if (!string.IsNullOrEmpty(entityGuid))
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Replace existing GUID?", $"This entity already has a GUID:\n{entityGuid}\n\nReplacing it will break any existing save files referencing this object.", "Replace", "Cancel"))
                return;
        }
        entityGuid = Guid.NewGuid().ToString();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif

    [Serializable]
    private class EntityStateSnapshot
    {
        public List<Entry> entries = new List<Entry>();

        [Serializable]
        public class Entry
        {
            public string typeName;
            public string blob;
        }
    }
}
