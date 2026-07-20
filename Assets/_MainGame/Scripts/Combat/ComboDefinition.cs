using UnityEngine;

namespace ClockWork.Game
{
    [CreateAssetMenu(fileName = "ComboDefinition", menuName = "Clock Work/Combat/Combo Definition")]
    public class ComboDefinition : ScriptableObject
    {
        [System.Serializable]
        public struct Step
        {
            [Tooltip("플레이어 기준 히트박스 위치 (x는 바라보는 방향으로 반전)")]
            public Vector2 hitboxOffset;

            [Tooltip("BoxCollider2D size")]
            public Vector2 hitboxSize;

            public bool useRightHand;

            [Tooltip("이 타 데미지. 0이면 PlayerFistCombat 기본 damage 사용")]
            public float damage;

            [Tooltip("이 타 모션 유지 시간(초). ComboMotionHold")]
            public float motionHold;
        }

        [SerializeField] Step[] steps =
        {
            new() { hitboxOffset = new Vector2(0.55f, 0.05f), hitboxSize = new Vector2(0.72f, 0.58f), useRightHand = true, damage = 1f, motionHold = 0.35f },
            new() { hitboxOffset = new Vector2(0.38f, 0.08f), hitboxSize = new Vector2(0.65f, 0.52f), useRightHand = false, damage = 1f, motionHold = 0.6f },
            new() { hitboxOffset = new Vector2(0.62f, 0.06f), hitboxSize = new Vector2(0.85f, 0.65f), useRightHand = true, damage = 1f, motionHold = 0.6f }
        };

        public int StepCount => steps != null ? steps.Length : 0;

        public Step GetStep(int index)
        {
            if (steps == null || steps.Length == 0)
                return default;

            return steps[Mathf.Clamp(index, 0, steps.Length - 1)];
        }
    }
}
