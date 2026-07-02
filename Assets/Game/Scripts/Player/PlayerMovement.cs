using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float moveSpeed = 8f;
        [SerializeField] float groundAcceleration = 70f;
        [SerializeField] float groundDeceleration = 80f;
        [SerializeField] float airAcceleration = 45f;

        [Header("Jump")]
        [SerializeField] float jumpForce = 14f;
        [SerializeField] float jumpCutMultiplier = 0.45f;
        [SerializeField] float coyoteTime = 0.12f;
        [SerializeField] float jumpBufferTime = 0.12f;
        [SerializeField] float fallGravityMultiplier = 2.4f;
        [SerializeField] float lowJumpGravityMultiplier = 2f;

        [Header("Air Jump")]
        [SerializeField] float airJumpForce = 10f;
        [SerializeField] int maxAirJumps = 1;
        [SerializeField] bool airJumpEnabled = true;

        [Header("Collider")]
        [SerializeField] bool feetAtTransform = true;

        [Header("Ground Check")]
        [SerializeField] Transform groundCheck;
        [SerializeField] Vector2 groundCheckSize = new(0.55f, 0.08f);
        [SerializeField] LayerMask groundLayers = ~0;

        Rigidbody2D rb;
        CapsuleCollider2D bodyCollider;
        PlayerInput playerInput;
        GrapplingHookController grapple;
        PlayerFistCombat fistCombat;
        PlayerCombatMode combatMode;
        PlayerCharacterVisual characterVisual;
        InputAction moveAction;
        InputAction jumpAction;
        InputAction toggleAirJumpAction;
        SpriteRenderer spriteRenderer;

        float coyoteCounter;
        float jumpBufferCounter;
        int airJumpsRemaining;
        int facingDirection = 1;
        bool jumpHeld;
        bool wasGrounded;

        readonly Collider2D[] groundOverlapResults = new Collider2D[4];
        ContactFilter2D groundContactFilter;

        public bool IsGrounded { get; private set; }
        public int FacingDirection => facingDirection;
        public bool AirJumpEnabled => airJumpEnabled;

        public void RefreshAirJumps() => airJumpsRemaining = airJumpEnabled ? maxAirJumps : 0;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            playerInput = GetComponent<PlayerInput>();
            grapple = GetComponent<GrapplingHookController>();
            fistCombat = GetComponent<PlayerFistCombat>();
            combatMode = GetComponent<PlayerCombatMode>();
            characterVisual = GetComponentInChildren<PlayerCharacterVisual>();
            spriteRenderer = characterVisual != null
                ? characterVisual.GetComponent<SpriteRenderer>()
                : GetComponentInChildren<SpriteRenderer>();

            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];
            toggleAirJumpAction = playerInput.actions.FindAction("ToggleAirJump", false);

            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            EnsureGroundCheck();
            if (feetAtTransform)
                ApplyFeetPivotCollider();
            UpdateGroundCheckPosition();
            ConfigureGroundContactFilter();
        }

        void Start()
        {
            if (characterVisual == null)
                characterVisual = GetComponentInChildren<PlayerCharacterVisual>();

            if (fistCombat == null)
                fistCombat = GetComponent<PlayerFistCombat>();
        }

        float EffectiveMoveSpeed =>
            moveSpeed + (combatMode != null ? combatMode.HoldMoveSpeedBonus : 0f);

        bool IsPowerAttackLockingMovement =>
            fistCombat != null && fistCombat.IsPowerAttacking;

#if UNITY_EDITOR
        void OnValidate()
        {
            if (!feetAtTransform)
                return;

            if (bodyCollider == null)
                bodyCollider = GetComponent<CapsuleCollider2D>();

            ConfigureFeetPivotOffset();
            UpdateGroundCheckPosition();
        }
