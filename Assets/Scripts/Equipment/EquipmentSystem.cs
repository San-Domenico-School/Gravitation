using System;
using UnityEngine;

/// <summary>
/// Manages the 3 upgrade/equipment slots (drone, compass, armor, etc.).
/// Items placed here are removed from the inventory; unequipping returns them.
/// Only ItemType.Equipment items may be slotted.
/// </summary>
public class EquipmentSystem : MonoBehaviour
{
    public static EquipmentSystem Instance { get; private set; }

    public const int SlotCount = 3;
    private InventoryItem[] slots = new InventoryItem[SlotCount];

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
    }

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Equips an item from the inventory into the given slot.
    /// Removes it from inventory; if the slot is already occupied the old item is
    /// returned to inventory first. Returns false if the item is not Equipment-type.
    /// </summary>
    public bool TryEquip(int slot, InventoryItem item)
    {
        if (slot < 0 || slot >= SlotCount) return false;
        if (item == null || item.data.itemType != ItemType.Equipment) return false;

        // Push out any current occupant
        if (slots[slot] != null)
            InventorySystem.Instance.TryAddItem(slots[slot]);

        // Pull from inventory (may already be absent if we're mid-drag)
        InventorySystem.Instance.TryRemoveItem(item.uniqueInstanceId);

        slots[slot] = item;
        OnEquipmentChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// Directly sets a slot without touching inventory.
    /// Use during drag operations when the item has already been removed from its source.
    /// Any existing occupant is returned to inventory.
    /// </summary>
    public void ForceSet(int slot, InventoryItem item)
    {
        if (slot < 0 || slot >= SlotCount) return;

        if (slots[slot] != null)
            InventorySystem.Instance.TryAddItem(slots[slot]);

        slots[slot] = item;
        OnEquipmentChanged?.Invoke();
    }

    /// <summary>
    /// Unequips the item in the given slot and returns it to the inventory.
    /// </summary>
    public void Unequip(int slot)
    {
        if (slot < 0 || slot >= SlotCount || slots[slot] == null) return;
        InventorySystem.Instance.TryAddItem(slots[slot]);
        slots[slot] = null;
        OnEquipmentChanged?.Invoke();
    }

    /// <summary>
    /// Removes the item from the slot WITHOUT returning it to inventory.
    /// Use during drag operations — the caller is responsible for the item.
    /// </summary>
    public InventoryItem TakeFromSlot(int slot)
    {
        if (slot < 0 || slot >= SlotCount) return null;
        var item = slots[slot];
        slots[slot] = null;
        if (item != null) OnEquipmentChanged?.Invoke();
        return item;
    }

    /// <summary>Returns the item in the given slot, or null if empty.</summary>
    public InventoryItem GetEquipped(int slot) =>
        (slot >= 0 && slot < SlotCount) ? slots[slot] : null;

    /// <summary>Returns true if the given ItemData is currently equipped in any slot.</summary>
    public bool HasEquipped(ItemData data)
    {
        foreach (var s in slots)
            if (s != null && s.data == data) return true;
        return false;
    }

    /// <summary>Returns the slot index of the given ItemData, or -1 if not found.</summary>
    public int FindEquippedSlot(ItemData data)
    {
        for (int i = 0; i < SlotCount; i++)
            if (slots[i] != null && slots[i].data == data) return i;
        return -1;
    }
}
