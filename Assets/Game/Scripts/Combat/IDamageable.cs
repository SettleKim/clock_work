namespace ClockWork.Game
{
    public interface IDamageable
    {
        bool IsAlive { get; }
        void ApplyDamage(in DamageInfo info);
    }
}
