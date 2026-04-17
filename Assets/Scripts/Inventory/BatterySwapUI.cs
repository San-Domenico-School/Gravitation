using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BatterySwapUI : MonoBehaviour
{
    [SerializeField] private GameObject overlayPanel;
    [SerializeField] private Transform entryContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GunBatterySystem gunBatterySystem;

    private bool isOpen;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            if (isOpen) Close();
            else Open();
        }

        if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    private void Open()
    {
        isOpen = true;
        overlayPanel.SetActive(true);
        Refresh();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
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
        var allItems = InventorySystem.Instance.GetAllItems();
        foreach (var slot in allItems)
        {
            if (slot != null && slot.data.itemType == ItemType.GravitonCell && slot.data.cellData == cell)
                return slot.data;
        }
        return null;
    }
}
