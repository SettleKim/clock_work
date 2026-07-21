using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    /// <summary>
    /// 지상·공중 대시. PlayerMovement 옆에 붙는 별도 컴포넌트로,
    /// 대시 중에는 Rigidbody를 직접 몰고(수평 고정·중력 0), PlayerMovement 는
    /// <see cref="IsDashing"/> 을 보고 이동/점프/중력을 비켜준다.
    /// (GrapplingHookController 와 동일한 컴포넌트 조합 패턴)
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerDash : MonoBehaviour
    {
        [Header("Dash (Game 스케일 — 걷기 8 기준)")]
        [SerializeField] float dashSpeed = 16f;
        [SerializeField] float dashDuration = 0.32f;
        [SerializeField] float dashCooldown = 0.54f;
        [SerializeField] bool allowAirDash = true;

        Rigidbody2D rb;
        PlayerInput playerInput;
        PlayerMovement movement;
        GrapplingHookController grapple;
        PlayerFistCombat fistCombat;
        PlayerCharacterVisual characterVisual;
        InputAction dashAction;

        float dashTimer;
        float cooldownTimer;
        int dashDirection = 1;
        float cachedGravityScale;
        bool dashPressed;

        public bool IsDashing => dashTimer > 0f;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            playerInput = GetComponent<PlayerInput>();
            movement = GetComponent<PlayerMovement>();
            grapple = GetComponent<GrapplingHookController>();
            fistCombat = GetComponent<PlayerFistCombat>();
            characterVisual = GetComponentInChildren<PlayerCharacterVisual>();
            dashAction = playerInput.actions.FindAction("Dash", false);
        }

        void Start()
        {
            // PlayerFistCombat 은 GameBootstrap 이 런타임에 AddComponent 하므로 Awake 이후 재취득.
            if (fistCombat == null)
                fistCombat = GetComponent<PlayerFistCombat>();
            if (characterVisual == null)
                characterVisual = GetComponentInChildren<PlayerCharacterVisual>();
        }

        void Update()
        {
            if (dashAction != null && dashAction.WasPressedThisFrame())
                dashPressed = true;
        }

        void FixedUpdate()
        {
            float dt = Time.fixedDeltaTime;
            cooldownTimer = Mathf.Max(0f, cooldownTimer - dt);

            if (dashPressed)
            {
                dashPressed = false;
                TryStartDash();
            }

            if (dashTimer > 0f)
            {
                dashTimer -= dt;
                rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);

                if (dashTimer <= 0f)
                    EndDash();
            }
        }

        void TryStartDash()
        {
            if (dashTimer > 0f || cooldownTimer > 0f)
                return;
            if (!allowAirDash && movement != null && !movement.IsGrounded)
                return;
            if (IsBlocked())
                return;

            dashDirection = movement != null ? movement.FacingDirection : 1;
            if (dashDirection == 0)
                dashDirection = 1;

            dashTimer = dashDuration;
            cooldownTimer = dashCooldown;
            cachedGravityScale = rb.gravityScale;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(dashDirection * dashSpeed, 0f);
            characterVisual?.SetDashing(true);
        }

        void EndDash()
        {
            dashTimer = 0f;
            rb.gravityScale = cachedGravityScale;
            characterVisual?.SetDashing(false);
        }

        // 파워어택 중이거나 그랩이 이동을 잠근 상태에서는 대시 시작 금지 (이동 잠금과 동일 규칙).
        bool IsBlocked()
        {
            if (fistCombat != null && fistCombat.IsPowerAttacking)
                return true;
            if (grapple != null && grapple.BlocksPlayerMovement)
                return true;
            return false;
        }

        void OnDisable()
        {
            if (dashTimer > 0f)
                EndDash();
        }
    }
}
