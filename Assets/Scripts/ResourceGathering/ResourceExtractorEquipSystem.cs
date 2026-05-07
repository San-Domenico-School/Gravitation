using UnityEngine;

/// <summary>
/// Watches the hotbar selection and enables the matching ResourceExtractor when the
/// player has the right Resource Extractor item selected. Mirrors GravityGunEquipSystem.
/// Drop multiple of these on a player rig (one per tier) to support both T1 and T2.
/// </summary>
public class ResourceExtractorEquipSystem : MonoBehaviour
{
    [Tooltip("ResourceExtractor component to enable/disable based on hotbar selection.")]
    [SerializeField] private ResourceExtractor extractor;

    [Tooltip("ItemData asset that, when equipped, activates this extractor.")]
    [SerializeField] private ItemData extractorItemData;

    private void Start()
    {
        if (HotbarSystem.Instance != null)
        {
            HotbarSystem.Instance.OnHotbarChanged += UpdateEquipState;
            HotbarSystem.Instance.OnSelectionChanged += UpdateEquipState;
        }
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged += UpdateEquipState;
        UpdateEquipState();
    }

    private void OnDestroy()
    {
        if (HotbarSystem.Instance != null)
        {
            HotbarSystem.Instance.OnHotbarChanged -= UpdateEquipState;
            HotbarSystem.Instance.OnSelectionChanged -= UpdateEquipState;
        }
        if (InventorySystem.Instance != null)
            InventorySystem.Instance.OnInventoryChanged -= UpdateEquipState;
    }

    private void UpdateEquipState()
    {
        if (extractor == null || extractorItemData == null) return;

        var selected = HotbarSystem.Instance != null ? HotbarSystem.Instance.GetSelectedItem() : null;
        bool equipped = selected != null && selected.data == extractorItemData;
        extractor.SetEquipped(equipped);
    }
}
