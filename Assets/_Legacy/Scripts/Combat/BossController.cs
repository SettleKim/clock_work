using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(BoxCollider2D))]
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Health))]
public class BossController : MonoBehaviour
{
    enum BossState { Wander, Skill1, Skill2, Skill3, Stunned }

    [Header("Stats")]
    [SerializeField] float maxHealth = 20f;
    [SerializeField] float skillInterval = 3f;
    [SerializeField] float contactDamage = 1f;
    [SerializeField] float contactCooldown = 0.45f;
    [SerializeField] float dangerWarningDuration = 1f;

    [Header("Wander")]
    [SerializeField] float wanderDistanceFromPlayer = 3f;
    [SerializeField] float wanderSpeed = 1.6f;
    [SerializeField] float wanderChangeInterval = 1.2f;

    [Header("Skill 1")]
    [SerializeField] float skill1Distance = 5f;
    [SerializeField] float skill1Duration = 0.35f;
    [SerializeField] float skill1ParryStunDuration = 1f;

    [Header("Skill 2")]
    [SerializeField] float skill2HoverHeight = 5f;
    [SerializeField] float skill2HoverDuration = 1f;
    [SerializeField] float skill2JumpSpeed = 10f;
    [SerializeField] float skill2SlamSpeed = 14f;
    [SerializeField] float skill2HoverFollowSpeed = 5f;
    [SerializeField] float groundY = 1.8f;
    [SerializeField] float warningSurfaceOffsetFromGround = -1.3f;

    [Header("Skill 3")]
    [SerializeField] float skill3RetreatDistance = 15f;
    [SerializeField] float[] skill3PlayerXOffsets = { -3f, 0f, 3f };
    [SerializeField] float skill3MoveSpeed = 4f;

    Rigidbody2D rb;
    SpriteRenderer spriteRenderer;
    BoxCollider2D bodyCollider;
    Health health;
    WorldHealthBar healthBar;
    Transform player;
    Collider2D playerCollider;

    BossState state = BossState.Wander;
    int facing = 1;
    float skillTimer;
    float wanderTimer;
    int wanderDirection = 1;
    float lastContactDamageTime;
    bool contactDamageActive;
    Coroutine activeSkillRoutine;
    Vector3 spawnPosition;
    float defaultGravityScale;
    RigidbodyType2D defaultBodyType;

    public bool ContactDamageActive => contactDamageActive;

