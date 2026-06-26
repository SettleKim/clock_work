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

        [Header("Ground Check")]
        [SerializeField] Transform groundCheck;
        [SerializeField] Vector2 groundCheckSize = new(0.55f, 0.08f);
        [SerializeField] LayerMask groundLayers = ~0;

        Rigidbody2D rb;
        CapsuleCollider2D bodyCollider;
        PlayerInput playerInput;
        GrapplingHookController grapple;
        InputAction moveAction;
        InputAction jumpAction;
        SpriteRenderer spriteRenderer;

        float coyoteCounter;
        float jumpBufferCounter;
        int airJumpsRemaining;
        int facingDirection = 1;
        bool jumpHeld;
        bool wasGrounded;

        readonly Collider2D[] groundOverlapResults = new Collider2D[4];

        public bool IsGrounded { get; private set; }
        public int FacingDirection => facingDirection;

        void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            playerInput = GetComponent<PlayerInput>();
            grapple = GetComponent<GrapplingHookController>();
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

            moveAction = playerInput.actions["Move"];
            jumpAction = playerInput.actions["Jump"];

            rb.gravityScale = 3f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            EnsureGroundCheck();
            UpdateGroundCheckPosition();
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
            if (grapple != null && grapple.IsActive)
                return;

            if (jumpAction.WasPressedThisFrame())
                jumpBufferCounter = jumpBufferTime;
            else
                jumpBufferCounter -= Time.deltaTime;

            jumpHeld = jumpAction.IsPressed();

            if (jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            if (Mathf.Abs(moveInput.x) > 0.05f)
                facingDirection = moveInput.x > 0f ? 1 : -1;

            UpdateVisualFacing();
        }

        void FixedUpdate()
        {
            if (grapple != null && grapple.IsActive)
                return;

            IsGrounded = CheckGrounded();

            if (IsGrounded && rb.linearVelocity.y <= 0.01f)
                coyoteCounter = coyoteTime;
            else if (!IsGrounded)
                coyoteCounter -= Time.fixedDeltaTime;

            if (IsGrounded && !wasGrounded)
                airJumpsRemaining = maxAirJumps;

            wasGrounded = IsGrounded;

            if (jumpBufferCounter > 0f)
            {
                if (coyoteCounter > 0f)
                {
                    Jump(jumpForce);
                    jumpBufferCounter = 0f;
                    coyoteCounter = 0f;
                }
                else if (airJumpsRemaining > 0)
                {
                    Jump(airJumpForce);
                    airJumpsRemaining--;
                    jumpBufferCounter = 0f;
                }
            }

            Vector2 moveInput = moveAction.ReadValue<Vector2>();
            float horizontalInput = moveInput.x;

            bool preserveMomentum = grapple != null && grapple.ShouldPreserveLaunchMomentum;
            if (preserveMomentum && Mathf.Abs(horizontalInput) > 0.05f)
                grapple.ClearMomentumPreserve();

            if (!preserveMomentum || Mathf.Abs(horizontalInput) > 0.05f)
            {
                float targetSpeed = horizontalInput * moveSpeed;
                float speedDifference = targetSpeed - rb.linearVelocity.x;
                float accelerationRate = IsGrounded
                    ? Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration
                    : airAcceleration;
                float velocityChange = speedDifference * accelerationRate * Time.fixedDeltaTime;
                rb.linearVelocity = new Vector2(rb.linearVelocity.x + velocityChange, rb.linearVelocity.y);
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

        void Jump(float force)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, force);
        }

        bool CheckGrounded()
        {
            if (groundCheck == null)
                return false;

            int count = Physics2D.OverlapBoxNonAlloc(
                groundCheck.position, groundCheckSize, 0f, groundOverlapResults, groundLayers);

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
