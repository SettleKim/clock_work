using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(MetroidvaniaPlayerController))]
[RequireComponent(typeof(Health))]
[RequireComponent(typeof(PlayerWeaponInventory))]
public class PlayerCombatController : MonoBehaviour
{
    static readonly Collider2D[] OverlapBuffer = new Collider2D[16];
    static readonly ContactFilter2D OverlapFilter = ContactFilter2D.noFilter;

    [Header("Charge Attack")]
    [SerializeField] float chargeTime = 0.45f;
    [SerializeField] float swordWaveDamage = 2f;
    [SerializeField] Transform swordWaveSpawnPoint;

    [Header("Magic")]
    [SerializeField] float laserDamage = 2f;
    [SerializeField] float laserCooldown = 0.7f;
    [SerializeField] Vector2 laserOriginOffset = new(0.35f, 0.15f);

    [Header("References")]
    [SerializeField] GameObject swordWavePrefab;
    [SerializeField] Transform meleeHitboxRoot;

    PlayerInput playerInput;
    MetroidvaniaPlayerController movement;
    Health health;
    PlayerGearbotVisual gearbotVisual;
    PlayerWeaponInventory weaponInventory;
    PlayerComboController comboController;
    InputAction attackAction;
    InputAction magicAction;
    LaserBeamAttack laserBeam;

    BoxCollider2D meleeCollider;
    DamageDealer meleeDamageDealer;
    SpriteRenderer meleeVisual;

    WeaponStats currentStats;
    float attackCooldownCounter;
    float magicCooldownCounter;
    float attackHoldTimer;
    bool attackHeld;
    bool chargeAttackFired;
    bool isAttacking;
    bool isCharging;

    public bool IsAttacking => isAttacking;
    public bool IsCharging => isCharging;
    public bool IsComboLocked => comboController != null && (comboController.IsComboActive || comboController.IsSelecting);

    void Awake()
    {
        playerInput = GetComponent<PlayerInput>();
        movement = GetComponent<MetroidvaniaPlayerController>();
        health = GetComponent<Health>();
        gearbotVisual = GetComponentInChildren<PlayerGearbotVisual>();
        weaponInventory = GetComponent<PlayerWeaponInventory>();
        comboController = GetComponent<PlayerComboController>();

        attackAction = playerInput.actions["Attack"];
        magicAction = playerInput.actions["MagicAttack"];

        laserBeam = gameObject.AddComponent<LaserBeamAttack>();
        laserBeam.Configure(laserDamage);

        health.DestroyOnDeath = false;
        if (GetComponent<PlayerRespawn>() == null)
            health.ResetHealth();
        EnsureMeleeHitbox();
        EnsureSwordWavePrefab();
        ApplyWeaponStats(weaponInventory.GetCurrentStats());
        weaponInventory.WeaponChanged += OnWeaponChanged;
    }

    void OnDestroy()
    {
        if (weaponInventory != null)
            weaponInventory.WeaponChanged -= OnWeaponChanged;
    }

    void OnWeaponChanged(WeaponType _)
    {
        ApplyWeaponStats(weaponInventory.GetCurrentStats());
    }

    void ApplyWeaponStats(WeaponStats stats)
    {
        currentStats = stats;
        meleeCollider.size = stats.hitboxSize;
        meleeDamageDealer.Configure(stats.damage, false);
        meleeVisual.color = stats.hitboxColor;
    }

    void Update()
    {
        if (PlayerMenuUI.IsMenuOpen || TradeUI.IsTradeOpen || IsComboLocked)
            return;

        attackCooldownCounter -= Time.deltaTime;
        magicCooldownCounter -= Time.deltaTime;

        HandleMeleeInput();
        HandleMagicInput();
    }

