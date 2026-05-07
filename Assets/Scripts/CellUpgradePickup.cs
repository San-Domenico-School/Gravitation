using UnityEngine;

/// <summary>
/// Walk-in pickup that auto-installs a Graviton Cell on the player's gun and
/// destroys itself. Attach to any GameObject with a Collider (the collider is
/// auto-configured as a trigger when the script is added).
/// </summary>
[RequireComponent(typeof(Collider))]
public class CellUpgradePickup : MonoBehaviour
{
    [SerializeField]
    [Tooltip("The cell asset that will be installed when the player walks into this pickup.")]
    private GravitonCell cell;

    [SerializeField]
    [Tooltip("If true, only swaps when this cell's tier is higher than the currently equipped cell. Prevents accidental downgrades.")]
    private bool onlyIfHigherTier = true;

    [SerializeField]
    [Tooltip("Tag used on the player root. Must match the player's tag.")]
    private string playerTag = "Player";

    private bool consumed;

    private void Reset()
    {
        // Default the collider to trigger mode when the script is first added.
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (consumed || cell == null) return;
        if (!other.CompareTag(playerTag)) return;

        var battery = other.GetComponentInChildren<GunBatterySystem>(true);
        if (battery == null) battery = other.GetComponentInParent<GunBatterySystem>();
        if (battery == null) battery = FindFirstObjectByType<GunBatterySystem>(FindObjectsInactive.Include);
        if (battery == null)
        {
            Debug.LogWarning($"CellUpgradePickup: No GunBatterySystem found for player; pickup ignored.");
            return;
        }

        if (onlyIfHigherTier && battery.CurrentCell != null && cell.Tier <= battery.CurrentCell.Tier)
            return;

        battery.SwapCell(cell);
        consumed = true;
        Destroy(gameObject);
    }
}
