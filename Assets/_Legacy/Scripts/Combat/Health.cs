using System;
using UnityEngine;

public class Health : MonoBehaviour
{
    [SerializeField] float maxHealth = 5f;
    [SerializeField] bool destroyOnDeath = true;

    float currentHealth;
    bool isDead;
    bool isPlayer;
    PlayerParryController parry;
    PlayerComboController combo;

    public float MaxHealth => maxHealth;
    public float CurrentHealth => currentHealth;
    public bool IsDead => isDead;
    public bool DestroyOnDeath
    {
        get => destroyOnDeath;
        set => destroyOnDeath = value;
    }

    public event Action<float, float> HealthChanged;
    public event Action<float> Damaged;
    public event Action Died;

    void Awake()
    {
        currentHealth = maxHealth;
        isPlayer = CompareTag("Player");
    }

    void Start()
    {
        if (isPlayer)
            CachePlayerCombatRefs();
    }

    void CachePlayerCombatRefs()
    {
        if (parry == null)
            TryGetComponent(out parry);
        if (combo == null)
            TryGetComponent(out combo);
    }

    public void Configure(float newMaxHealth, bool shouldDestroyOnDeath)
    {
        maxHealth = newMaxHealth;
        destroyOnDeath = shouldDestroyOnDeath;
        ResetHealth();
    }

    public void ResetHealth()
    {
        isDead = false;
        currentHealth = maxHealth;
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void TakeDamage(float amount, GameObject source = null)
    {
        if (isDead || amount <= 0f)
            return;

        if (isPlayer)
        {
            if (parry == null || combo == null)
                CachePlayerCombatRefs();

            if (parry != null && parry.TryParryIncomingDamage(amount, source))
                return;

            if (combo != null && combo.IsInvulnerable)
                return;
        }

        currentHealth = Mathf.Max(0f, currentHealth - amount);
        Damaged?.Invoke(amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);

        if (currentHealth <= 0f)
            Die();
    }

    public void Heal(float amount)
    {
        if (isDead || amount <= 0f)
            return;

        currentHealth = Mathf.Min(maxHealth, currentHealth + amount);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    public void SetCurrentHealth(float value)
    {
        isDead = false;
        currentHealth = Mathf.Clamp(value, 0f, maxHealth);
        HealthChanged?.Invoke(currentHealth, maxHealth);
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;
        Died?.Invoke();

        if (destroyOnDeath)
            Destroy(gameObject);
    }
}