#endif

        void ConfigureFeetPivotOffset()
        {
            if (bodyCollider == null)
                return;

            float targetOffsetY = bodyCollider.size.y * 0.5f;
            Vector2 offset = bodyCollider.offset;
            if (Mathf.Approximately(offset.y, targetOffsetY))
                return;

            bodyCollider.offset = new Vector2(offset.x, targetOffsetY);
        }

        void ApplyFeetPivotCollider()
        {
            if (bodyCollider == null)
                return;

            Vector2 size = bodyCollider.size;
            float halfHeight = size.y * 0.5f;
            float targetOffsetY = halfHeight;

            if (Mathf.Approximately(bodyCollider.offset.y, targetOffsetY))
                return;

            float oldFootLocalY = bodyCollider.offset.y - halfHeight;
            float oldFootWorldY = transform.position.y + oldFootLocalY * transform.lossyScale.y;

            bodyCollider.offset = new Vector2(bodyCollider.offset.x, targetOffsetY);
            transform.position = new Vector3(transform.position.x, oldFootWorldY, transform.position.z);
        }

        void ConfigureGroundContactFilter()
        {
            groundContactFilter.useLayerMask = true;
            groundContactFilter.SetLayerMask(groundLayers);
            groundContactFilter.useTriggers = false;
        }

        void EnsureGroundCheck()
        {
            if (groundCheck != null)
                return;

            var checkObject = new GameObject("GroundCheck");
            checkObject.transform.SetParent(transform);
            groundCheck = checkObject.transform;
        }

        void UpdateGroundCheckPosition()
        {
            if (groundCheck == null || bodyCollider == null)
                return;

            Vector2 offset = bodyCollider.offset;
            float footY = offset.y - bodyCollider.size.y * 0.5f - groundCheckSize.y * 0.5f + 0.02f;
            groundCheck.localPosition = new Vector3(offset.x, footY, 0f);
        }

        void Update()
        {
            if (toggleAirJumpAction != null && toggleAirJumpAction.WasPressedThisFrame())
                SetAirJumpEnabled(!airJumpEnabled);

            bool blocksJump = (grapple != null && grapple.IsActive && !grapple.AllowsPlayerJump)
                || IsPowerAttackLockingMovement;
            bool blocksMovement = (grapple != null && grapple.BlocksPlayerMovement)
                || IsPowerAttackLockingMovement;

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            float moveX = blocksMovement ? 0f : moveInput.x;

            if (!blocksJump)
            {
                if (jumpAction.WasPressedThisFrame())
                    jumpBufferCounter = jumpBufferTime;
                else
                    jumpBufferCounter -= Time.deltaTime;

                jumpHeld = jumpAction.IsPressed();

                if (jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
                    rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);
            }

            if (!blocksMovement && Mathf.Abs(moveX) > 0.05f)
                facingDirection = moveX > 0f ? 1 : -1;

            if (characterVisual != null)
                characterVisual.ApplyMovement(moveX, !blocksMovement);
            else if (!blocksMovement)
                UpdateVisualFacing();
        }

        void FixedUpdate()
        {
            bool blocksJump = (grapple != null && grapple.IsActive && !grapple.AllowsPlayerJump)
                || IsPowerAttackLockingMovement;
            bool blocksMovement = (grapple != null && grapple.BlocksPlayerMovement)
                || IsPowerAttackLockingMovement;

            IsGrounded = CheckGrounded();

            if (IsGrounded && rb.linearVelocity.y <= 0.01f)
                coyoteCounter = coyoteTime;
            else if (!IsGrounded)
                coyoteCounter -= Time.fixedDeltaTime;

            if (IsGrounded && !wasGrounded)
                airJumpsRemaining = airJumpEnabled ? maxAirJumps : 0;

            wasGrounded = IsGrounded;

            if (!blocksJump && jumpBufferCounter > 0f)
            {
                if (coyoteCounter > 0f)
                {
                    Jump(jumpForce);
                    jumpBufferCounter = 0f;
                    coyoteCounter = 0f;
                }
                else if (airJumpEnabled && airJumpsRemaining > 0)
                {
                    Jump(airJumpForce);
                    airJumpsRemaining--;
                    jumpBufferCounter = 0f;
                }
            }

            if (!blocksMovement)
            {
                Vector2 moveInput = moveAction.ReadValue<Vector2>();
                float horizontalInput = grapple != null && grapple.IsApproachingMomentumAnchor
                    ? 0f
                    : moveInput.x;
                float vx = rb.linearVelocity.x;

                if (grapple != null && grapple.HasCarriedMomentum)
                {
                    vx = grapple.BleedHorizontalMomentum(
                        EffectiveMoveSpeed,
                        horizontalInput,
                        Time.fixedDeltaTime,
                        airAcceleration,
                        groundAcceleration,
                        groundDeceleration,
                        IsGrounded);
                    rb.linearVelocity = new Vector2(vx, rb.linearVelocity.y);
                }
                else if (Mathf.Abs(horizontalInput) > 0.05f)
                {
                    float targetSpeed = horizontalInput * EffectiveMoveSpeed;
                    float speedDifference = targetSpeed - vx;
                    float accelerationRate = IsGrounded ? groundAcceleration : airAcceleration;
                    float velocityChange = speedDifference * accelerationRate * Time.fixedDeltaTime;
                    rb.linearVelocity = new Vector2(vx + velocityChange, rb.linearVelocity.y);
                }
                else if (Mathf.Abs(vx) > 0.001f)
                {
                    float decelerationRate = IsGrounded ? groundDeceleration : airAcceleration;
                    float newVx = Mathf.MoveTowards(vx, 0f, decelerationRate * Time.fixedDeltaTime);
                    rb.linearVelocity = new Vector2(newVx, rb.linearVelocity.y);
                }
            }

            if (rb.linearVelocity.y < 0f)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                    (fallGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
            else if (rb.linearVelocity.y > 0f && !jumpHeld)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y *
                    (lowJumpGravityMultiplier - 1f) * Time.fixedDeltaTime;
            }
        }

        void SetAirJumpEnabled(bool enabled)
        {
            airJumpEnabled = enabled;
            airJumpsRemaining = enabled ? maxAirJumps : 0;
        }

        void Jump(float force)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        }

        bool CheckGrounded()
        {
            if (groundCheck == null)
                return false;

            int count = Physics2D.OverlapBox(
                groundCheck.position, groundCheckSize, 0f, groundContactFilter, groundOverlapResults);

            for (int i = 0; i < count; i++)
            {
                Collider2D hit = groundOverlapResults[i];
                if (hit == null)
                    continue;

                if (hit.transform == transform || hit.transform.IsChildOf(transform))
                    continue;

                return true;
            }

            return false;
        }

        void UpdateVisualFacing()
        {
            if (spriteRenderer == null)
                return;

            spriteRenderer.flipX = facingDirection < 0;
        }
    }
}
