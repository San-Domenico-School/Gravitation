using UnityEngine;

/// <summary>
/// Watches the EquipmentSystem and activates/deactivates the Scout Drone and its
/// minimap radar whenever the Scout Drone item enters or leaves any equipment slot.
///
/// Setup: add this component to the player (or any persistent object), then wire:
///   • scoutDroneGameObject — the GameObject that has the ScoutDrone component.
///   • scoutDroneItemData   — the Scout Drone ItemData ScriptableObject asset.
///
/// The drone GameObject should start INACTIVE in the scene.
/// </summary>
public class ScoutDroneEquipSystem : MonoBehaviour
{
    [Tooltip("The GameObject that holds the ScoutDrone component. Must start inactive.")]
    [SerializeField] private GameObject scoutDroneGameObject;

    [Tooltip("ItemData asset for the Scout Drone item (ItemType must be Equipment).")]
    [SerializeField] private ItemData scoutDroneItemData;

    private void Start()
    {
        if (EquipmentSystem.Instance != null)
            EquipmentSystem.Instance.OnEquipmentChanged += UpdateEquipState;
        UpdateEquipState();
    }

    private void OnDestroy()
    {
        if (EquipmentSystem.Instance != null)
            EquipmentSystem.Instance.OnEquipmentChanged -= UpdateEquipState;
    }

    private void UpdateEquipState()
    {
        if (scoutDroneGameObject == null || scoutDroneItemData == null) return;

        bool equipped = EquipmentSystem.Instance != null
            && EquipmentSystem.Instance.HasEquipped(scoutDroneItemData);

        // Enable/disable the drone object (ScoutDrone.OnEnable/OnDisable handle minimap).
        scoutDroneGameObject.SetActive(equipped);

        // Also show/hide the minimap panel itself.
        if (MinimapUI.Instance != null)
            MinimapUI.Instance.gameObject.SetActive(equipped);
    }
}
