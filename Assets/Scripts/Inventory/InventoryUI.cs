using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;
    [SerializeField] private Image cursorIcon;
    [SerializeField] private GameObject tooltip;
    [SerializeField] private TextMeshProUGUI tooltipName;
    [SerializeField] private TextMeshProUGUI tooltipDesc;

    private const int SlotCount = 48;
    private SlotUI[] slotUIs;
    private bool isOpen;

    private int dragSourceSlot = -1;

    private void Awake()
    {
        slotUIs = new SlotUI[SlotCount];
        for (int i = 0; i < SlotCount; i++)
        {
            var go = Instantiate(slotPrefab, slotContainer);
            slotUIs[i] = go.GetComponent<SlotUI>();
            int captured = i;
            slotUIs[i].Init(captured, OnSlotClicked, OnSlotHoverEnter, OnSlotHoverExit, OnSlotHotkey);
        }

        if (tooltip != null) tooltip.SetActive(false);
        if (cursorIcon != null) cursorIcon.gameObject.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }

    private void OnEnable()
    {
        InventorySystem.Instance.OnInventoryChanged += Refresh;
        HotbarSystem.Instance.OnHotbarChanged += Refresh;
    }

    private void OnDisable()
    {
        if (InventorySystem.Instance != null) InventorySystem.Instance.OnInventoryChanged -= Refresh;
        if (HotbarSystem.Instance != null) HotbarSystem.Instance.OnHotbarChanged -= Refresh;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (isOpen) CloseInventory();
            else OpenInventory();
        }

        if (isOpen && dragSourceSlot >= 0 && cursorIcon != null)
        {
            cursorIcon.rectTransform.position = Input.mousePosition;

            if (Input.GetMouseButtonDown(0) && !IsPointerOverSlot())
                CancelDrag();
        }
    }

    private void OpenInventory()
    {
        isOpen = true;
        inventoryPanel.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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

    private void Refresh()
    {
        if (!isOpen) return;
        for (int i = 0; i < SlotCount; i++)
        {
            var item = InventorySystem.Instance.GetItemInSlot(i);
            bool isDragging = dragSourceSlot == i;
            slotUIs[i].SetItem(isDragging ? null : item);
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
                cursorIcon.rectTransform.position = Input.mousePosition;
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
        var item = InventorySystem.Instance.GetItemInSlot(slotIndex);
        if (item != null && tooltip != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowTooltipDelayed(item, Input.mousePosition));
        }
    }

    private void OnSlotHoverExit(int slotIndex)
    {
        StopAllCoroutines();
        if (tooltip != null) tooltip.SetActive(false);
    }

    private IEnumerator ShowTooltipDelayed(InventoryItem item, Vector3 pos)
    {
        yield return new WaitForSecondsRealtime(0.5f);
        if (tooltip != null)
        {
            tooltipName.text = item.data.itemName;
            tooltipDesc.text = item.data.description;
            tooltip.SetActive(true);
            tooltip.transform.position = pos + new Vector3(10, -10, 0);
        }
    }

    private void OnSlotHotkey(int slotIndex, KeyCode key)
    {
        int hotbarIndex = key - KeyCode.Alpha1;
        var item = InventorySystem.Instance.GetItemInSlot(slotIndex);
        if (item != null)
            HotbarSystem.Instance.AssignToHotbar(hotbarIndex, item);
    }
}
