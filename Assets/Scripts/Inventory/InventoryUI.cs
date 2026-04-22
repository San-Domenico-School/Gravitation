using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

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

    public bool IsOpen => isOpen;

    private const int SlotCount = 48;
    private SlotUI[] slotUIs;
    private bool isOpen;
    private int dragSourceSlot = -1;
    private int hoveredSlotIndex = -1;
    private Transform playerTransform;

    private void Awake()
    {
        Instance = this;
        slotUIs = new SlotUI[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            var go = Instantiate(slotPrefab, slotContainer);
            slotUIs[i] = go.GetComponent<SlotUI>();
            int captured = i;
            slotUIs[i].Init(captured, OnSlotClicked, OnSlotRightClicked, OnSlotHoverEnter, OnSlotHoverExit);
        }

        if (tooltip != null) tooltip.SetActive(false);
        if (cursorIcon != null) cursorIcon.gameObject.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    private void OnEnable()
    {
        toggleInventoryAction.action.performed += OnToggleInventory;
    }

    private void Start()
    {
        InventorySystem.Instance.OnInventoryChanged += Refresh;
        HotbarSystem.Instance.OnHotbarChanged += Refresh;
        var player = GameObject.FindWithTag("Player");
        if (player != null) playerTransform = player.transform;
    }

    private void OnDisable()
    {
        toggleInventoryAction.action.performed -= OnToggleInventory;
        if (InventorySystem.Instance != null) InventorySystem.Instance.OnInventoryChanged -= Refresh;
        if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnHotbarChanged -= Refresh;
    }

    private void OnToggleInventory(InputAction.CallbackContext ctx)
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public void TryAssignHovered(int hotbarIndex)
    {
        if (hoveredSlotIndex < 0) return;
        var item = InventorySystem.Instance.GetItemInSlot(hoveredSlotIndex);
        if (item != null)
            HotbarSystem.Instance.AssignToHotbar(hotbarIndex, item);
    }

    private void OpenInventory()
    {
        isOpen = true;
        inventoryPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // timeScale=0 freezes movement and camera (both use Time.deltaTime).
        // We do NOT disable action maps — that kills InputSystemUIInputModule's click detection.

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

    }

    private void Update()
    {
        if (!isOpen) return;

        PollHoveredSlot();

        if (dragSourceSlot >= 0 && cursorIcon != null)
        {
            cursorIcon.rectTransform.position = Mouse.current.position.ReadValue();
            if (Mouse.current.leftButton.wasPressedThisFrame && !IsPointerOverSlot())
                CancelDrag();
        }
    }

    private readonly List<RaycastResult> _raycastResults = new List<RaycastResult>();

    private void PollHoveredSlot()
    {
        _raycastResults.Clear();
        var pointerData = new PointerEventData(EventSystem.current)
            { position = Mouse.current.position.ReadValue() };
        EventSystem.current.RaycastAll(pointerData, _raycastResults);

        int newHovered = -1;
        foreach (var result in _raycastResults)
        {
            var slot = result.gameObject.GetComponent<SlotUI>();
            if (slot == null) slot = result.gameObject.GetComponentInParent<SlotUI>();
            if (slot != null)
            {
                for (int i = 0; i < slotUIs.Length; i++)
                {
                    if (slotUIs[i] == slot) { newHovered = i; break; }
                }
                break;
            }
        }

        if (newHovered != hoveredSlotIndex)
        {
            hoveredSlotIndex = newHovered;
            if (hoveredSlotIndex < 0 || tooltip == null) return;
            var item = InventorySystem.Instance.GetItemInSlot(hoveredSlotIndex);
            StopAllCoroutines();
            if (item != null) StartCoroutine(ShowTooltipDelayed(item, Mouse.current.position.ReadValue()));
            else tooltip.SetActive(false);
        }
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

    private void OnSlotHoverEnter(int slotIndex) { }
    private void OnSlotHoverExit(int slotIndex) { }

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

    public bool TryAssignDraggedToHotbar(int hotbarIndex)
    {
        if (dragSourceSlot < 0) return false;
        var item = InventorySystem.Instance.GetItemInSlot(dragSourceSlot);
        if (item == null) return false;

        HotbarSystem.Instance.AssignToHotbar(hotbarIndex, item);
        dragSourceSlot = -1;
        if (cursorIcon != null) cursorIcon.gameObject.SetActive(false);
        Refresh();
        return true;
    }

    private void OnSlotRightClicked(int slotIndex)
    {
        if (dragSourceSlot >= 0) return;
        var item = InventorySystem.Instance.GetItemInSlot(slotIndex);
        if (item == null) return;
        DropItem(item);
    }

    private void DropItem(InventoryItem item)
    {
        if (item.data.worldPrefab == null)
        {
            PickupPromptUI.ShowMessage($"{item.data.itemName} cannot be dropped.");
            return;
        }

        Vector3 spawnPos = playerTransform != null
            ? playerTransform.position + playerTransform.forward * 1.5f
            : Vector3.zero;

        Object.Instantiate(item.data.worldPrefab, spawnPos, Quaternion.identity);
        InventorySystem.Instance.TryRemoveItem(item.uniqueInstanceId);
        if (tooltip != null) tooltip.SetActive(false);
        Refresh();
    }
}
