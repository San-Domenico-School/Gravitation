using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages the visual battery HUD display. Shows charge bar, cell name, percentage,
/// and changes color based on charge level. Pulses at critical levels.
/// </summary>
public class BatteryHUD : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the GunBatterySystem")]
    private GunBatterySystem batterySystem;

    [SerializeField]
    [Tooltip("Image component for the charge bar fill")]
    private Image chargeBarFill;

    [SerializeField]
    [Tooltip("TextMeshPro component for cell name")]
    private TextMeshProUGUI cellNameText;

    [SerializeField]
    [Tooltip("TextMeshPro component for percentage")]
    private TextMeshProUGUI chargePercentText;

    [SerializeField]
    [Tooltip("Color when charge is above 50%")]
    private Color normalColor = new Color(0.2f, 0.8f, 1f); // Cool blue/teal

    [SerializeField]
    [Tooltip("Color when charge is 25-50%")]
    private Color warningColor = new Color(1f, 0.8f, 0.2f); // Amber/yellow

    [SerializeField]
    [Tooltip("Color when charge is below 25%")]
    private Color criticalColor = new Color(1f, 0.2f, 0.2f); // Red

    private float lastChargePercent = 1f;
    private float pulsingAlpha = 1f;

    private void OnEnable()
    {
        if (batterySystem != null)
        {
            batterySystem.OnChargeChanged += UpdateHUD;
            // Initial update
            UpdateHUD(batterySystem.CurrentCharge, batterySystem.CurrentCell?.MaxCharge ?? 1f);
        }
    }

    private void OnDisable()
    {
        if (batterySystem != null)
        {
            batterySystem.OnChargeChanged -= UpdateHUD;
        }
    }

    private void Update()
    {
        // Handle pulsing effect at critical levels
        float chargePercent = batterySystem?.ChargePercent ?? 0f;
        if (chargePercent < 0.25f)
        {
            // Pulse using sine wave
            pulsingAlpha = 0.5f + Mathf.Sin(Time.time * 4f) * 0.5f; // Range 0-1, oscillates 4 times per second
            if (chargeBarFill != null)
            {
                Color barColor = chargeBarFill.color;
                barColor.a = pulsingAlpha;
                chargeBarFill.color = barColor;
            }
        }
    }

    private void UpdateHUD(float currentCharge, float maxCharge)
    {
        if (batterySystem?.CurrentCell == null)
            return;

        float chargePercent = maxCharge > 0 ? currentCharge / maxCharge : 0f;

        // Update bar fill
        if (chargeBarFill != null)
        {
            chargeBarFill.fillAmount = chargePercent;

            // Update bar color based on charge level
            Color barColor;
            if (chargePercent > 0.5f)
            {
                barColor = normalColor;
            }
            else if (chargePercent > 0.25f)
            {
                barColor = warningColor;
            }
            else
            {
                barColor = criticalColor;
            }
            barColor.a = 1f; // Reset alpha to full (will be adjusted in Update if critical)
            chargeBarFill.color = barColor;
        }

        // Update cell name
        if (cellNameText != null)
        {
            cellNameText.text = batterySystem.CurrentCell.CellName.ToUpper();
        }

        // Update percentage text
        if (chargePercentText != null)
        {
            chargePercentText.text = Mathf.RoundToInt(chargePercent * 100f) + "%";
        }

        lastChargePercent = chargePercent;
    }
}
