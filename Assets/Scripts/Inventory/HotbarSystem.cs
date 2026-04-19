using System;
using UnityEngine;

public class HotbarSystem : MonoBehaviour
{
    public static HotbarSystem Instance { get; private set; }

    private InventoryItem[] hotbarSlots = new InventoryItem[5];

    public event Action OnHotbarChanged;
    public event Action OnSelectionChanged;

    public int SelectedIndex { get; private set; } = 0;

    public void SelectSlot(int index)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        SelectedIndex = index;
        OnSelectionChanged?.Invoke();
    }

    public InventoryItem GetSelectedItem() => GetHotbarItem(SelectedIndex);

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(this); return; }
        Instance = this;
    }

    private void Start()
    {
        InventorySystem.Instance.OnInventoryChanged += SyncWithInventory;
    }

    private void OnDestroy()
    {
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= SyncWithInventory;
    }

    private void SyncWithInventory()
    {
        bool changed = false;
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            if (hotbarSlots[i] != null && !InventorySystem.Instance.HasItem(hotbarSlots[i].uniqueInstanceId))
            {
                hotbarSlots[i] = null;
                changed = true;
            }
        }
        if (changed) OnHotbarChanged?.Invoke();
    }

    public void AssignToHotbar(int index, InventoryItem item)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;

        if (item != null)
        {
            for (int i = 0; i < hotbarSlots.Length; i++)
            {
                if (hotbarSlots[i] != null && hotbarSlots[i].uniqueInstanceId == item.uniqueInstanceId)
                {
                    hotbarSlots[i] = null;
                    break;
                }
            }
        }

        hotbarSlots[index] = item;
        OnHotbarChanged?.Invoke();
    }

    public InventoryItem GetHotbarItem(int index) =>
        (index >= 0 && index < hotbarSlots.Length) ? hotbarSlots[index] : null;

    public void ClearHotbarSlot(int index)
    {
        if (index < 0 || index >= hotbarSlots.Length) return;
        hotbarSlots[index] = null;
        OnHotbarChanged?.Invoke();
    }

    public void ClearAllHotbar()
    {
        for (int i = 0; i < hotbarSlots.Length; i++) hotbarSlots[i] = null;
        OnHotbarChanged?.Invoke();
    }
}
