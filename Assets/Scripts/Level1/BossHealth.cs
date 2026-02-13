using UnityEngine;

public class BossHealth : MonoBehaviour, IDamageable
{
    [SerializeField] private float maxHealth = 300f;
    [SerializeField] private float currentHealth = 300f;

    public float MaxHealth => Mathf.Max(1f, maxHealth);
    public float CurrentHealth => currentHealth;
    public float Health01 => Mathf.Approximately(MaxHealth, 0f) ? 0f : currentHealth / MaxHealth;

    private void Awake()
    {
        maxHealth = Mathf.Max(1f, maxHealth);
        if (currentHealth <= 0f)
        {
            currentHealth = maxHealth;
        }
        else
        {
            currentHealth = Mathf.Clamp(currentHealth, 0f, maxHealth);
        }
    }

    public void SetMaxHealth(float value, bool refillCurrent = true)
    {
        maxHealth = Mathf.Max(1f, value);
        currentHealth = refillCurrent ? maxHealth : Mathf.Clamp(currentHealth, 0f, maxHealth);
    }

    public void TakeDamage(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
    }

    public void Heal(float amount)
    {
        if (amount <= 0f || currentHealth <= 0f)
        {
            return;
        }

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
    }
}
