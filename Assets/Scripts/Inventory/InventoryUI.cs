using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Image cursorIcon;
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TextMeshProUGUI tooltipName;
    [SerializeField] private TextMeshProUGUI tooltipDesc;
    [SerializeField] private PlayerInput playerInput;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference toggleInventoryAction;
    [SerializeField] private InputActionReference hotbar1Action;
    [SerializeField] private InputActionReference hotbar2Action;
    [SerializeField] private InputActionReference hotbar3Action;
    [SerializeField] private InputActionReference hotbar4Action;
    [SerializeField] private InputActionReference hotbar5Action;

    private const int SlotCount = 48;
    private SlotUI[] slotUIs;
    private bool isOpen;
    private int dragSourceSlot = -1;
    private int hoveredSlotIndex = -1;

    private void Awake()
    {
        slotUIs = new SlotUI[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            var go = Instantiate(slotPrefab, slotContainer);
            slotUIs[i] = go.GetComponent<SlotUI>();
            int captured = i;
            slotUIs[i].Init(captured, OnSlotClicked, OnSlotHoverEnter, OnSlotHoverExit);
        }

        if (tooltip != null) tooltip.SetActive(false);
        if (cursorIcon != null) cursorIcon.gameObject.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    private void OnEnable()
    {
        toggleInventoryAction.action.performed += OnToggleInventory;
        hotbar1Action.action.performed += OnHotbar1;
        hotbar2Action.action.performed += OnHotbar2;
        hotbar3Action.action.performed += OnHotbar3;
        hotbar4Action.action.performed += OnHotbar4;
        hotbar5Action.action.performed += OnHotbar5;
    }

    private void Start()
    {
        InventorySystem.Instance.OnInventoryChanged += Refresh;
        HotbarSystem.Instance.OnHotbarChanged += Refresh;
    }

    private void OnDisable()
    {
        toggleInventoryAction.action.performed -= OnToggleInventory;
        hotbar1Action.action.performed -= OnHotbar1;
        hotbar2Action.action.performed -= OnHotbar2;
        hotbar3Action.action.performed -= OnHotbar3;
        hotbar4Action.action.performed -= OnHotbar4;
        hotbar5Action.action.performed -= OnHotbar5;

        if (InventorySystem.Instance != null) InventorySystem.Instance.OnInventoryChanged -= Refresh;
        if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnHotbarChanged -= Refresh;
    }

    private void OnToggleInventory(InputAction.CallbackContext ctx)
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    private void OnHotbar1(InputAction.CallbackContext ctx) => TryAssignHotbar(0);
    private void OnHotbar2(InputAction.CallbackContext ctx) => TryAssignHotbar(1);
    private void OnHotbar3(InputAction.CallbackContext ctx) => TryAssignHotbar(2);
    private void OnHotbar4(InputAction.CallbackContext ctx) => TryAssignHotbar(3);
    private void OnHotbar5(InputAction.CallbackContext ctx) => TryAssignHotbar(4);

    private void TryAssignHotbar(int index)
    {
        if (!isOpen || hoveredSlotIndex < 0) return;
        var item = InventorySystem.Instance.GetItemInSlot(hoveredSlotIndex);
        if (item != null) HotbarSystem.Instance.AssignToHotbar(index, item);
    }

    private void OpenInventory()
    {
        isOpen = true;
        inventoryPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Disable gameplay maps so movement/camera stop while inventory is open
        playerInput?.actions.FindActionMap("Player").Disable();
        playerInput?.actions.FindActionMap("GravityGun").Disable();

        // Re-enable just the inventory toggle so Tab can still close it
        toggleInventoryAction.action.Enable();

        Refresh();
    }

    private void CloseInventory()
    {
        if (dragSourceSlot >= 0) CancelDrag();
        isOpen = false;
        inventoryPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (tooltip != null) tooltip.SetActive(false);

        playerInput?.actions.FindActionMap("Player").Enable();
        playerInput?.actions.FindActionMap("GravityGun").Enable();
    }

    private void Update()
    {
        if (!isOpen || dragSourceSlot < 0 || cursorIcon == null) return;

        cursorIcon.rectTransform.position = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverSlot())
            CancelDrag();
    }

    private void Refresh()
    {
        if (!isOpen) return;
        for (int i = 0; i < SlotCount; i++)
        {
            var item = InventorySystem.Instance.GetItemInSlot(i);
            slotUIs[i].SetItem(dragSourceSlot == i ? null : item);
        }
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (dragSourceSlot < 0)
        {
            var item = InventorySystem.Instance.GetItemInSlot(slotIndex);
            if (item == null) return;

            dragSourceSlot = slotIndex;
            if (cursorIcon != null)
            {
                cursorIcon.sprite = item.data.icon;
                cursorIcon.gameObject.SetActive(true);
                cursorIcon.rectTransform.position = Mouse.current.position.ReadValue();
            }
            Refresh();
        }
        else
        {
            if (slotIndex != dragSourceSlot)
                InventorySystem.Instance.TryMoveItem(dragSourceSlot, slotIndex);

            dragSourceSlot = -1;
            if (cursorIcon != null) cursorIcon.gameObject.SetActive(false);
            Refresh();
        }
    }

    private void CancelDrag()
    {
        dragSourceSlot = -1;
        if (cursorIcon != null) cursorIcon.gameObject.SetActive(false);
        Refresh();
    }

    private bool IsPointerOverSlot() =>
        EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

    private void OnSlotHoverEnter(int slotIndex)
    {
        hoveredSlotIndex = slotIndex;
        var item = InventorySystem.Instance.GetItemInSlot(slotIndex);
        if (item != null && tooltip != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowTooltipDelayed(item, Mouse.current.position.ReadValue()));
        }
    }

    private void OnSlotHoverExit(int slotIndex)
    {
        if (hoveredSlotIndex == slotIndex) hoveredSlotIndex = -1;
        StopAllCoroutines();
        if (tooltip != null) tooltip.SetActive(false);
    }

    private IEnumerator ShowTooltipDelayed(InventoryItem item, Vector2 pos)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        if (tooltip != null)
        {
            tooltipName.text = item.data.itemName;
            tooltipDesc.text = item.data.description;
            tooltip.SetActive(true);
            tooltip.transform.position = (Vector3)pos + new Vector3(10, -10, 0);
        }
    }
}
