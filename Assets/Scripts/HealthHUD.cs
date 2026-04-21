using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Drives the player's health HUD display.
/// </summary>
public class HealthHUD : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Reference to the player's health component.")]
    private PlayerHealth playerHealth;

    [SerializeField]
    [Tooltip("Image component used for the health bar fill.")]
    private Image healthBarFill;

    [SerializeField]
    [Tooltip("TextMeshPro component used for the numeric health readout.")]
    private TextMeshProUGUI healthValueText;

    [SerializeField]
    [Tooltip("Color when health is above 60%.")]
    private Color healthyColor = new Color(0.2f, 0.8f, 0.2f);

    [SerializeField]
    [Tooltip("Color when health is between 30% and 60%.")]
    private Color warningColor = new Color(1f, 0.8f, 0.2f);

    [SerializeField]
    [Tooltip("Color when health is below 30%.")]
    private Color criticalColor = new Color(1f, 0.2f, 0.2f);

    private float pulsingAlpha = 1f;

    private void Awake()
    {
        if (playerHealth == null)
        {
            playerHealth = FindObjectOfType<PlayerHealth>();
        }
    }

    private void OnEnable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged += UpdateHUD;
            UpdateHUD(playerHealth.CurrentHealth, playerHealth.MaxHealth);
        }
    }

    private void OnDisable()
    {
        if (playerHealth != null)
        {
            playerHealth.OnHealthChanged -= UpdateHUD;
        }
    }

    private void Update()
    {
        if (healthBarFill == null)
            return;

        float healthPercent = playerHealth != null ? playerHealth.HealthPercent : 0f;
        Color barColor = healthBarFill.color;

        if (healthPercent < 0.3f)
        {
            // Pulsing animation when critical
            pulsingAlpha = 0.5f + Mathf.Sin(Time.time * 4f) * 0.5f;
            barColor.a = pulsingAlpha;
        }
        else
        {
            // Ensure full alpha when health is not critical
            barColor.a = 1f;
        }

        healthBarFill.color = barColor;
    }

    private void UpdateHUD(float currentHealth, float maxHealth)
    {
        if (healthBarFill != null)
        {
            float healthPercent = maxHealth > 0f ? currentHealth / maxHealth : 0f;
            healthBarFill.fillAmount = healthPercent;

            Color barColor;
            if (healthPercent > 0.6f)
            {
                barColor = healthyColor;
            }
            else if (healthPercent > 0.3f)
            {
                barColor = warningColor;
            }
            else
            {
                barColor = criticalColor;
            }
            barColor.a = 1f;
            healthBarFill.color = barColor;
        }

        if (healthValueText != null)
        {
            healthValueText.text = Mathf.RoundToInt(currentHealth) + " / " + Mathf.RoundToInt(maxHealth);
        }
    }
}
