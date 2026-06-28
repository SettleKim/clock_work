using UnityEngine;

namespace ClockWork.Game
{
    public readonly struct DamageInfo
    {
        public float Amount { get; }
        public DamageType Type { get; }
        public GameObject Source { get; }

        public DamageInfo(float amount, DamageType type, GameObject source)
        {
            Amount = amount;
            Type = type;
            Source = source;
        }

        public static DamageInfo Physical(float amount, GameObject source) =>
            new(amount, DamageType.Physical, source);
    }
}