    void HandleMeleeInput()
    {
        if (attackAction.WasPressedThisFrame())
        {
            attackHeld = true;
            attackHoldTimer = 0f;
            chargeAttackFired = false;
            isCharging = currentStats.canCharge;
        }

        if (attackHeld && attackAction.IsPressed())
        {
            attackHoldTimer += Time.deltaTime;
            isCharging = currentStats.canCharge && !chargeAttackFired;

            if (currentStats.canCharge && !chargeAttackFired && attackHoldTimer >= chargeTime)
            {
                FireChargeAttack();
                chargeAttackFired = true;
                isCharging = false;
            }
        }

        if (attackAction.WasReleasedThisFrame() && attackHeld)
        {
            if (!chargeAttackFired && attackHoldTimer < chargeTime)
                PerformMeleeAttack();

            attackHeld = false;
            attackHoldTimer = 0f;
            isCharging = false;
        }
    }

    void HandleMagicInput()
    {
        if (magicAction.WasPressedThisFrame())
            FireLaser();
    }

    void PerformMeleeAttack()
    {
        if (attackCooldownCounter > 0f || isAttacking)
            return;

        StartCoroutine(MeleeRoutine());
    }

    IEnumerator MeleeRoutine()
    {
        isAttacking = true;
        attackCooldownCounter = currentStats.cooldown;
        gearbotVisual?.PlayAttack(weaponInventory.CurrentWeapon, currentStats.attackDuration);

        UpdateMeleeHitboxPosition();
        meleeDamageDealer.ResetHits();
        meleeCollider.enabled = true;
        meleeVisual.enabled = true;

        yield return new WaitForSeconds(currentStats.attackDuration);

        meleeCollider.enabled = false;
        meleeVisual.enabled = false;
        isAttacking = false;
    }

    public IEnumerator PerformComboStrike(WeaponStats stats, float damage, float duration)
    {
        return PerformComboStrike(stats, damage, duration, null, true);
    }

    public IEnumerator PerformComboStrike(WeaponStats stats, float damage, float duration, GearbotAttackClip? attackClip)
    {
        return PerformComboStrike(stats, damage, duration, attackClip, true);
    }

    public IEnumerator PerformComboStrike(WeaponStats stats, float damage, float duration, bool playVisual)
    {
        return PerformComboStrike(stats, damage, duration, null, playVisual);
    }

