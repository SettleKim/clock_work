using UnityEngine;

namespace ClockWork.Game
{
    [CreateAssetMenu(fileName = "PowerAttackDefinition", menuName = "Clock Work/Combat/Power Attack Definition")]
    public class PowerAttackDefinition : ScriptableObject
    {
        [SerializeField] int[] pattern = { 2, 1, 2, 1, 2, 1, 2, 1, 2, 3 };
        [SerializeField] float damagePerHit = 1.5f;
        [SerializeField] float strikeInterval = 0.12f;
        [SerializeField] float hitActiveDuration = 0.08f;
        [SerializeField] float finisherHoldDuration = 0.5f;
        [SerializeField] ComboDefinition hitboxReference;

        public int PatternLength => pattern != null ? pattern.Length : 0;
        public float DamagePerHit => damagePerHit;
        public float StrikeInterval => strikeInterval;
        public float HitActiveDuration => hitActiveDuration;
        public float FinisherHoldDuration => finisherHoldDuration;
        public ComboDefinition HitboxReference => hitboxReference;

        public int GetPatternStep(int index)
        {
            if (pattern == null || pattern.Length == 0)
                return 1;

            return pattern[Mathf.Clamp(index, 0, pattern.Length - 1)];
        }

        /// <summary>attack_fist_con(1~3)에 대응하는 히트박스 Step.</summary>
        public ComboDefinition.Step GetHitboxForCon(int con)
        {
            int stepIndex = Mathf.Clamp(con - 1, 0, 2);
            if (hitboxReference != null && hitboxReference.StepCount > 0)
                return hitboxReference.GetStep(stepIndex);

            return default;
        }
    }
}
