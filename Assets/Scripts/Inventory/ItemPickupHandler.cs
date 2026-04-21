using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemPickupHandler : MonoBehaviour
{
    [SerializeField] private InputActionReference interactAction;

    private readonly List<WorldItem> itemsInRange = new List<WorldItem>();

    private void OnEnable()
    {
        WorldItem.OnItemInRange += HandleItemInRange;
        WorldItem.OnItemOutOfRange += HandleItemOutOfRange;
        interactAction.action.performed += OnInteract;
    }

    private void OnDisable()
    {
        WorldItem.OnItemInRange -= HandleItemInRange;
        WorldItem.OnItemOutOfRange -= HandleItemOutOfRange;
        interactAction.action.performed -= OnInteract;
    }

    private void HandleItemInRange(WorldItem item) => itemsInRange.Add(item);
    private void HandleItemOutOfRange(WorldItem item) => itemsInRange.Remove(item);

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        GetClosest()?.TryPickup();
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