    public IEnumerator PerformComboStrike(WeaponStats stats, float damage, float duration, GearbotAttackClip? attackClip, bool playVisual)
    {
        isAttacking = true;
        currentStats = stats;
        meleeCollider.size = stats.hitboxSize;
        meleeVisual.color = stats.hitboxColor;

        UpdateMeleeHitboxPosition();
        meleeCollider.enabled = false;
        meleeDamageDealer.enabled = false;
        meleeVisual.enabled = true;

        if (playVisual)
        {
            if (attackClip.HasValue)
                gearbotVisual?.PlayAttack(attackClip.Value, duration);
            else if (stats.canCharge)
                gearbotVisual?.PlayAttack(WeaponType.HandBlade, duration);
            else
                gearbotVisual?.PlayAttack(WeaponType.Hammer, duration);
        }

        var struck = new HashSet<Health>();
        float elapsed = 0f;
        while (elapsed < duration)
        {
            UpdateMeleeHitboxPosition();
            ApplyComboHitDamage(stats.hitboxSize, damage, struck);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        meleeVisual.enabled = false;
        meleeDamageDealer.enabled = true;
        isAttacking = false;
        ApplyWeaponStats(weaponInventory.GetCurrentStats());
    }

    void ApplyComboHitDamage(Vector2 hitboxSize, float damage, HashSet<Health> struck)
    {
        Vector2 center = meleeHitboxRoot.position;
        float angle = meleeHitboxRoot.eulerAngles.z;
        int count = Physics2D.OverlapBox(center, hitboxSize, angle, OverlapFilter, OverlapBuffer);

        for (int i = 0; i < count; i++)
        {
            Collider2D hit = OverlapBuffer[i];
            if (!hit.CompareTag("Enemy"))
                continue;

            if (!hit.TryGetComponent(out Health targetHealth) || targetHealth.IsDead || !struck.Add(targetHealth))
                continue;

            targetHealth.TakeDamage(damage, gameObject);
        }
    }

    void FireChargeAttack()
    {
        if (attackCooldownCounter > 0f || !currentStats.canCharge)
            return;

        attackCooldownCounter = currentStats.cooldown;
        gearbotVisual?.PlayCharge();

        Vector3 spawnPosition = swordWaveSpawnPoint != null
            ? swordWaveSpawnPoint.position
            : transform.position + Vector3.right * movement.FacingDirection * 0.6f + Vector3.up * 0.05f;

        GameObject wave = Instantiate(swordWavePrefab, spawnPosition, Quaternion.identity);
        wave.transform.SetParent(null);
        wave.transform.position = spawnPosition;
        wave.SetActive(true);

        SwordWaveProjectile projectile = wave.GetComponent<SwordWaveProjectile>();
        projectile.Launch(Vector2.right * movement.FacingDirection, swordWaveDamage);
    }

    void FireLaser()
    {
        if (magicCooldownCounter > 0f)
            return;

        magicCooldownCounter = laserCooldown;
        gearbotVisual?.PlayMagic();

        Vector2 origin = (Vector2)transform.position + new Vector2(
            laserOriginOffset.x * movement.FacingDirection,
            laserOriginOffset.y);

        laserBeam.Fire(origin, Vector2.right * movement.FacingDirection);
    }

    void EnsureMeleeHitbox()
    {
        if (meleeHitboxRoot == null)
        {
            var hitboxObject = new GameObject("MeleeHitbox");
            hitboxObject.transform.SetParent(transform);
            meleeHitboxRoot = hitboxObject.transform;
        }

        meleeCollider = meleeHitboxRoot.GetComponent<BoxCollider2D>();
        if (meleeCollider == null)
            meleeCollider = meleeHitboxRoot.gameObject.AddComponent<BoxCollider2D>();

        meleeCollider.isTrigger = true;
        meleeCollider.enabled = false;

        meleeDamageDealer = meleeHitboxRoot.GetComponent<DamageDealer>();
        if (meleeDamageDealer == null)
            meleeDamageDealer = meleeHitboxRoot.gameObject.AddComponent<DamageDealer>();

        meleeVisual = meleeHitboxRoot.GetComponent<SpriteRenderer>();
        if (meleeVisual == null)
            meleeVisual = meleeHitboxRoot.gameObject.AddComponent<SpriteRenderer>();

        meleeVisual.sprite = CombatSpriteUtil.CreateRectSprite(12, 8, Color.white);
        meleeVisual.sortingOrder = 5;
        meleeVisual.enabled = false;
    }

    void EnsureSwordWavePrefab()
    {
        if (swordWavePrefab != null)
            return;

        swordWavePrefab = new GameObject("SwordWavePrefab");
        swordWavePrefab.SetActive(false);
        swordWavePrefab.AddComponent<Rigidbody2D>();
        swordWavePrefab.AddComponent<BoxCollider2D>();
        swordWavePrefab.AddComponent<SpriteRenderer>();
        swordWavePrefab.AddComponent<DamageDealer>();
        swordWavePrefab.AddComponent<SwordWaveProjectile>();
        swordWavePrefab.transform.position = new Vector3(0f, -100f, 0f);
    }

    void UpdateMeleeHitboxPosition()
    {
        meleeHitboxRoot.localPosition = new Vector3(
            currentStats.hitboxOffset.x * movement.FacingDirection,
            currentStats.hitboxOffset.y,
            0f);

        Vector3 visualScale = meleeHitboxRoot.localScale;
        visualScale.x = Mathf.Abs(visualScale.x) * movement.FacingDirection;
        meleeHitboxRoot.localScale = visualScale;
    }
}
