using UnityEngine;

/// Enables/disables the GravityGun component based on whether the GravityGun item
/// is currently the selected hotbar slot. The gun only functions when equipped.
public class GravityGunEquipSystem : MonoBehaviour
{
    [SerializeField] private GravityGun gravityGun;
    [Tooltip("The ItemData asset that represents the Gravity Gun item.")]
    [SerializeField] private ItemData gravityGunItemData;

    private void Start()
    {
        if (gravityGun != null) gravityGun.enabled = false;

        HotbarSystem.Instance.OnHotbarChanged += UpdateEquipState;
        HotbarSystem.Instance.OnSelectionChanged += UpdateEquipState;
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
        if (gravityGun == null)   { Debug.LogWarning("[GravityGunEquip] gravityGun field is not assigned!"); return; }
        if (gravityGunItemData == null) { Debug.LogWarning("[GravityGunEquip] gravityGunItemData field is not assigned!"); return; }

        bool inInventory = InventorySystem.Instance.HasItemWithData(gravityGunItemData);
        var selected = HotbarSystem.Instance.GetSelectedItem();
        bool equipped = inInventory && selected != null && selected.data == gravityGunItemData;

        Debug.Log($"[GravityGunEquip] inInventory={inInventory} selectedItem={selected?.data?.itemName ?? "null"} equipped={equipped}");
        gravityGun.enabled = equipped;
    }
}
