using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PlayerWeaponInventory))]
[RequireComponent(typeof(MetroidvaniaPlayerController))]
[RequireComponent(typeof(PlayerCombatController))]
public class PlayerComboController : MonoBehaviour
{
    const float SelectionDuration = 3f;

    readonly List<WeaponType> comboInputs = new();

    PlayerWeaponInventory weaponInventory;
    MetroidvaniaPlayerController movement;
    PlayerCombatController combat;
    PlayerGearbotVisual gearbotVisual;
    Rigidbody2D rb;
    Coroutine comboRoutine;
    float selectionEndUnscaledTime;
    int lastInputFrame = -1;

    public bool IsSelecting { get; private set; }
    public bool IsComboActive { get; private set; }
    public bool IsInvulnerable => IsComboActive;

    void Awake()
    {
        weaponInventory = GetComponent<PlayerWeaponInventory>();
        movement = GetComponent<MetroidvaniaPlayerController>();
        combat = GetComponent<PlayerCombatController>();
        gearbotVisual = GetComponentInChildren<PlayerGearbotVisual>();
        rb = GetComponent<Rigidbody2D>();
    }

    public void BeginComboSelection()
    {
        if (IsComboActive)
            return;

        comboInputs.Clear();
        lastInputFrame = -1;
        selectionEndUnscaledTime = Time.unscaledTime + SelectionDuration;
        IsSelecting = true;
    }

    public bool TryAddComboInput(WeaponType weapon)
    {
        if (!IsSelecting || IsComboActive || Time.unscaledTime > selectionEndUnscaledTime)
        {
            if (Time.unscaledTime > selectionEndUnscaledTime)
                IsSelecting = false;
            return false;
        }

        if (Time.frameCount == lastInputFrame || !weaponInventory.Owns(weapon))
            return false;

        lastInputFrame = Time.frameCount;
        comboInputs.Add(weapon);

        if (comboInputs.Count < 2)
            return true;

        IsSelecting = false;
        StartComboFromInputs();
        return true;
    }

    void StartComboFromInputs()
    {
        if (comboRoutine != null)
            StopCoroutine(comboRoutine);

        if (comboInputs.Count < 2)
            return;

        WeaponType first = comboInputs[0];
        WeaponType second = comboInputs[1];
        comboInputs.Clear();

        comboRoutine = first switch
        {
            WeaponType.HandBlade when second == WeaponType.Hammer => StartCoroutine(ComboHandThenHammer()),
            WeaponType.Hammer when second == WeaponType.HandBlade => StartCoroutine(ComboHammerThenHand()),
            _ => null,
        };

        if (comboRoutine == null)
            IsSelecting = false;
    }

    IEnumerator ComboHandThenHammer()
    {
        IsComboActive = true;
        weaponInventory.Equip(WeaponType.HandBlade);
        gearbotVisual?.PlayAttack(GearbotAttackClip.ComboHandHammer, 0.45f);

        for (int i = 0; i < 3; i++)
            yield return combat.StartCoroutine(combat.PerformComboStrike(WeaponStats.HandBlade, 1f, 0.1f, playVisual: false));

        yield return combat.StartCoroutine(combat.PerformComboStrike(WeaponStats.Hammer, 3f, 0.15f, playVisual: false));
        weaponInventory.Equip(WeaponType.Hammer);
        EndCombo();
    }

    IEnumerator ComboHammerThenHand()
    {
        IsComboActive = true;
        weaponInventory.Equip(WeaponType.Hammer);
        gearbotVisual?.PlayAttack(GearbotAttackClip.ComboHammerWarp, 1.1f);

        int facing = movement.FacingDirection;
        Vector2 origin = (Vector2)transform.position + new Vector2(0.6f * facing, 0.15f);
        HammerThrowProjectile projectile = HammerThrowProjectile.Spawn(origin, facing, 2f);

        float waitElapsed = 0f;
        while (projectile != null && !projectile.HasResolved && waitElapsed < 1.2f)
        {
            waitElapsed += Time.deltaTime;
            yield return null;
        }

        Vector2 landing = projectile != null
            ? projectile.LandingPosition
            : (Vector2)transform.position + Vector2.right * facing * 3f;

        yield return MoveToPosition(landing, 18f);

        weaponInventory.Equip(WeaponType.HandBlade);
        yield return combat.StartCoroutine(
            combat.PerformComboStrike(WeaponStats.HandBlade, 1f, 0.2f, playVisual: false));

        weaponInventory.Equip(WeaponType.HandBlade);
        EndCombo();
    }

    IEnumerator MoveToPosition(Vector2 target, float speed)
    {
        while (Vector2.Distance(transform.position, target) > 0.15f)
        {
            transform.position = Vector2.MoveTowards(transform.position, target, speed * Time.deltaTime);
            if (rb != null)
                rb.linearVelocity = Vector2.zero;
            yield return null;
        }

        transform.position = target;
        if (rb != null)
            rb.linearVelocity = Vector2.zero;
    }

    void EndCombo()
    {
        IsComboActive = false;
        IsSelecting = false;
        comboRoutine = null;
    }

    void Update()
    {
        if (IsSelecting && !IsComboActive && Time.unscaledTime > selectionEndUnscaledTime)
            IsSelecting = false;
    }
}
