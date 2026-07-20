using System;
using UnityEngine;

namespace ClockWork.Game
{
    public class Health : MonoBehaviour, IDamageable
    {
        [SerializeField] float maxHealth = 10f;
        [SerializeField] bool destroyOnDeath = true;

        float currentHealth;
        bool isDead;

        public float MaxHealth => maxHealth;
        public float CurrentHealth => currentHealth;
        public bool IsDead => isDead;
        public bool IsAlive => !isDead;
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

        public void ApplyDamage(in DamageInfo info)
        {
            TakeDamage(info.Amount, info.Source);
        }

        public void TakeDamage(float amount, GameObject source = null)
        {
            if (isDead || amount <= 0f)
                return;

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
}
