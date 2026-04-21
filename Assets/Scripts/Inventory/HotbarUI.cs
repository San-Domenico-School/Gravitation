using UnityEngine;
using UnityEngine.InputSystem;

public class HotbarUI : MonoBehaviour
{
    [SerializeField] private HotbarSlotUI[] slotUIs;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference hotbar1Action;
    [SerializeField] private InputActionReference hotbar2Action;
    [SerializeField] private InputActionReference hotbar3Action;
    [SerializeField] private InputActionReference hotbar4Action;
    [SerializeField] private InputActionReference hotbar5Action;

    private void OnEnable()
    {
        hotbar1Action.action.performed += OnHotbar1;
        hotbar2Action.action.performed += OnHotbar2;
        hotbar3Action.action.performed += OnHotbar3;
        hotbar4Action.action.performed += OnHotbar4;
        hotbar5Action.action.performed += OnHotbar5;
    }

    private void Start()
    {
        for (int i = 0; i < slotUIs.Length; i++)
        {
            if (slotUIs[i] == null) { Debug.LogError($"[HotbarUI] slotUIs[{i}] is NULL"); continue; }
            int captured = i;
            slotUIs[i].Init(captured, OnSlotLeftClicked, OnSlotRightClicked);
        }

        HotbarSystem.Instance.OnHotbarChanged += Refresh;
        HotbarSystem.Instance.OnSelectionChanged += UpdateSelection;
        Refresh();
    }

    private void OnSlotLeftClicked(int index)
    {
        Debug.Log($"[HotbarUI] OnSlotLeftClicked index={index}");
        if (InventoryUI.Instance != null && InventoryUI.Instance.TryAssignDraggedToHotbar(index))
            return;
        HotbarSystem.Instance.SelectSlot(index);
    }

    private void OnSlotRightClicked(int index)
    {
        HotbarSystem.Instance.ClearHotbarSlot(index);
    }

    private void OnDisable()
    {
        if (HotbarSystem.Instance != null)
        {
            HotbarSystem.Instance.OnHotbarChanged -= Refresh;
            HotbarSystem.Instance.OnSelectionChanged -= UpdateSelection;
        }
        hotbar1Action.action.performed -= OnHotbar1;
        hotbar2Action.action.performed -= OnHotbar2;
        hotbar3Action.action.performed -= OnHotbar3;
        hotbar4Action.action.performed -= OnHotbar4;
        hotbar5Action.action.performed -= OnHotbar5;
    }

    private void OnHotbar1(InputAction.CallbackContext ctx) => HandleHotbarKey(0);
    private void OnHotbar2(InputAction.CallbackContext ctx) => HandleHotbarKey(1);
    private void OnHotbar3(InputAction.CallbackContext ctx) => HandleHotbarKey(2);
    private void OnHotbar4(InputAction.CallbackContext ctx) => HandleHotbarKey(3);
    private void OnHotbar5(InputAction.CallbackContext ctx) => HandleHotbarKey(4);

    private void HandleHotbarKey(int index)
    {
        bool invOpen = InventoryUI.Instance != null && InventoryUI.Instance.IsOpen;
        Debug.Log($"[HotbarUI] HandleHotbarKey index={index} inventoryOpen={invOpen}");
        if (invOpen)
            InventoryUI.Instance.TryAssignHovered(index);
        else
            HotbarSystem.Instance.SelectSlot(index);
    }

    private void Refresh()
    {
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetItem(HotbarSystem.Instance.GetHotbarItem(i), i + 1);
        UpdateSelection();
    }

    private void UpdateSelection()
    {
        for (int i = 0; i < slotUIs.Length; i++)
            slotUIs[i].SetSelected(i == HotbarSystem.Instance.SelectedIndex);
    }
}
