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

        int currentTier = gunBatterySystem.CurrentCell != null ? gunBatterySystem.CurrentCell.Tier : 0;

        List<InventoryItem> allCells = InventorySystem.Instance.GetItemsOfType(ItemType.GravitonCell);
        List<InventoryItem> upgrades = new List<InventoryItem>();
        foreach (var item in allCells)
        {
            if (item.data.cellData != null && item.data.cellData.Tier > currentTier)
                upgrades.Add(item);
        }

        if (upgrades.Count == 0)
        {
            if (statusText != null) statusText.text = "No upgrades available.";
            return;
        }

        foreach (var item in upgrades)
        {
            var entry = Instantiate(entryPrefab, entryContainer);

            var icon = entry.transform.Find("Icon")?.GetComponent<Image>();
            if (icon != null && item.data.icon != null) icon.sprite = item.data.icon;

            var nameText = entry.transform.Find("Name")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null) nameText.text = $"Install {item.data.itemName} (T{item.data.cellData.Tier})";

            var chargeText = entry.transform.Find("Charge")?.GetComponent<TextMeshProUGUI>();
            if (chargeText != null && item.data.cellData != null)
                chargeText.text = $"Max: {item.data.cellData.MaxCharge}  |  +{item.data.cellData.PassiveRechargeRate}/s";

            var btn = entry.GetComponent<Button>();
            var capturedItem = item;
            btn?.onClick.AddListener(() => OnInstallCell(capturedItem));
        }
    }

    private void OnInstallCell(InventoryItem selectedItem)
    {
        GravitonCell newCell = selectedItem.data.cellData;
        if (newCell == null) return;

        // One-way install per GDD: never swap back, never recover the previous cell.
        // The previous cell is permanently consumed by the upgrade.
        int currentTier = gunBatterySystem.CurrentCell != null ? gunBatterySystem.CurrentCell.Tier : 0;
        if (newCell.Tier <= currentTier)
        {
            PickupPromptUI.ShowMessage("Cannot downgrade installed cell.");
            return;
        }

        gunBatterySystem.SwapCell(newCell);
        InventorySystem.Instance.TryRemoveItem(selectedItem.uniqueInstanceId);
        Close();
    }
}
