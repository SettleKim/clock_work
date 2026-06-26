using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerGearbotVisual : MonoBehaviour
{
    [Header("Motion Timing")]
    [SerializeField] float walkFrameDuration = 0.09f;
    [SerializeField] float dashFrameDuration = 0.07f;
    [SerializeField] float guardFrameDuration = 0.08f;
    [SerializeField] float attackFrameDuration = 0.08f;
    [SerializeField] float comboFrameDuration = 0.1f;
    [SerializeField] float landDuration = 0.14f;
    [SerializeField] float hitDuration = 0.22f;
    [SerializeField] float attackDuration = 0.16f;
    [SerializeField] float magicDuration = 0.22f;
    [SerializeField] float chargeDuration = 0.18f;
    [SerializeField] float guardHoldDuration = 0.45f;
    [SerializeField] float guardReleaseDelay = 0.12f;
    [SerializeField] float runSpeedThreshold = 5.5f;
    [SerializeField] Vector3 visualOffset = new(0f, -0.22f, 0f);

    SpriteRenderer spriteRenderer;
    MetroidvaniaPlayerController movement;
    PlayerCombatController combat;
    PlayerParryController parry;
    Health health;

    Sprite[] walkFrames = System.Array.Empty<Sprite>();
    Sprite[] dashFrames = System.Array.Empty<Sprite>();
    Sprite[] guardFrames = System.Array.Empty<Sprite>();
    Sprite[] hitFrames = System.Array.Empty<Sprite>();
    Sprite[] bladeAttackFrames = System.Array.Empty<Sprite>();
    Sprite[] hammerAttackFrames = System.Array.Empty<Sprite>();
    Sprite[] comboHandHammerFrames = System.Array.Empty<Sprite>();
    Sprite[] comboHammerWarpFrames = System.Array.Empty<Sprite>();
    Sprite[] activeAttackFrames = System.Array.Empty<Sprite>();
    float activeAttackFrameDuration;
    Sprite fallbackSprite;

    GearbotPose currentPose = GearbotPose.Idle;
    bool attackVisualActive;
    float actionTimer;
    float frameTimer;
    int frameIndex;
    bool wasGrounded = true;
    bool guardVisualActive;

    enum GearbotPose
    {
        Idle,
        Walk,
        Dash,
        Jump,
        Fall,
        Land,
        Guard,
        Melee,
        Charge,
        Magic,
        Hit,
        Dead
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.sortingOrder = 15;
        movement = GetComponentInParent<MetroidvaniaPlayerController>();
        combat = GetComponentInParent<PlayerCombatController>();
        parry = GetComponentInParent<PlayerParryController>();
        health = GetComponentInParent<Health>();

        LoadMotionFrames();

        if (walkFrames.Length == 0)
        {
            Debug.LogWarning("PlayerGearbotVisual: motion frames missing. Using placeholder.");
            fallbackSprite = CreateFallbackSprite();
            walkFrames = new[] { fallbackSprite };
        }

        ApplyStaticPose(GearbotPose.Idle, GetIdleSprite());
        transform.localPosition = visualOffset;
    }

    void OnEnable()
    {
        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    void OnDisable()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    void LateUpdate()
    {
        if (movement == null || spriteRenderer == null || currentPose == GearbotPose.Dead)
            return;

        if (guardVisualActive)
        {
            actionTimer -= Time.deltaTime;
            bool parryWindowOpen = parry != null && parry.IsParryWindowOpen;
            if (parryWindowOpen)
                actionTimer = Mathf.Max(actionTimer, 0.08f);

            if (actionTimer <= 0f && !parryWindowOpen)
                EndGuardVisual();
        }
        else if (attackVisualActive)
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
                EndAttackVisual();
        }
        else if (actionTimer > 0f)
        {
            actionTimer -= Time.deltaTime;
            if (actionTimer <= 0f)
                currentPose = GearbotPose.Idle;
        }

        GearbotPose targetPose = DeterminePose();
        if (targetPose != currentPose)
        {
            frameIndex = 0;
            frameTimer = 0f;
        }

        currentPose = targetPose;
        SyncFacing();
        AdvanceFrameAnimation(targetPose);
        transform.localPosition = visualOffset;
    }

    void SyncFacing()
    {
        if (movement == null || spriteRenderer == null)
            return;

        spriteRenderer.flipX = movement.FacingDirection < 0;
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    GearbotPose DeterminePose()
    {
        if (health != null && health.IsDead)
            return GearbotPose.Dead;

        if (guardVisualActive)
            return GearbotPose.Guard;

        if (attackVisualActive || (actionTimer > 0f && currentPose == GearbotPose.Melee))
            return GearbotPose.Melee;

        if (actionTimer > 0f && currentPose is GearbotPose.Hit or GearbotPose.Land or GearbotPose.Magic)
            return currentPose;

        if (combat != null)
        {
            if (combat.IsCharging)
                return GearbotPose.Charge;
        }

        if (movement.IsDashing && dashFrames.Length > 0)
            return GearbotPose.Dash;

        if (movement.IsGrappling || !movement.IsGrounded)
            return movement.VerticalVelocity > 0.5f ? GearbotPose.Jump : GearbotPose.Fall;

        if (!wasGrounded && movement.IsGrounded)
        {
            TriggerTimedPose(GearbotPose.Land, landDuration);
            return GearbotPose.Land;
        }

        if (Mathf.Abs(movement.HorizontalSpeed) > 0.2f)
            return GearbotPose.Walk;

        return GearbotPose.Idle;
    }

    void AdvanceFrameAnimation(GearbotPose pose)
    {
        wasGrounded = movement.IsGrounded;
        Sprite idle = GetIdleSprite();

        switch (pose)
        {
            case GearbotPose.Walk:
            {
                float duration = Mathf.Abs(movement.HorizontalSpeed) > runSpeedThreshold
                    ? walkFrameDuration * 0.72f
                    : walkFrameDuration;
                TickFrames(walkFrames, duration, true, idle);
                break;
            }
            case GearbotPose.Dash:
                TickFrames(dashFrames, dashFrameDuration, true, idle);
                break;
            case GearbotPose.Guard:
                TickFrames(guardFrames, guardFrameDuration, false, idle);
                break;
            case GearbotPose.Melee:
                TickFrames(activeAttackFrames, activeAttackFrameDuration, false, idle);
                break;
            case GearbotPose.Hit:
                spriteRenderer.sprite = hitFrames.Length > 0 ? hitFrames[0] : idle;
                break;
            case GearbotPose.Idle:
            case GearbotPose.Land:
            case GearbotPose.Jump:
            case GearbotPose.Fall:
            case GearbotPose.Charge:
            case GearbotPose.Magic:
                spriteRenderer.sprite = idle;
                break;
            case GearbotPose.Dead:
                spriteRenderer.sprite = idle;
                spriteRenderer.color = new Color(0.55f, 0.55f, 0.55f, 1f);
                break;
        }
    }

    void TickFrames(Sprite[] frames, float frameDuration, bool loop, Sprite fallback)
    {
        if (frames == null || frames.Length == 0)
        {
            spriteRenderer.sprite = fallback;
            return;
        }

        frameTimer += Time.deltaTime;
        while (frameTimer >= frameDuration)
        {
            frameTimer -= frameDuration;
            frameIndex++;
            if (frameIndex >= frames.Length)
                frameIndex = loop ? 0 : frames.Length - 1;
        }

        Sprite frame = frames[Mathf.Clamp(frameIndex, 0, frames.Length - 1)];
        spriteRenderer.sprite = frame != null ? frame : fallback;
    }

    void ApplyStaticPose(GearbotPose pose, Sprite sprite)
    {
        currentPose = pose;
        spriteRenderer.color = Color.white;
        if (sprite != null)
            spriteRenderer.sprite = sprite;
    }

    void TriggerTimedPose(GearbotPose pose, float duration)
    {
        currentPose = pose;
        actionTimer = duration;
        frameIndex = 0;
        frameTimer = 0f;
    }

    Sprite GetIdleSprite()
    {
        return walkFrames.Length > 0 ? walkFrames[0] : fallbackSprite;
    }

    public void PlayGuard()
    {
        if (guardFrames.Length == 0)
            return;

        guardVisualActive = true;
        actionTimer = guardHoldDuration;
        currentPose = GearbotPose.Guard;
        frameIndex = 0;
        frameTimer = 0f;
    }

    public void OnGuardSucceeded()
    {
        if (!guardVisualActive)
            return;

        actionTimer = guardReleaseDelay;
    }

    public void EndGuardVisual()
    {
        guardVisualActive = false;
        actionTimer = 0f;
        frameIndex = 0;
        frameTimer = 0f;

        if (currentPose == GearbotPose.Guard)
            currentPose = GearbotPose.Idle;
    }

    public void PlayAttack(GearbotAttackClip clip, float duration)
    {
        Sprite[] frames = GetAttackFrames(clip);
        if (frames.Length == 0)
        {
            TriggerTimedPose(GearbotPose.Melee, duration);
            return;
        }

        attackVisualActive = true;
        activeAttackFrames = frames;
        activeAttackFrameDuration = Mathf.Max(duration / frames.Length, attackFrameDuration);
        actionTimer = activeAttackFrameDuration * frames.Length;
        currentPose = GearbotPose.Melee;
        frameIndex = 0;
        frameTimer = 0f;
    }

    public void PlayAttack(WeaponType weapon, float duration)
    {
        GearbotAttackClip clip = weapon == WeaponType.Hammer
            ? GearbotAttackClip.Hammer
            : GearbotAttackClip.HandBlade;
        PlayAttack(clip, duration);
    }

    public void EndAttackVisual()
    {
        attackVisualActive = false;
        activeAttackFrames = System.Array.Empty<Sprite>();
        actionTimer = 0f;
        frameIndex = 0;
        frameTimer = 0f;

        if (currentPose == GearbotPose.Melee)
            currentPose = GearbotPose.Idle;
    }

    public void PlayMelee() { }

    public void PlayCharge()
    {
        currentPose = GearbotPose.Charge;
        actionTimer = chargeDuration;
    }

    public void PlayMagic() => TriggerTimedPose(GearbotPose.Magic, magicDuration);

    void OnDamaged(float _)
    {
        if (health == null || health.IsDead)
            return;

        EndGuardVisual();
        EndAttackVisual();
        TriggerTimedPose(GearbotPose.Hit, hitDuration);
    }

    void OnDied()
    {
        EndGuardVisual();
        EndAttackVisual();
        ApplyStaticPose(GearbotPose.Dead, GetIdleSprite());
    }

    void LoadMotionFrames()
    {
        walkFrames = FilterEmpty(GearbotMotionLoader.LoadWalk());
        dashFrames = FilterEmpty(GearbotMotionLoader.LoadDash());
        guardFrames = FilterEmpty(GearbotMotionLoader.LoadGuard());
        hitFrames = FilterEmpty(GearbotMotionLoader.LoadHit());
        bladeAttackFrames = FilterEmpty(GearbotAttackLoader.LoadHandBlade());
        hammerAttackFrames = FilterEmpty(GearbotAttackLoader.LoadHammer());
        comboHandHammerFrames = FilterEmpty(GearbotAttackLoader.LoadComboHandHammer());
        comboHammerWarpFrames = FilterEmpty(GearbotAttackLoader.LoadComboHammerWarp());

        Debug.Log(
            $"GearbotMotion loaded walk={walkFrames.Length} dash={dashFrames.Length} guard={guardFrames.Length} hit={hitFrames.Length} " +
            $"attack blade={bladeAttackFrames.Length} hammer={hammerAttackFrames.Length} combo={comboHandHammerFrames.Length}/{comboHammerWarpFrames.Length}");
    }

    Sprite[] GetAttackFrames(GearbotAttackClip clip)
    {
        return clip switch
        {
            GearbotAttackClip.HandBlade => bladeAttackFrames,
            GearbotAttackClip.Hammer => hammerAttackFrames,
            GearbotAttackClip.ComboHandHammer => comboHandHammerFrames,
            GearbotAttackClip.ComboHammerWarp => comboHammerWarpFrames,
            _ => System.Array.Empty<Sprite>(),
        };
    }

    static Sprite[] FilterEmpty(Sprite[] source)
    {
        if (source == null || source.Length == 0)
            return System.Array.Empty<Sprite>();

        int count = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null && source[i].rect.width > 8f && source[i].rect.height > 8f)
                count++;
        }

        if (count == 0)
            return System.Array.Empty<Sprite>();

        var filtered = new Sprite[count];
        int index = 0;
        for (int i = 0; i < source.Length; i++)
        {
            if (source[i] != null && source[i].rect.width > 8f && source[i].rect.height > 8f)
                filtered[index++] = source[i];
        }

        return filtered;
    }

    static Sprite CreateFallbackSprite()
    {
        const int size = 16;
        var texture = new Texture2D(size, size);
        texture.filterMode = FilterMode.Point;

        Color body = new Color(0.78f, 0.55f, 0.28f);
        Color core = new Color(0.35f, 0.85f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool shell = x >= 3 && x <= 12 && y >= 2 && y <= 13;
                bool heart = x >= 6 && x <= 9 && y >= 5 && y <= 8;
                texture.SetPixel(x, y, heart ? core : shell ? body : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.08f), 16f);
    }
}
