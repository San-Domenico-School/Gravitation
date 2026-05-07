using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime lookup table that maps stable string IDs back to <see cref="ItemData"/> assets
/// when loading a save file. Two ways to populate it:
///
///   1. Drop ItemData assets into the <c>entries</c> list in the inspector. (Authoritative.)
///   2. Drop the asset into a <c>Resources</c> folder. ItemDatabase.Instance auto-scans
///      <c>Resources.LoadAll&lt;ItemData&gt;("")</c> at startup and merges anything it finds.
///
/// Place this asset at <c>Assets/Resources/ItemDatabase.asset</c> so the save system can
/// load it via <c>Resources.Load&lt;ItemDatabase&gt;("ItemDatabase")</c>.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Gravitas/Save System/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Tooltip("Authoritative list of ItemData assets known to the save system.")]
    [SerializeField] private List<ItemData> entries = new List<ItemData>();

    [Tooltip("If true, also scans Resources/ for any ItemData assets at runtime and adds them automatically.")]
    [SerializeField] private bool autoScanResources = true;

    private static ItemDatabase instance;
    private Dictionary<string, ItemData> idToItem;

    /// <summary>Lazily-loaded singleton. Loads from Resources/ItemDatabase.</summary>
    public static ItemDatabase Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = Resources.Load<ItemDatabase>("ItemDatabase");
            if (instance == null)
            {
                Debug.LogWarning("[ItemDatabase] No ItemDatabase asset found at Resources/ItemDatabase.asset. Creating a transient empty database. Save/load of items will not work until you create one.");
                instance = CreateInstance<ItemDatabase>();
            }
            instance.BuildLookup();
            return instance;
        }
    }

    private void BuildLookup()
    {
        idToItem = new Dictionary<string, ItemData>(64);

        foreach (var item in entries)
        {
            if (item == null) continue;
            idToItem[item.SaveId] = item;
        }

        if (autoScanResources)
        {
            var found = Resources.LoadAll<ItemData>("");
            foreach (var item in found)
            {
                if (item == null) continue;
                if (!idToItem.ContainsKey(item.SaveId))
                    idToItem[item.SaveId] = item;
            }
        }
    }

    /// <summary>Returns the ItemData with the given save ID, or null.</summary>
    public ItemData GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;
        if (idToItem == null) BuildLookup();
        idToItem.TryGetValue(id, out var item);
        return item;
    }

#if UNITY_EDITOR
    [ContextMenu("Refresh from Project")]
    private void RefreshFromProject()
    {
        entries.Clear();
        var guids = UnityEditor.AssetDatabase.FindAssets("t:ItemData");
        foreach (var guid in guids)
        {
            var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            var item = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemData>(path);
            if (item != null) entries.Add(item);
        }
        UnityEditor.EditorUtility.SetDirty(this);
        Debug.Log($"[ItemDatabase] Refreshed: {entries.Count} ItemData assets registered.");
    }
#endif
}
