using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Health))]
    public class PlayerFistCombat : MonoBehaviour
    {
        readonly struct FistStrike
        {
            public readonly Vector2 offset;
            public readonly Vector2 hitboxSize;
            public readonly bool useRightHand;

            public FistStrike(Vector2 offset, Vector2 hitboxSize, bool useRightHand)
            {
                this.offset = offset;
                this.hitboxSize = hitboxSize;
                this.useRightHand = useRightHand;
            }
        }

        static readonly FistStrike[] ComboPattern =
        {
            new(new Vector2(0.55f, 0.05f), new Vector2(0.72f, 0.58f), true),
            new(new Vector2(0.38f, 0.08f), new Vector2(0.65f, 0.52f), false),
            new(new Vector2(0.62f, 0.06f), new Vector2(0.85f, 0.65f), true)
        };

        [Header("Fist")]
        [SerializeField] float damage = 1f;
        [SerializeField] float hitDuration = 0.1f;
        [SerializeField] float comboStepCooldown = 0.16f;
        [SerializeField] float comboResetTime = 0.55f;
        [SerializeField] float finisherCooldown = 0.32f;
        [SerializeField] Color hitboxColor = new(1f, 0.92f, 0.55f, 0.45f);

        PlayerInput playerInput;
        PlayerMovement movement;
        GrapplingHookController grapple;
        InputAction attackAction;

        Transform hitboxRoot;
        BoxCollider2D hitboxCollider;
        CapsuleCollider2D bodyCollider;
        DamageDealer hitboxDamage;
        SpriteRenderer hitboxVisual;

        int comboIndex;
        float comboWindowTimer;
        float attackCooldownCounter;
        bool isAttacking;

        public bool IsAttacking => isAttacking;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            movement = GetComponent<PlayerMovement>();
            grapple = GetComponent<GrapplingHookController>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            attackAction = playerInput.actions["Attack"];

            var health = GetComponent<Health>();
            health.DestroyOnDeath = false;

            if (GetComponent<WorldHealthBar>() == null)
            {
                var bar = gameObject.AddComponent<WorldHealthBar>();
                bar.Configure(WorldHealthBar.BarStyle.Player, showCumulativeDamage: false);
            }

            EnsureHitbox();
        }

        void EnsureHitbox()
        {
            hitboxRoot = transform.Find("FistHitbox");
            if (hitboxRoot == null)
            {
                var rootObject = new GameObject("FistHitbox");
                rootObject.transform.SetParent(transform);
                hitboxRoot = rootObject.transform;
            }

            hitboxCollider = hitboxRoot.GetComponent<BoxCollider2D>();
            if (hitboxCollider == null)
                hitboxCollider = hitboxRoot.gameObject.AddComponent<BoxCollider2D>();
            hitboxCollider.isTrigger = true;
            hitboxCollider.enabled = false;

            hitboxDamage = hitboxRoot.GetComponent<DamageDealer>();
            if (hitboxDamage == null)
                hitboxDamage = hitboxRoot.gameObject.AddComponent<DamageDealer>();
            hitboxDamage.Configure(damage);

            hitboxVisual = hitboxRoot.GetComponent<SpriteRenderer>();
            if (hitboxVisual == null)
                hitboxVisual = hitboxRoot.gameObject.AddComponent<SpriteRenderer>();
            hitboxVisual.sprite = CombatSpriteUtil.CreateRectSprite(8, 8, hitboxColor);
            hitboxVisual.drawMode = SpriteDrawMode.Sliced;
            hitboxVisual.sortingOrder = 5;
            hitboxVisual.enabled = false;
        }

        void Update()
        {
            if (grapple != null && grapple.IsActive)
                return;

            attackCooldownCounter -= Time.deltaTime;

            if (comboIndex > 0)
            {
                comboWindowTimer -= Time.deltaTime;
                if (comboWindowTimer <= 0f)
                    comboIndex = 0;
            }

            if (attackAction.WasPressedThisFrame())
                TryAttack();
        }

        void TryAttack()
        {
            if (isAttacking || attackCooldownCounter > 0f)
                return;

            StartCoroutine(StrikeRoutine(ComboPattern[comboIndex]));
            comboIndex = (comboIndex + 1) % ComboPattern.Length;
            comboWindowTimer = comboResetTime;

            attackCooldownCounter = comboIndex == 0 ? finisherCooldown : comboStepCooldown;
        }

        IEnumerator StrikeRoutine(FistStrike strike)
        {
            isAttacking = true;

            int facing = movement != null ? movement.FacingDirection : 1;
            float anchorY = bodyCollider != null ? bodyCollider.offset.y : 0f;
            hitboxRoot.localPosition = new Vector3(
                strike.offset.x * facing,
                anchorY + strike.offset.y,
                0f);
            hitboxCollider.size = strike.hitboxSize;
            hitboxVisual.size = strike.hitboxSize;
            hitboxVisual.color = strike.useRightHand
                ? hitboxColor
                : new Color(0.85f, 0.78f, 1f, 0.42f);
            hitboxDamage.ResetHits();
            hitboxDamage.Configure(damage);

            hitboxCollider.enabled = true;
            hitboxVisual.enabled = true;

            yield return new WaitForSeconds(hitDuration);

            hitboxCollider.enabled = false;
            hitboxVisual.enabled = false;
            isAttacking = false;
        }
    }
}
