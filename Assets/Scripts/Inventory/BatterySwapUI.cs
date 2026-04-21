using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

public class BatterySwapUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GunBatterySystem gunBatterySystem;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference swapBatteryAction;
    [SerializeField] private InputActionReference cancelAction;

    private bool isOpen;

    private void OnEnable()
    {
        swapBatteryAction.action.performed += OnSwapBatteryPressed;
        cancelAction.action.performed += OnCancelPressed;
    }

    private void OnDisable()
    {
        swapBatteryAction.action.performed -= OnSwapBatteryPressed;
        cancelAction.action.performed -= OnCancelPressed;
    }

    private void OnSwapBatteryPressed(InputAction.CallbackContext ctx)
    {
        if (isOpen) Close();
        else Open();
    }

    private void OnCancelPressed(InputAction.CallbackContext ctx)
    {
        if (isOpen) Close();
    }

    private void Open()
    {
        isOpen = true;
        overlayPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Refresh();
    }

    private void Close()
    {
        isOpen = false;
        overlayPanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Refresh()
    {
        foreach (Transform child in entryContainer)
            Destroy(child.gameObject);

        if (statusText != null) statusText.text = "";

        List<InventoryItem> cells = InventorySystem.Instance.GetItemsOfType(ItemType.GravitonCell);

        if (cells.Count == 0)
        {
            if (statusText != null) statusText.text = "No batteries in inventory.";
            return;
        }

        foreach (var item in cells)
        {
            var entry = Instantiate(entryPrefab, entryContainer);

            var icon = entry.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && item.data.icon != null) icon.sprite = item.data.icon;

            var nameText = entry.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null) nameText.text = item.data.itemName;

            var chargeText = entry.transform.Find("Charge")?.GetComponent<TextMeshProUGUI>();
            if (chargeText != null && item.data.cellData != null)
                chargeText.text = $"Max: {item.data.cellData.MaxCharge}";

            var btn = entry.GetComponent<Button>();
            var capturedItem = item;
            btn?.onClick.AddListener(() => OnSelectCell(capturedItem));
        }
    }

    private void OnSelectCell(InventoryItem selectedItem)
    {
        GravitonCell newCell = selectedItem.data.cellData;
        if (newCell == null) return;

        GravitonCell previousCell = gunBatterySystem.CurrentCell;

        if (previousCell != null)
        {
            ItemData previousCellData = FindItemDataForCell(previousCell);
            if (previousCellData != null)
            {
                var returnItem = new InventoryItem(previousCellData);
                if (!InventorySystem.Instance.TryAddItem(returnItem))
                {
                    PickupPromptUI.ShowMessage("Inventory full — cannot swap battery.");
                    return;
                }
            }
        }

        gunBatterySystem.SwapCell(newCell);
        InventorySystem.Instance.TryRemoveItem(selectedItem.uniqueInstanceId);
        Close();
    }

    private ItemData FindItemDataForCell(GravitonCell cell)
    {
        foreach (var slot in InventorySystem.Instance.GetAllItems())
        {
            if (slot != null && slot.data.itemType == ItemType.GravitonCell && slot.data.cellData == cell)
                return slot.data;
        }
        return null;
    }
}
