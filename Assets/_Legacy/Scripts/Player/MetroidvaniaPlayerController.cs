using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(CapsuleCollider2D))]
[RequireComponent(typeof(PlayerInput))]
public class MetroidvaniaPlayerController : MonoBehaviour
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

    [Header("Wall")]
    [SerializeField] float wallSlideSpeed = 1.75f;
    [SerializeField] Vector2 wallJumpForce = new(9f, 14f);
    [SerializeField] float wallJumpControlLock = 0.16f;

    [Header("Dash")]
    [SerializeField] float dashSpeed = 20f;
    [SerializeField] float dashDuration = 0.16f;
    [SerializeField] float dashCooldown = 0.35f;

    [Header("Checks")]
    [SerializeField] Transform groundCheck;
    [SerializeField] Transform wallCheck;
    [SerializeField] Vector2 groundCheckSize = new(0.55f, 0.08f);
    [SerializeField] float wallCheckDistance = 0.45f;
    [SerializeField] LayerMask groundLayers = ~0;

    [Header("Visual")]
    [SerializeField] Transform visual;
    [SerializeField] SpriteRenderer spriteRenderer;

    Rigidbody2D rb;
    PlayerInput playerInput;
    PlayerCombatController combat;
    PlayerComboController comboController;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction dashAction;

    float coyoteCounter;
    float jumpBufferCounter;
    float wallJumpLockCounter;
    float dashCooldownCounter;
    float dashTimer;
    int facingDirection = 1;
    bool isDashing;

    public int FacingDirection => facingDirection;
    public float HorizontalSpeed => rb != null ? rb.linearVelocity.x : 0f;
    public float VerticalVelocity => rb != null ? rb.linearVelocity.y : 0f;
    public bool IsGrounded => IsGroundedCheck();
    public bool IsDashing => isDashing;
    public bool IsGrappling => grapple != null && grapple.IsActive;
    public bool IsGrappleSelecting => grapple != null && grapple.IsSelecting;
    public bool IsComboActive => comboController != null && comboController.IsComboActive;

    bool jumpHeld;
    bool wasGrounded;

    Vector2 dashDirection = Vector2.right;
    GrapplingHookController grapple;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        playerInput = GetComponent<PlayerInput>();
        combat = GetComponent<PlayerCombatController>();
        comboController = GetComponent<PlayerComboController>();
        grapple = GetComponent<GrapplingHookController>();

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        dashAction = playerInput.actions["Sprint"];

        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        if (visual == null)
        {
            var visualTransform = transform.Find("Visual");
            if (visualTransform != null)
                visual = visualTransform;
        }

        if (spriteRenderer == null && visual != null)
            spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && spriteRenderer.sprite == null && GetComponentInChildren<PlayerGearbotVisual>() == null)
            spriteRenderer.sprite = CreatePlaceholderSprite();
    }

    void Update()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen || isDashing || IsCombatLocked() || IsComboActive)
            return;

        Vector2 moveInput = moveAction.ReadValue<Vector2>();

        if (IsGrappling)
        {
            if (Mathf.Abs(moveInput.x) > 0.05f)
                facingDirection = moveInput.x > 0f ? 1 : -1;
            UpdateVisualFacing();
            return;
        }

        bool grounded = IsGroundedCheck();
        bool touchingWall = IsTouchingWall(out int wallDirection);
        float horizontalInput = moveInput.x;

        if (Mathf.Abs(horizontalInput) > 0.05f)
            facingDirection = horizontalInput > 0f ? 1 : -1;

        if (grounded)
            coyoteCounter = coyoteTime;
        else
            coyoteCounter -= Time.deltaTime;

        if (jumpAction.WasPressedThisFrame())
            jumpBufferCounter = jumpBufferTime;
        else
            jumpBufferCounter -= Time.deltaTime;

        jumpHeld = jumpAction.IsPressed();

        if (dashAction.WasPressedThisFrame() && dashCooldownCounter <= 0f)
            StartDash(new Vector2(facingDirection, 0f));

        if (jumpBufferCounter > 0f)
        {
            if (coyoteCounter > 0f)
            {
                Jump();
                jumpBufferCounter = 0f;
            }
            else if (touchingWall && !grounded)
            {
                WallJump(wallDirection);
                jumpBufferCounter = 0f;
            }
        }

        if (jumpAction.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * jumpCutMultiplier);

        if (touchingWall && !grounded && rb.linearVelocity.y < 0f && horizontalInput * wallDirection > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, Mathf.Max(rb.linearVelocity.y, -wallSlideSpeed));
        }

        UpdateVisualFacing();
        wasGrounded = grounded;
    }

    void FixedUpdate()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen || IsCombatLocked() || IsGrappling || IsComboActive)
            return;

        float fixedDt = Time.fixedDeltaTime;

        if (isDashing)
        {
            dashTimer -= fixedDt;
            rb.linearVelocity = dashDirection * dashSpeed;

            if (dashTimer <= 0f)
                EndDash();

            return;
        }

        if (wallJumpLockCounter > 0f)
        {
            wallJumpLockCounter -= fixedDt;
            return;
        }

        Vector2 moveInput = moveAction.ReadValue<Vector2>();
        float horizontalInput = moveInput.x;
        bool grounded = IsGroundedCheck();
        float targetSpeed = horizontalInput * moveSpeed;
        float speedDifference = targetSpeed - rb.linearVelocity.x;
        float accelerationRate = grounded
            ? (Mathf.Abs(targetSpeed) > 0.01f ? groundAcceleration : groundDeceleration)
            : airAcceleration;
        float velocityChange = speedDifference * accelerationRate * fixedDt;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x + velocityChange, rb.linearVelocity.y);

        if (rb.linearVelocity.y < 0f)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallGravityMultiplier - 1f) * fixedDt;
        else if (rb.linearVelocity.y > 0f && !jumpHeld)
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpGravityMultiplier - 1f) * fixedDt;
    }

    void Jump()
    {
        coyoteCounter = 0f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
    }

    void WallJump(int wallDirection)
    {
        wallJumpLockCounter = wallJumpControlLock;
        coyoteCounter = 0f;
        facingDirection = -wallDirection;
        rb.linearVelocity = new Vector2(wallJumpForce.x * -wallDirection, wallJumpForce.y);
    }

    void StartDash(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f)
            direction = Vector2.right * facingDirection;

        dashDirection = direction.normalized;
        isDashing = true;
        dashTimer = dashDuration;
        dashCooldownCounter = dashCooldown;
        rb.gravityScale = 0f;
    }

    void EndDash()
    {
        isDashing = false;
        rb.gravityScale = 3f;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.35f, rb.linearVelocity.y * 0.35f);
    }

    bool IsCombatLocked()
    {
        return combat != null && (combat.IsAttacking || combat.IsComboLocked);
    }

    public bool IsGrappleBlocked()
    {
        return isDashing || IsCombatLocked();
    }

    void UpdateVisualFacing()
    {
        if (visual == null)
            return;

        Vector3 scale = visual.localScale;
        scale.x = Mathf.Abs(scale.x);
        scale.y = Mathf.Abs(scale.y);
        visual.localScale = scale;

        var gearbotVisual = visual.GetComponent<PlayerGearbotVisual>();
        if (gearbotVisual == null)
        {
            var spriteRenderer = visual.GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.flipX = facingDirection < 0;
        }
    }

    bool IsGroundedCheck()
    {
        if (groundCheck == null)
            return false;

        return Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayers);
    }

    bool IsTouchingWall(out int wallDirection)
    {
        wallDirection = facingDirection;

        if (wallCheck == null)
            return false;

        Vector2 direction = Vector2.right * facingDirection;
        RaycastHit2D hit = Physics2D.Raycast(wallCheck.position, direction, wallCheckDistance, groundLayers);
        return hit.collider != null;
    }

    static Sprite CreatePlaceholderSprite()
    {
        const int width = 16;
        const int height = 24;
        var texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point;

        Color body = new Color(0.72f, 0.78f, 0.86f);
        Color cloak = new Color(0.18f, 0.2f, 0.28f);
        Color horn = new Color(0.9f, 0.92f, 0.96f);

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool isBody = x >= 4 && x <= 11 && y >= 4 && y <= 17;
                bool isCloak = x >= 2 && x <= 13 && y >= 0 && y <= 8;
                bool isHead = x >= 5 && x <= 10 && y >= 16 && y <= 22;
                bool isHorn = (x == 5 || x == 10) && y >= 20;

                if (isHorn)
                    texture.SetPixel(x, y, horn);
                else if (isHead)
                    texture.SetPixel(x, y, body);
                else if (isCloak)
                    texture.SetPixel(x, y, cloak);
                else if (isBody)
                    texture.SetPixel(x, y, body);
                else
                    texture.SetPixel(x, y, Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.15f), 16f);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(groundCheck.position, groundCheckSize);
        }

        if (wallCheck != null)
        {
            Gizmos.color = Color.cyan;
            Vector3 end = wallCheck.position + Vector3.right * wallCheckDistance * facingDirection;
            Gizmos.DrawLine(wallCheck.position, end);
        }
    }
}
