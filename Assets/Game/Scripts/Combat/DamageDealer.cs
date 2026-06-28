using System.Collections.Generic;
using UnityEngine;

namespace ClockWork.Game
{
    [RequireComponent(typeof(Collider2D))]
    public class DamageDealer : MonoBehaviour
    {
        [SerializeField] float damage = 1f;
        [SerializeField] DamageType damageType = DamageType.Physical;
        [SerializeField] string[] targetTags = { "Enemy" };
        [SerializeField] bool destroyOnHit;
        [SerializeField] float hitCooldown = 0.08f;

        readonly HashSet<Health> hitTargets = new();
        float lastHitTime;

        public float Damage => damage;

        public void Configure(float newDamage, DamageType type = DamageType.Physical, bool shouldDestroyOnHit = false)
        {
            damage = newDamage;
            damageType = type;
            destroyOnHit = shouldDestroyOnHit;
        }

        public void ResetHits()
        {
            hitTargets.Clear();
        }

        void OnEnable()
        {
            hitTargets.Clear();
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            TryDamage(other.gameObject);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time - lastHitTime >= hitCooldown)
                TryDamage(other.gameObject);
        }

        void TryDamage(GameObject target)
        {
            if (!IsValidTarget(target))
                return;

            if (!target.TryGetComponent(out Health health) || health.IsDead || hitTargets.Contains(health))
                return;

            hitTargets.Add(health);
            lastHitTime = Time.time;
            health.ApplyDamage(DamageInfo.Physical(damage, gameObject));

            if (destroyOnHit)
                Destroy(gameObject);
        }

        bool IsValidTarget(GameObject target)
        {
            if (targetTags == null || targetTags.Length == 0)
                return false;

            for (int i = 0; i < targetTags.Length; i++)
            {
                if (target.CompareTag(targetTags[i]))
                    return true;
            }

            return false;
        }
    }
}
