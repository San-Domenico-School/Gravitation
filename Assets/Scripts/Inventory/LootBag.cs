using System.Collections.Generic;
using UnityEngine;

public class LootBag : MonoBehaviour
{
    private List<InventoryItem> contents = new List<InventoryItem>();
    private Transform playerTransform;
    private bool isInRange;
    private float pickupRange = 2.5f;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    public void Initialize(List<InventoryItem> items)
    {
        contents = new List<InventoryItem>(items);
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool nowInRange = dist <= pickupRange;

        if (nowInRange != isInRange)
        {
            isInRange = nowInRange;
            if (isInRange)
                PickupPromptUI.ShowMessage("Press E to recover loot bag");
        }

        if (isInRange && Input.GetKeyDown(KeyCode.E))
            TryRecoverItems();
    }

    private void TryRecoverItems()
    {
        var remaining = new List<InventoryItem>();

        foreach (var item in contents)
        {
            if (!InventorySystem.Instance.TryAddItem(item))
                remaining.Add(item);
        }

        contents = remaining;

        if (contents.Count == 0)
        {
            Destroy(gameObject);
        }
        else
        {
            PickupPromptUI.ShowMessage($"Inventory full — {contents.Count} items remain in bag.");
        }
    }
}
