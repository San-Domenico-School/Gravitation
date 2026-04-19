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
        Debug.Log($"[GravityGunEquip] Start — gun={gravityGun != null}, itemData={gravityGunItemData != null}, gun.go.active={gravityGun?.gameObject.activeInHierarchy}, gun.enabled={gravityGun?.enabled}");
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
        if (gravityGun == null)         { Debug.LogWarning("[GravityGunEquip] gravityGun field is not assigned!"); return; }
        if (gravityGunItemData == null) { Debug.LogWarning("[GravityGunEquip] gravityGunItemData field is not assigned!"); return; }

        var selected = HotbarSystem.Instance.GetSelectedItem();
        bool equipped = selected != null && selected.data == gravityGunItemData;

        gravityGun.SetEquipped(equipped);
    }
}
