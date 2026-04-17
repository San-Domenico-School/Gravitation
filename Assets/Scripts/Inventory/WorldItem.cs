using System;
using UnityEngine;

public class WorldItem : MonoBehaviour
{
    [SerializeField] public ItemData itemData;
    [SerializeField] private float pickupRange = 2.5f;

    public static event Action<WorldItem> OnItemInRange;
    public static event Action<WorldItem> OnItemOutOfRange;

    private Transform playerTransform;
    private bool isInRange;

    private void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void Update()
    {
        if (playerTransform == null) return;

        float dist = Vector3.Distance(transform.position, playerTransform.position);
        bool nowInRange = dist <= pickupRange;

        if (nowInRange && !isInRange)
        {
            isInRange = true;
            OnItemInRange?.Invoke(this);
        }
        else if (!nowInRange && isInRange)
        {
            isInRange = false;
            OnItemOutOfRange?.Invoke(this);
        }
    }

    private void OnDestroy()
    {
        if (isInRange) OnItemOutOfRange?.Invoke(this);
    }

    public float DistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }

    public void TryPickup()
    {
        if (itemData == null) return;

        if (itemData.isUnique && InventorySystem.Instance.HasItemOfType(itemData.itemType))
        {
            PickupPromptUI.ShowMessage("Already carrying this unique item.");
            return;
        }

        var inventoryItem = new InventoryItem(itemData);
        if (InventorySystem.Instance.TryAddItem(inventoryItem))
        {
            isInRange = false;
            OnItemOutOfRange?.Invoke(this);
            Destroy(gameObject);
        }
        else
        {
            PickupPromptUI.ShowMessage("Inventory full.");
        }
    }
}
