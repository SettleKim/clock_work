using UnityEngine;

namespace ClockWork.Game
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class PlayerBackWeaponVisual : MonoBehaviour
    {
        SpriteRenderer spriteRenderer;
        PlayerMovement movement;
        Vector3 basePosition;
        Vector2 currentOffset;
        bool hasIcon;
        bool hiddenByAttack;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            movement = GetComponentInParent<PlayerMovement>();
            basePosition = transform.localPosition;
        }

        void LateUpdate()
        {
            if (spriteRenderer == null || movement == null)
                return;

            bool facingLeft = movement.FacingDirection < 0;
            spriteRenderer.flipX = facingLeft;

            float offsetX = facingLeft ? -currentOffset.x : currentOffset.x;
            transform.localPosition = basePosition + new Vector3(offsetX, currentOffset.y, 0f);
        }

        public void ShowWeapon(WeaponDefinition definition)
        {
            if (spriteRenderer == null)
                spriteRenderer = GetComponent<SpriteRenderer>();

            var icon = definition != null ? definition.BackIcon : null;
            spriteRenderer.sprite = icon;
            hasIcon = icon != null;
            currentOffset = definition != null ? definition.BackAttachOffset : Vector2.zero;
            ApplyVisibility();
        }

        public void SetHiddenForAttack(bool hidden)
        {
            hiddenByAttack = hidden;
            ApplyVisibility();
        }

        void ApplyVisibility()
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.enabled = hasIcon && !hiddenByAttack;
        }
    }
}
