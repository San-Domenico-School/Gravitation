using System;
using System.Collections.Generic;
using UnityEngine;

public class InventorySystem : MonoBehaviour
{
    public static InventorySystem Instance { get; private set; }

    private InventoryItem[] slots = new InventoryItem[48];

    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public bool TryAddItem(InventoryItem item)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
            {
                slots[i] = item;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public bool TryRemoveItem(string uniqueInstanceId)
    {
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] != null && slots[i].uniqueInstanceId == uniqueInstanceId)
            {
                slots[i] = null;
                OnInventoryChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public bool HasItem(string uniqueInstanceId)
    {
        foreach (var slot in slots)
            if (slot != null && slot.uniqueInstanceId == uniqueInstanceId) return true;
        return false;
    }

    public bool HasItemWithData(ItemData data)
    {
        foreach (var slot in slots)
            if (slot != null && slot.data == data) return true;
        return false;
    }

    public bool HasItemOfType(ItemType type)
    {
        foreach (var slot in slots)
            if (slot != null && slot.data.itemType == type) return true;
        return false;
    }

    public List<InventoryItem> GetItemsOfType(ItemType type)
    {
        var result = new List<InventoryItem>();
        foreach (var slot in slots)
            if (slot != null && slot.data.itemType == type) result.Add(slot);
        return result;
    }

    public InventoryItem GetItemInSlot(int slotIndex) => slots[slotIndex];

    public bool TryMoveItem(int fromSlot, int toSlot)
    {
        if (fromSlot < 0 || fromSlot >= slots.Length || toSlot < 0 || toSlot >= slots.Length)
            return false;
        (slots[fromSlot], slots[toSlot]) = (slots[toSlot], slots[fromSlot]);
        OnInventoryChanged?.Invoke();
        return true;
    }

    public void ClearAllItems()
    {
        for (int i = 0; i < slots.Length; i++) slots[i] = null;
        OnInventoryChanged?.Invoke();
    }

    public InventoryItem[] GetAllItems()
    {
        var copy = new InventoryItem[slots.Length];
        Array.Copy(slots, copy, slots.Length);
        return copy;
    }
}
