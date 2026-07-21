using UnityEngine;

namespace ClockWork.Game
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCharacterVisual : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");
        static readonly int FaceLeftHash = Animator.StringToHash("FaceLeft");
        static readonly int IdleLeftHash = Animator.StringToHash("Idle_Left");
        static readonly int IdleRightHash = Animator.StringToHash("Idle_Right");
        static readonly int WalkHash = Animator.StringToHash("Walk");
        static readonly int DashHash = Animator.StringToHash("Dash");

        [SerializeField] float walkSpeedThreshold = 0.05f;

        SpriteRenderer spriteRenderer;
        Animator animator;
        int facingSign = 1;
        bool isWalking;
        bool isAttacking;

        public void SetAttacking(bool attacking)
        {
            isAttacking = attacking;
            if (!attacking)
                RestoreLocomotionState();
        }

        public void SetDashing(bool dashing)
        {
            if (animator == null)
                return;

            if (dashing)
                animator.Play(DashHash, 0, 0f);
            else
                RestoreLocomotionState();
        }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
        }

        void Start()
        {
            if (animator != null)
                animator.SetBool(FaceLeftHash, facingSign < 0);
        }

        public void ApplyMovement(float moveInputX, bool allowWalkAnimation)
        {
            if (Mathf.Abs(moveInputX) > walkSpeedThreshold)
                facingSign = moveInputX > 0f ? 1 : -1;

            isWalking = allowWalkAnimation && Mathf.Abs(moveInputX) > walkSpeedThreshold;

            if (animator == null)
                return;

            animator.SetBool(FaceLeftHash, facingSign < 0);
            animator.SetFloat(SpeedHash, isWalking ? Mathf.Abs(moveInputX) : 0f);
        }

        void LateUpdate()
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.flipX = ShouldMirrorSprite() && facingSign < 0;
        }

        bool ShouldMirrorSprite()
        {
            if (animator == null)
                return false;

            var state = animator.GetCurrentAnimatorStateInfo(0);
            return state.IsName("Walk")
                || state.IsName("Dash")
                || state.IsName("tick_attack_fist_1")
                || state.IsName("tick_attack_fist_2")
                || state.IsName("tick_attack_fist_3")
                || state.IsName("tick_attack_hammer_1")
                || state.IsName("tick_attack_hammer_2")
                || state.IsName("tick_attack_greatsword_1")
                || state.IsName("tick_attack_greatsword_2")
                || state.IsName("tick_attack_greatsword_3")
                || state.IsName("tick_attack_dagger_1")
                || state.IsName("tick_attack_dagger_2")
                || state.IsName("tick_attack_dagger_3");
        }

        void RestoreLocomotionState()
        {
            if (animator == null)
                return;

            if (isWalking)
                animator.Play(WalkHash, 0, 0f);
            else
                animator.Play(facingSign < 0 ? IdleLeftHash : IdleRightHash, 0, 0f);
        }
    }
}
