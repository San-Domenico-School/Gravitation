using TMPro;
using UnityEngine;

/// <summary>
/// Minimal HUD for the Gravity Gun. Writes mode + selection count + tier into a single
/// TextMeshPro label. Hook it up like BatteryHUD: drag the gun, drag one text field, done.
/// </summary>
public class GravityGunHUD : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the GravityGun.")]
    private GravityGun gun;

    [SerializeField]
    [Tooltip("Single TextMeshPro label for the gun's status line.")]
    private TextMeshProUGUI statusText;

    private void Awake()
    {
        if (gun == null)
            gun = FindFirstObjectByType<GravityGun>();
    }

    private void OnEnable()
    {
        if (gun == null) return;
        gun.OnModeChanged += HandleModeChanged;
        gun.OnSelectionChanged += HandleSelectionChanged;
        gun.OnEffectiveTierChanged += HandleTierChanged;
        Refresh();
    }

    private void OnDisable()
    {
        if (gun == null) return;
        gun.OnModeChanged -= HandleModeChanged;
        gun.OnSelectionChanged -= HandleSelectionChanged;
        gun.OnEffectiveTierChanged -= HandleTierChanged;
    }

    private void HandleModeChanged(GravityGun.Mode _) => Refresh();
    private void HandleSelectionChanged(int _, int __) => Refresh();
    private void HandleTierChanged(int _) => Refresh();

    private void Refresh()
    {
        if (statusText == null || gun == null) return;

        string modeText;
        bool showSelection;
        switch (gun.CurrentMode)
        {
            case GravityGun.Mode.Selection:        modeText = "<color=#B5D9FF>SELECT</color>";        showSelection = true;  break;
            case GravityGun.Mode.GravityPlacement: modeText = "<color=#FFCC66>PLACE</color>";          showSelection = true;  break;
            case GravityGun.Mode.PulseMode:        modeText = "<color=#FF8050>PULSE</color>";          showSelection = false; break;
            case GravityGun.Mode.LockMode:
                modeText = gun.IsLockActive
                    ? "<color=#9966FF>LOCK•ACTIVE</color>"
                    : "<color=#9966FF>LOCK</color>";
                showSelection = true;
                break;
            case GravityGun.Mode.FlipMode:         modeText = "<color=#66FFB0>FLIP</color>";           showSelection = false; break;
            default:                               modeText = gun.CurrentMode.ToString();              showSelection = false; break;
        }

        // Tier badges: filled = unlocked, hollow = locked. Reads at a glance.
        int tier = gun.EffectiveTier;
        string tierBadges =
            (tier >= 1 ? "●" : "○") +
            (tier >= 2 ? "●" : "○") +
            (tier >= 3 ? "●" : "○") +
            (tier >= 4 ? "●" : "○");

        string selectionPart = showSelection
            ? $"  |  {SelectionCount()}/{gun.MaxSelectionCount}"
            : "";

        statusText.text = $"{modeText}{selectionPart}  |  T{tier} {tierBadges}";
    }

    private int SelectionCount()
    {
        return gun.selectedBodies != null ? gun.selectedBodies.Count : 0;
    }
}
