using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Manages the player's health, death, and respawn behavior.
/// </summary>
public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    [Tooltip("Maximum health value for the player.")]
    private float maxHealth = 100f;

    private float currentHealth;

    [SerializeField]
    [Tooltip("Reference to the player's battery system used to reset charge on respawn.")]
    private GunBatterySystem batterySystem;

    private Rigidbody playerRigidbody;
    private GravityBody playerGravityBody;
    private DroppedLoot droppedLoot;

    public event Action<float, float> OnHealthChanged;
    public event Action OnPlayerDied;

    public float CurrentHealth => currentHealth;
    public float MaxHealth => maxHealth;
    public float HealthPercent => currentHealth / maxHealth;

    private void Awake()
    {
        currentHealth = maxHealth;
        playerRigidbody = GetComponent<Rigidbody>();
        playerGravityBody = GetComponent<GravityBody>();
        droppedLoot = GetComponent<DroppedLoot>();

        if (batterySystem == null)
        {
            batterySystem = GetComponent<GunBatterySystem>() ?? GetComponentInChildren<GunBatterySystem>();
        }
    }

    private void Start()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        float newHealth = Mathf.Clamp(currentHealth - amount, 0f, maxHealth);
        if (!Mathf.Approximately(newHealth, currentHealth))
        {
            currentHealth = newHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        float newHealth = Mathf.Clamp(currentHealth + amount, 0f, maxHealth);
        if (!Mathf.Approximately(newHealth, currentHealth))
        {
            currentHealth = newHealth;
            OnHealthChanged?.Invoke(currentHealth, maxHealth);
        }
    }

    private void Die()
    {
        OnPlayerDied?.Invoke();

        ScreenFade fade = ScreenFade.Instance;
        if (fade != null)
        {
            fade.FadeOut(2f, RespawnPlayer);
        }
        else
        {
            StartCoroutine(RespawnPlayerAfterDelay(2f));
        }
    }

    private IEnumerator RespawnPlayerAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        RespawnPlayer();
    }

    private void RespawnPlayer()
    {
        GameObject spawnPoint = GameObject.Find("PlayerSpawnpoint");
        if (spawnPoint == null)
        {
            Debug.LogError("PlayerSpawnpoint not found in scene. Please add an empty GameObject named exactly 'PlayerSpawnpoint'.");
            return;
        }

        transform.position = spawnPoint.transform.position;
        transform.rotation = spawnPoint.transform.rotation;

        if (playerRigidbody != null)
        {
            playerRigidbody.linearVelocity = Vector3.zero;
            playerRigidbody.angularVelocity = Vector3.zero;
        }

        currentHealth = maxHealth;
        OnHealthChanged?.Invoke(currentHealth, maxHealth);

        if (playerGravityBody != null)
        {
            playerGravityBody.SetGravity(Vector3.down, playerGravityBody.gravityStrength);
        }

        if (batterySystem != null && batterySystem.CurrentCell != null)
        {
            float targetCharge = batterySystem.CurrentCell.MaxCharge * 0.25f;
            batterySystem.SetCharge(targetCharge);
        }

        if (droppedLoot != null)
        {
            droppedLoot.SpawnLootBag(transform.position);
        }

        ScreenFade fade = ScreenFade.Instance;
        if (fade != null)
        {
            fade.FadeIn(1f);
        }
    }
}
