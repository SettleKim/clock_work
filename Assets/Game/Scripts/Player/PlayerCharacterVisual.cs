using UnityEngine;

namespace ClockWork.Game
{
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(Animator))]
    public class PlayerCharacterVisual : MonoBehaviour
    {
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] float walkSpeedThreshold = 0.05f;
        [SerializeField] Sprite idleRightSprite;
        [SerializeField] Sprite idleLeftSprite;

        SpriteRenderer spriteRenderer;
        Animator animator;
        int facingSign = 1;
        bool isWalking;

        public void ConfigureIdleSprites(Sprite right, Sprite left)
        {
            if (right != null)
                idleRightSprite = right;
            if (left != null)
                idleLeftSprite = left;
        }

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            animator = GetComponent<Animator>();
            ResolveIdleSpritesIfNeeded();
        }

        public void ApplyMovement(float moveInputX, bool allowWalkAnimation)
        {
            if (Mathf.Abs(moveInputX) > walkSpeedThreshold)
                facingSign = moveInputX > 0f ? 1 : -1;

            isWalking = allowWalkAnimation && Mathf.Abs(moveInputX) > walkSpeedThreshold;

            if (animator != null)
            {
                float speed = isWalking ? Mathf.Abs(moveInputX) : 0f;
                animator.SetFloat(SpeedHash, speed);
            }
        }

        void LateUpdate()
        {
            if (spriteRenderer == null)
                return;

            if (isWalking)
            {
                spriteRenderer.flipX = facingSign < 0;
                return;
            }

            ApplyIdleSprite();
        }

        void ApplyIdleSprite()
        {
            spriteRenderer.flipX = false;

            if (facingSign < 0 && idleLeftSprite != null)
                spriteRenderer.sprite = idleLeftSprite;
            else if (facingSign > 0 && idleRightSprite != null)
                spriteRenderer.sprite = idleRightSprite;
        }

        void ResolveIdleSpritesIfNeeded()
        {
            if (idleRightSprite != null && idleLeftSprite != null)
                return;

            if (PlayerSpriteSheetResolver.TryGetIdleSprites(out Sprite left, out Sprite right))
            {
                idleLeftSprite ??= left;
                idleRightSprite ??= right;
            }
        }

#if UNITY_EDITOR
        void OnValidate() => ResolveIdleSpritesIfNeeded();
#endif
    }
}
