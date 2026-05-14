using UnityEngine;

/// <summary>
/// The 3-slot upgrade/equipment panel that lives inside the inventory screen.
///
/// Drop this component on a child GameObject inside the inventory panel. Wire
/// slotPrefab (an EquipmentSlotUI prefab) and slotContainer.
///
/// Drag interaction:
///   • Dragging an Equipment item from inventory → click slot → equips it.
///   • Left-clicking an occupied slot (no drag active) → picks item up into drag cursor.
///   • Right-clicking an occupied slot → unequips to bag instantly.
/// </summary>
public class EquipmentPanelUI : MonoBehaviour
{
    public static EquipmentPanelUI Instance { get; private set; }

    [Tooltip("Prefab with EquipmentSlotUI component. One will be instantiated per slot.")]
    [SerializeField] private GameObject slotPrefab;

    [Tooltip("Parent transform that holds the instantiated slot GameObjects.")]
    [SerializeField] private Transform slotContainer;

    private EquipmentSlotUI[] slotUIs;

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        slotUIs = new EquipmentSlotUI[EquipmentSystem.SlotCount];
        for (int i = 0; i < EquipmentSystem.SlotCount; i++)
        {
            var go = Instantiate(slotPrefab, slotContainer);
            slotUIs[i] = go.GetComponent<EquipmentSlotUI>();
            if (slotUIs[i] == null)
            {
                Debug.LogError("[EquipmentPanelUI] slotPrefab is missing an EquipmentSlotUI component!");
                continue;
            }
            int captured = i;
            slotUIs[i].Init(captured, OnSlotLeftClicked, OnSlotRightClicked);
        }
    }

    private void OnEnable()
    {
        // Refresh whenever the panel becomes visible (e.g., inventory opens).
        Refresh();
    }

    private void Start()
    {
        if (EquipmentSystem.Instance != null)
            EquipmentSystem.Instance.OnEquipmentChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (EquipmentSystem.Instance != null)
            EquipmentSystem.Instance.OnEquipmentChanged -= Refresh;
    }

    // -------------------------------------------------------------------------
    // Refresh
    // -------------------------------------------------------------------------

    private void Refresh()
    {
        if (slotUIs == null) return;
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null) continue;
            slotUIs[i].SetItem(EquipmentSystem.Instance != null
                ? EquipmentSystem.Instance.GetEquipped(i)
                : null);
        }
    }

    // -------------------------------------------------------------------------
    // Slot interaction
    // -------------------------------------------------------------------------

    private void OnSlotLeftClicked(int equipSlot)
    {
        var inv = InventoryUI.Instance;
        if (inv == null) return;

        if (inv.IsDragging)
        {
            // Player is dragging — attempt to equip into this slot.
            var draggedItem = inv.GetDraggedItem();
            if (draggedItem == null) return;

            if (draggedItem.data.itemType == ItemType.Equipment)
            {
                // Pull the item out of wherever it was (inventory or another equip slot).
                inv.ConsumeDraggedItem();
                EquipmentSystem.Instance.ForceSet(equipSlot, draggedItem);
            }
            else
            {
                // Wrong type — cancel the drag and return item to bag.
                inv.CancelDragPublic();
                PickupPromptUI.ShowMessage($"{draggedItem.data.itemName} can't go in an upgrade slot.");
            }
        }
        else
        {
            // No active drag — pick up the equipped item to start dragging it.
            var equipped = EquipmentSystem.Instance != null
                ? EquipmentSystem.Instance.GetEquipped(equipSlot)
                : null;
            if (equipped == null) return;

            // Remove from equipment (without returning to inventory — item is "in hand").
            EquipmentSystem.Instance.TakeFromSlot(equipSlot);
            inv.StartDragFromEquipment(equipped);
        }

        Refresh();
    }

    private void OnSlotRightClicked(int equipSlot)
    {
        // Right-click unequips directly back to the bag.
        if (EquipmentSystem.Instance == null) return;
        EquipmentSystem.Instance.Unequip(equipSlot);
        Refresh();
    }
}
