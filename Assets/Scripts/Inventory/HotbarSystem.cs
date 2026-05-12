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
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        // Persist across scenes — hotbar follows the player progression, not the scene.
        // DDOL only if scene-root; otherwise rely on parent's DDOL.
        if (transform.parent == null) DontDestroyOnLoad(gameObject);
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

    /// <summary>Total number of hotbar slots (5).</summary>
    public int SlotCount => hotbarSlots.Length;

    /// <summary>
    /// Replaces all hotbar assignments + selected index at once. Used by the save system.
    /// Caller is responsible for passing already-rehydrated InventoryItems whose
    /// uniqueInstanceId matches an item currently in the InventorySystem.
    /// </summary>
    public void RestoreFromSnapshot(InventoryItem[] snapshot, int selectedIndex)
    {
        for (int i = 0; i < hotbarSlots.Length; i++)
        {
            hotbarSlots[i] = (snapshot != null && i < snapshot.Length) ? snapshot[i] : null;
        }
        SelectedIndex = Mathf.Clamp(selectedIndex, 0, hotbarSlots.Length - 1);
        OnHotbarChanged?.Invoke();
        OnSelectionChanged?.Invoke();
    }
}
