using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Registry of prefab-id → prefab mappings. Required for runtime-instantiated saveables
/// (placed crafters, future storage chests, dropped loot bags, etc) so the SaveManager
/// knows what to spawn when restoring a save.
///
/// Place this asset at <c>Assets/Resources/PrefabRegistry.asset</c> so the save system
/// can load it via <c>Resources.Load&lt;PrefabRegistry&gt;("PrefabRegistry")</c>.
///
/// Each entry's prefab must have a <see cref="SaveableEntity"/> on its root with a
/// matching <c>prefabId</c> string.
/// </summary>
[CreateAssetMenu(fileName = "PrefabRegistry", menuName = "Gravitas/Save System/Prefab Registry")]
public class PrefabRegistry : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        [Tooltip("Stable string ID. Must match the prefab's SaveableEntity.prefabId.")]
        public string prefabId;
        [Tooltip("Prefab to instantiate. Must have a SaveableEntity on its root.")]
        public GameObject prefab;
    }

    [SerializeField] private List<Entry> entries = new List<Entry>();

    private static PrefabRegistry instance;
    private Dictionary<string, GameObject> idToPrefab;

    public static PrefabRegistry Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = Resources.Load<PrefabRegistry>("PrefabRegistry");
            if (instance == null)
            {
                Debug.LogWarning("[PrefabRegistry] No PrefabRegistry asset at Resources/PrefabRegistry.asset. Placed objects (crafters, chests, etc) will not be restored. Create one via Create > Gravitas > Save System > Prefab Registry.");
                instance = CreateInstance<PrefabRegistry>();
            }
            instance.BuildLookup();
            return instance;
        }
    }

    private void BuildLookup()
    {
        idToPrefab = new Dictionary<string, GameObject>(entries.Count);
        foreach (var e in entries)
        {
            if (string.IsNullOrEmpty(e.prefabId) || e.prefab == null) continue;
            idToPrefab[e.prefabId] = e.prefab;
        }
    }

    public GameObject GetPrefab(string prefabId)
    {
        if (string.IsNullOrEmpty(prefabId)) return null;
        if (idToPrefab == null) BuildLookup();
        idToPrefab.TryGetValue(prefabId, out var prefab);
        return prefab;
    }
}
