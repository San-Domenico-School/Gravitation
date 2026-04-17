using System.Collections.Generic;
using UnityEngine;

public class ItemPickupHandler : MonoBehaviour
{
    private readonly List<WorldItem> itemsInRange = new List<WorldItem>();

    private void OnEnable()
    {
        WorldItem.OnItemInRange += HandleItemInRange;
        WorldItem.OnItemOutOfRange += HandleItemOutOfRange;
    }

    private void OnDisable()
    {
        WorldItem.OnItemInRange -= HandleItemInRange;
        WorldItem.OnItemOutOfRange -= HandleItemOutOfRange;
    }

    private void HandleItemInRange(WorldItem item) => itemsInRange.Add(item);

    private void HandleItemOutOfRange(WorldItem item) => itemsInRange.Remove(item);

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            WorldItem closest = GetClosest();
            closest?.TryPickup();
        }
    }

    private WorldItem GetClosest()
    {
        WorldItem closest = null;
        float minDist = float.MaxValue;
        for (int i = itemsInRange.Count - 1; i >= 0; i--)
        {
            if (itemsInRange[i] == null) { itemsInRange.RemoveAt(i); continue; }
            float d = itemsInRange[i].DistanceToPlayer();
            if (d < minDist) { minDist = d; closest = itemsInRange[i]; }
        }
        return closest;
    }

    public WorldItem GetClosestInRange() => GetClosest();
}
