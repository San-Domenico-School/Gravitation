using UnityEngine;

/// <summary>
/// Stubbed loot drop handler for player death. Spawns a loot bag at the death location.
/// </summary>
public class DroppedLoot : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Prefab used to spawn the loot bag. Leave unassigned until the LootBag prefab exists.")]
    private GameObject lootBagPrefab;

    public void SpawnLootBag(Vector3 position)
    {
        if (lootBagPrefab == null)
        {
            Debug.LogWarning("LootBag prefab not assigned in DroppedLoot. Skipping loot drop.");
            return;
        }

        GameObject lootBag = Instantiate(lootBagPrefab, position, Quaternion.identity);
        if (lootBag.GetComponent<Rigidbody>() == null)
        {
            lootBag.AddComponent<Rigidbody>();
        }

        // TODO: When inventory system is built, transfer player's held items into this loot bag on spawn.
    }
}
