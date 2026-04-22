using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CraftingSystem : MonoBehaviour
{
    public static CraftingSystem Instance { get; private set; }

    [SerializeField] private CraftingDatabase craftingDatabase;

    public event Action<ItemData> OnCraftStarted;
    public event Action<ItemData> OnCraftCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
        if (craftingDatabase == null)
            Debug.LogError("[CraftingSystem] craftingDatabase is not assigned in the inspector!");
        else
            Debug.Log($"[CraftingSystem] Awake OK — database has {craftingDatabase.allCraftableItems.Count} items");
    }

    public bool CanCraft(ItemData item)
    {
        if (item == null || item.recipe == null || item.recipe.Count == 0) return false;
        foreach (var ingredient in item.recipe)
        {
            if (ingredient.itemData == null) continue;
            if (CountItemsWithData(ingredient.itemData) < ingredient.amount) return false;
        }
        return true;
    }

    // Returns false immediately if CanCraft fails. Otherwise starts the craft coroutine,
    // consumes ingredients after craftTime seconds, adds the result, and fires onComplete.
    public bool TryCraft(ItemData item, float craftTime, Action onComplete)
    {
        if (!CanCraft(item)) return false;

        // Capture specific instance IDs now so mid-craft inventory changes don't affect which
        // items get consumed (best-effort — TryRemoveItem fails silently if an item was removed).
        var toRemove = new List<string>();
        foreach (var ingredient in item.recipe)
        {
            var instances = GetItemsByData(ingredient.itemData);
            for (int i = 0; i < ingredient.amount; i++)
                toRemove.Add(instances[i].uniqueInstanceId);
        }

        StartCoroutine(CraftCoroutine(item, craftTime, toRemove, onComplete));
        return true;
    }

    private IEnumerator CraftCoroutine(ItemData item, float craftTime, List<string> toRemove, Action onComplete)
    {
        OnCraftStarted?.Invoke(item);
        yield return new WaitForSecondsRealtime(craftTime);

        foreach (var id in toRemove)
            InventorySystem.Instance.TryRemoveItem(id);

        var newItem = new InventoryItem(item);
        if (!InventorySystem.Instance.TryAddItem(newItem))
            Debug.LogWarning($"[CraftingSystem] Inventory full — could not deliver crafted item: {item.itemName}");

        OnCraftCompleted?.Invoke(item);
        onComplete?.Invoke();
    }

    // Returns items filtered to <= maxTier, tier != None, recipe non-empty.
    public List<ItemData> GetCraftableItemsForTier(CraftingTier maxTier)
    {
        var result = new List<ItemData>();
        if (craftingDatabase == null) { Debug.LogError("[CraftingSystem] GetCraftableItemsForTier: craftingDatabase is null!"); return result; }

        Debug.Log($"[CraftingSystem] GetCraftableItemsForTier({maxTier}) — scanning {craftingDatabase.allCraftableItems.Count} database entries");
        foreach (var item in craftingDatabase.allCraftableItems)
        {
            if (item == null) { Debug.LogWarning("[CraftingSystem] Null entry in CraftingDatabase — remove it from the list"); continue; }
            if (item.tier == CraftingTier.None) { Debug.Log($"[CraftingSystem]   SKIP {item.itemName} — tier=None"); continue; }
            if (item.recipe == null || item.recipe.Count == 0) { Debug.Log($"[CraftingSystem]   SKIP {item.itemName} — recipe empty"); continue; }
            if ((int)item.tier > (int)maxTier) { Debug.Log($"[CraftingSystem]   SKIP {item.itemName} — tier {item.tier} > crafter tier {maxTier}"); continue; }
            Debug.Log($"[CraftingSystem]   ADD  {item.itemName} (tier={item.tier}, ingredients={item.recipe.Count})");
            result.Add(item);
        }
        Debug.Log($"[CraftingSystem] GetCraftableItemsForTier result: {result.Count} items");
        return result;
    }

    public int CountItemsWithData(ItemData data)
    {
        int count = 0;
        foreach (var slot in InventorySystem.Instance.GetAllItems())
            if (slot != null && slot.data == data) count++;
        return count;
    }

    private List<InventoryItem> GetItemsByData(ItemData data)
    {
        var result = new List<InventoryItem>();
        foreach (var slot in InventorySystem.Instance.GetAllItems())
            if (slot != null && slot.data == data) result.Add(slot);
        return result;
    }
}
