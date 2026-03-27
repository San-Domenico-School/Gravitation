using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField]
    private float maxHealth = 3f;

    [SerializeField]
    private float startingHealth = 3f;

    public float CurrentHealth { get; private set; }

    public float MaxHealth => maxHealth;

    private void Awake()
    {
        CurrentHealth = Mathf.Clamp(startingHealth, 0f, maxHealth);
    }

    public void SetHealth(float health)
    {
        CurrentHealth = Mathf.Clamp(health, 0f, maxHealth);
    }

    public bool Heal(float amount)
    {
        if (amount <= 0f)
            return false;

        float previous = CurrentHealth;
        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        return !Mathf.Approximately(previous, CurrentHealth);
    }

    public bool Damage(float amount)
    {
        if (amount <= 0f)
            return false;

        float previous = CurrentHealth;
        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        return !Mathf.Approximately(previous, CurrentHealth);
    }
}
