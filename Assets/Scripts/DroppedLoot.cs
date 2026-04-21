using System.Collections.Generic;
using UnityEngine;

public class DroppedLoot : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Prefab used to spawn the loot bag. Must have a LootBag component.")]
    private GameObject lootBagPrefab;

    public void SpawnLootBag(Vector3 position)
    {
        if (lootBagPrefab == null)
        {
            Debug.LogWarning("LootBag prefab not assigned in DroppedLoot. Skipping loot drop.");
            return;
        }

        InventoryItem[] allSlots = InventorySystem.Instance.GetAllItems();
        var itemsToTransfer = new List<InventoryItem>();
        foreach (var slot in allSlots)
        {
            if (slot != null) itemsToTransfer.Add(slot);
        }

        InventorySystem.Instance.ClearAllItems();
        HotbarSystem.Instance.ClearAllHotbar();

        GameObject lootBagGO = Instantiate(lootBagPrefab, position, Quaternion.identity);

        var lootBag = lootBagGO.GetComponent<LootBag>();
        if (lootBag != null)
        {
            lootBag.Initialize(itemsToTransfer);
        }
        else
        {
            Debug.LogWarning("LootBag prefab is missing a LootBag component.");
        }
    }
}
