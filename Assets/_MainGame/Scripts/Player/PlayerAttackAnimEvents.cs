using UnityEngine;

namespace ClockWork.Game
{
    /// <summary>
    /// Animator가 붙은 Visual에서 애니 이벤트를 받아 Player 전투로 전달합니다.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class PlayerAttackAnimEvents : MonoBehaviour
    {
        PlayerFistCombat combat;

        void Awake()
        {
            combat = GetComponentInParent<PlayerFistCombat>();
        }

        void HitboxOn() => combat?.SetHitboxActive(true);

        void HitboxOff() => combat?.SetHitboxActive(false);
    }
}