    public void OnChargeParried()
    {
        if (state != BossState.Skill1 || activeSkillRoutine == null)
            return;

        SetContactDamage(false);
        StopCoroutine(activeSkillRoutine);
        activeSkillRoutine = StartCoroutine(Skill1ParriedRoutine());
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        bodyCollider = GetComponent<BoxCollider2D>();
        health = GetComponent<Health>();
        spawnPosition = transform.position;

        gameObject.tag = "Enemy";
        rb.gravityScale = 3f;
        defaultGravityScale = rb.gravityScale;
        defaultBodyType = rb.bodyType;
        rb.freezeRotation = true;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        bodyCollider.size = new Vector2(1.4f, 2f);

        spriteRenderer.sprite = CombatSpriteUtil.CreateRectSprite(18, 26, new Color(0.55f, 0.18f, 0.22f));
        spriteRenderer.sortingOrder = 4;

        health.DestroyOnDeath = false;
        health.Configure(maxHealth, false);
        health.Died += OnBossDied;

        healthBar = gameObject.AddComponent<WorldHealthBar>();
        healthBar.Bind(health);
    }

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
            playerCollider = playerObject.GetComponent<Collider2D>();
            if (playerCollider != null)
                Physics2D.IgnoreCollision(bodyCollider, playerCollider, true);
        }
    }

    void OnDestroy()
    {
        if (health != null)
            health.Died -= OnBossDied;
    }

    void Update()
    {
        if (health.IsDead || player == null)
            return;

        if (state == BossState.Wander)
        {
            skillTimer += WorldSlowMotion.WorldDeltaTime;
            if (skillTimer >= skillInterval)
            {
                skillTimer = 0f;
                StartRandomSkill();
            }
        }

        UpdateFacing();
    }

    void FixedUpdate()
    {
        if (health.IsDead || player == null)
            return;

        if (contactDamageActive && IsOverlappingPlayer())
            TryContactDamagePlayer();

        if (state != BossState.Wander)
            return;

        TickWander();
    }

    void UpdateFacing()
    {
        float deltaX = player.position.x - transform.position.x;
        if (Mathf.Abs(deltaX) > 0.05f)
            facing = deltaX > 0f ? 1 : -1;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * facing;
        transform.localScale = scale;
    }

    void TickWander()
    {
        wanderTimer -= WorldSlowMotion.WorldFixedDeltaTime;
        if (wanderTimer <= 0f)
        {
            wanderTimer = wanderChangeInterval;
            wanderDirection = Random.value > 0.5f ? 1 : -1;
        }

        float slow = WorldSlowMotion.SlowScale;
        float desiredX = player.position.x + wanderDirection * wanderDistanceFromPlayer;
        float moveX = Mathf.Sign(desiredX - transform.position.x) * wanderSpeed * slow;
        if (Mathf.Abs(desiredX - transform.position.x) < 0.2f)
            moveX = wanderDirection * wanderSpeed * slow * 0.35f;

        rb.linearVelocity = new Vector2(moveX, rb.linearVelocity.y);
    }

    void StartRandomSkill()
    {
        if (activeSkillRoutine != null)
            return;

        int skillIndex = Random.Range(0, 3);
        activeSkillRoutine = skillIndex switch
        {
            0 => StartCoroutine(Skill1Routine()),
            1 => StartCoroutine(Skill2Routine()),
            _ => StartCoroutine(Skill3Routine()),
        };
    }

    IEnumerator Skill1Routine()
    {
        state = BossState.Skill1;
        BeginSkillMotion();

        Vector2 start = transform.position;
        Vector2 direction = new Vector2(facing, 0f);
        Vector2 end = start + direction * skill1Distance;
        Vector2 zoneCenter = (start + end) * 0.5f;
        Vector2 zoneSize = new Vector2(skill1Distance + bodyCollider.size.x, 1.1f);

        yield return BossDangerZone.ShowWarning(zoneCenter, zoneSize, dangerWarningDuration);

        SetContactDamage(true);
        float elapsed = 0f;
        while (elapsed < skill1Duration)
        {
            elapsed += WorldSlowMotion.WorldDeltaTime;
            transform.position = Vector2.Lerp(start, end, elapsed / skill1Duration);
            yield return null;
        }

        EndSkillMotion();
    }

    IEnumerator Skill1ParriedRoutine()
    {
        yield return StunRoutine(skill1ParryStunDuration);
        EndSkillMotion();
    }

    IEnumerator StunRoutine(float duration)
    {
        state = BossState.Stunned;
        rb.linearVelocity = Vector2.zero;

        Color originalColor = spriteRenderer.color;
        spriteRenderer.color = new Color(0.65f, 0.65f, 0.65f, 1f);

        float endTime = Time.unscaledTime + duration;
        while (Time.unscaledTime < endTime)
            yield return null;

        spriteRenderer.color = originalColor;
    }

    IEnumerator Skill2Routine()
    {
        state = BossState.Skill2;
        BeginSkillMotion();

        float peakY = transform.position.y + skill2HoverHeight;
        yield return MoveTransformTo(new Vector2(transform.position.x, peakY), skill2JumpSpeed);

        float hoverElapsed = 0f;
        float warningElapsed = 0f;
        Vector2 warningSize = new Vector2(1.6f, 0.5f);
        BossDangerZone dangerZone = BossDangerZone.Create(GetGroundWarningCenter(player.position.x, warningSize), warningSize);

        while (hoverElapsed < skill2HoverDuration || warningElapsed < dangerWarningDuration)
        {
            float dt = WorldSlowMotion.WorldDeltaTime;

            if (hoverElapsed < skill2HoverDuration)
            {
                hoverElapsed += dt;
                Vector2 hoverTarget = new Vector2(player.position.x, player.position.y + skill2HoverHeight);
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    hoverTarget,
                    skill2HoverFollowSpeed * dt);
            }

            if (warningElapsed < dangerWarningDuration)
            {
                warningElapsed += dt;
                dangerZone.SetCenter(GetGroundWarningCenter(player.position.x, warningSize));
            }

            yield return null;
        }

        if (dangerZone != null)
            Destroy(dangerZone.gameObject);

        SetContactDamage(true);
        Vector2 finalSlam = new Vector2(player.position.x, GetGroundY());
        yield return MoveTransformTo(finalSlam, skill2SlamSpeed);
        EndSkillMotion();
    }

    IEnumerator Skill3Routine()
    {
        state = BossState.Skill3;
        BeginSkillMotion();

        Vector2 playerPos = player.position;
        Vector2 away = ((Vector2)transform.position - playerPos).normalized;
        if (away.sqrMagnitude < 0.01f)
            away = new Vector2(-facing, 0f);

        Vector2 retreatPos = playerPos + away * skill3RetreatDistance;
        retreatPos.y = GetGroundY();
        yield return MoveTransformTo(retreatPos, skill3MoveSpeed);

        playerPos = player.position;
        Vector2 fireOrigin = transform.position;

        var landingPoints = new Vector2[skill3PlayerXOffsets.Length];
        Vector2 warningSize = new Vector2(1.4f, 0.45f);
        for (int i = 0; i < skill3PlayerXOffsets.Length; i++)
        {
            float landingX = playerPos.x + skill3PlayerXOffsets[i];
            landingPoints[i] = new Vector2(landingX, GetGroundY());
        }

        for (int i = 0; i < landingPoints.Length; i++)
        {
            Vector2 warningCenter = GetGroundWarningCenter(landingPoints[i].x, warningSize);
            StartCoroutine(BossDangerZone.ShowWarning(warningCenter, warningSize, dangerWarningDuration));
            BossArcProjectile.Spawn(fireOrigin, landingPoints[i], contactDamage, dangerWarningDuration);
            yield return WorldSlowMotion.WaitWorldSeconds(0.15f);
        }

        yield return WorldSlowMotion.WaitWorldSeconds(dangerWarningDuration + 0.15f);
        EndSkillMotion();
    }

    void BeginSkillMotion()
    {
        rb.linearVelocity = Vector2.zero;
        rb.gravityScale = 0f;
        rb.bodyType = RigidbodyType2D.Kinematic;
    }

    void EndSkillMotion()
    {
        SetContactDamage(false);
        rb.bodyType = defaultBodyType;
        rb.gravityScale = defaultGravityScale;
        rb.linearVelocity = Vector2.zero;
        state = BossState.Wander;
        activeSkillRoutine = null;
    }

    IEnumerator MoveTransformTo(Vector2 target, float speed)
    {
        while (Vector2.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector2.MoveTowards(transform.position, target, speed * WorldSlowMotion.WorldDeltaTime);
            yield return null;
        }

        transform.position = target;
    }

    void SetContactDamage(bool active)
    {
        contactDamageActive = active;
    }

    bool IsOverlappingPlayer()
    {
        if (playerCollider == null)
            return false;

        return bodyCollider.bounds.Intersects(playerCollider.bounds);
    }

    void TryContactDamagePlayer()
    {
        if (!contactDamageActive || player == null)
            return;

        if (Time.time - lastContactDamageTime < contactCooldown)
            return;

        Health playerHealth = player.GetComponent<Health>();
        if (playerHealth == null || playerHealth.IsDead)
            return;

        lastContactDamageTime = Time.time;
        playerHealth.TakeDamage(contactDamage, gameObject);
    }

    float GetGroundY() => groundY;

    float GetWarningSurfaceY() => groundY + warningSurfaceOffsetFromGround;

    Vector2 GetGroundWarningCenter(float worldX, Vector2 warningSize)
    {
        return new Vector2(worldX, GetWarningSurfaceY() + warningSize.y * 0.5f);
    }

    void OnBossDied()
    {
        rb.linearVelocity = Vector2.zero;
        SetContactDamage(false);
        if (activeSkillRoutine != null)
        {
            StopCoroutine(activeSkillRoutine);
            activeSkillRoutine = null;
        }

        StartCoroutine(BossRespawnRoutine());
    }

    IEnumerator BossRespawnRoutine()
    {
        spriteRenderer.enabled = false;
        bodyCollider.enabled = false;
        healthBar.SetVisible(false);
        yield return new WaitForSeconds(5f);
        transform.position = spawnPosition;
        health.ResetHealth();
        healthBar.ResetTotalDamage();
        spriteRenderer.enabled = true;
        bodyCollider.enabled = true;
        healthBar.SetVisible(true);
        state = BossState.Wander;
        skillTimer = 0f;
        rb.bodyType = defaultBodyType;
        rb.gravityScale = defaultGravityScale;
    }
}
