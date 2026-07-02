using System;
using UnityEngine;

namespace ClockWork.Game
{
    public class PlayerEnergyGauge : MonoBehaviour
    {
        [SerializeField] float maxEnergy = 100f;
        [SerializeField] float energyPerSegment = 5f;
        [SerializeField] float energyPerHit = 5f;

        float currentEnergy;
        DamageDealer damageDealer;

        public float MaxEnergy => maxEnergy;
        public float CurrentEnergy => currentEnergy;
        public float EnergyPerSegment => energyPerSegment;
        public int SegmentCount => Mathf.Max(1, Mathf.RoundToInt(maxEnergy / energyPerSegment));
        public int FilledSegmentCount => Mathf.Clamp(Mathf.FloorToInt(currentEnergy / energyPerSegment), 0, SegmentCount);

        public event Action<float, float> OnEnergyChanged;

        public bool HasEnough(float amount) => currentEnergy >= amount;

        /// <summary>슬로모 등 timeScale 영향 없이 연속 드레인. 남은 에너지가 0 초과면 true.</summary>
        public bool DrainContinuous(float perSecond, float unscaledDeltaTime)
        {
            if (perSecond <= 0f)
                return currentEnergy > 0f;

            if (unscaledDeltaTime <= 0f)
                return currentEnergy > 0f;

            float previous = currentEnergy;
            float drain = perSecond * unscaledDeltaTime;
            currentEnergy = Mathf.Max(0f, currentEnergy - drain);

            if (!Mathf.Approximately(previous, currentEnergy))
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);

            return currentEnergy > 0f;
        }

        void Start()
        {
            BindDamageDealer();
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        void BindDamageDealer()
        {
            if (damageDealer != null)
                return;

            var combat = GetComponent<PlayerFistCombat>();
            if (combat != null && combat.HitboxDamage != null)
                damageDealer = combat.HitboxDamage;
            else
            {
                var hitbox = transform.Find("FistHitbox");
                if (hitbox != null)
                    damageDealer = hitbox.GetComponent<DamageDealer>();
            }

            if (damageDealer != null)
                damageDealer.OnHit += OnEnemyHit;
            else
                Debug.LogWarning("[PlayerEnergyGauge] FistHitbox DamageDealer not found — energy will not charge on hit.");
        }

        void OnDestroy()
        {
            if (damageDealer != null)
                damageDealer.OnHit -= OnEnemyHit;
        }

        void OnEnemyHit(Health target)
        {
            if (target == null)
                return;

            var combat = GetComponent<PlayerFistCombat>();
            if (combat != null && combat.IsPowerAttacking)
                return;

            Add(energyPerHit);
        }

        public void Add(float amount)
        {
            if (amount <= 0f)
                return;

            float previous = currentEnergy;
            currentEnergy = Mathf.Min(maxEnergy, currentEnergy + amount);
            if (!Mathf.Approximately(previous, currentEnergy))
                OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
        }

        public bool TrySpend(float amount)
        {
            if (amount <= 0f || currentEnergy < amount)
                return false;

            currentEnergy -= amount;
            OnEnergyChanged?.Invoke(currentEnergy, maxEnergy);
            return true;
        }
    }
}
