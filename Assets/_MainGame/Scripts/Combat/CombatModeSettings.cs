using UnityEngine;

namespace ClockWork.Game
{
    [CreateAssetMenu(fileName = "CombatModeSettings", menuName = "Clock Work/Combat/Combat Mode Settings")]
    public class CombatModeSettings : ScriptableObject
    {
        [Header("Timing")]
        [SerializeField] float holdThreshold = 0.5f;
        [SerializeField] float tapWindowDuration = 1f;
        [SerializeField] float wCooldownDuration = 1f;
        [SerializeField] float slowMotionScale = 0.16f;

        [Header("Energy")]
        [SerializeField] float tapEntryCost = 25f;
        [SerializeField] float holdEntryCost = 5f;
        [SerializeField] float holdDrainPerSecond = 5f;
        [SerializeField] float tapConfirmCost = 25f;
        [SerializeField] float tapTimeoutCost = 5f;

        [Header("Movement")]
        [SerializeField] float holdMoveSpeedBonus = 5f;

        public float HoldThreshold => holdThreshold;
        public float TapWindowDuration => tapWindowDuration;
        public float WCooldownDuration => wCooldownDuration;
        public float SlowMotionScale => slowMotionScale;
        public float TapEntryCost => tapEntryCost;
        public float HoldEntryCost => holdEntryCost;
        public float HoldDrainPerSecond => holdDrainPerSecond;
        public float TapConfirmCost => tapConfirmCost;
        public float TapTimeoutCost => tapTimeoutCost;
        public float HoldMoveSpeedBonus => holdMoveSpeedBonus;
    }
}
