using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(Health))]
    public class PlayerFistCombat : MonoBehaviour
    {
        const string DefaultComboResourcePath = "Combos/FistCombo";
        const string DefaultPowerAttackResourcePath = "Combos/FistPowerAttack";

        static readonly int AttackFistHash = Animator.StringToHash("attack_fist");
        static readonly int AttackFistConHash = Animator.StringToHash("attack_fist_con");
        static readonly int WeaponHammerHash = Animator.StringToHash("WeaponHammer");
        static readonly int WeaponGreatswordHash = Animator.StringToHash("WeaponGreatsword");
        static readonly int WeaponDaggerHash = Animator.StringToHash("WeaponDagger");

        [Header("Combo Data")]
        [SerializeField] WeaponDefinition weaponDefinition;
        [SerializeField] ComboDefinition comboDefinition;
        [SerializeField] PowerAttackDefinition powerAttackDefinition;

        [Header("Fist")]
        [SerializeField] float damage = 1f;
        [SerializeField] float comboStepCooldown = 0.16f;
        [SerializeField] float comboInputBuffer = 0.35f;
        [SerializeField] float comboResetTime = 2.5f;
        [SerializeField] float finisherCooldown = 0.32f;
        [SerializeField] Color hitboxColor = new(1f, 0.92f, 0.55f, 0.45f);

        PlayerInput playerInput;
        PlayerMovement movement;
        GrapplingHookController grapple;
        PlayerCharacterVisual characterVisual;
        Animator visualAnimator;
        InputAction attackAction;

        Transform hitboxRoot;
        BoxCollider2D hitboxCollider;
        CapsuleCollider2D bodyCollider;
        Rigidbody2D body;
        DamageDealer hitboxDamage;
        SpriteRenderer hitboxVisual;

        PlayerCombatMode combatMode;
        PlayerBackWeaponVisual backWeaponVisual;

        PlayerBackWeaponVisual BackWeaponVisual
        {
            get
            {
                if (backWeaponVisual == null)
                    backWeaponVisual = GetComponentInChildren<PlayerBackWeaponVisual>();
                return backWeaponVisual;
            }
        }

        int comboIndex;
        float comboWindowTimer;
        float attackCooldownCounter;
        bool isAttacking;
        bool isPowerAttacking;
        bool isPlayingTransitionMove;
        bool suppressAnimHitboxEvents;
        bool attackBuffered;
        bool bufferWindowOpen;
        Coroutine attackCoroutine;

        public bool IsAttacking => isAttacking;
        public bool IsPowerAttacking => isPowerAttacking;
        public bool IsPlayingTransitionMove => isPlayingTransitionMove;
        public bool CanStartPowerAttack =>
            !isAttacking && powerAttackDefinition != null && powerAttackDefinition.PatternLength > 0;
        public ComboDefinition ComboDefinition => comboDefinition;
        public PowerAttackDefinition PowerAttackDefinition => powerAttackDefinition;
        public WeaponDefinition EquippedWeapon => weaponDefinition;
        public DamageDealer HitboxDamage => hitboxDamage;

        public void ConfigureWeapon(WeaponDefinition definition)
        {
            if (definition == null)
                return;

            weaponDefinition = definition;
            comboDefinition = definition.Combo;
            powerAttackDefinition = definition.PowerAttack;
            ResetComboState();
            visualAnimator?.SetBool(WeaponHammerHash, definition.WeaponId == "hammer");
            visualAnimator?.SetBool(WeaponGreatswordHash, definition.WeaponId == "greatsword");
            visualAnimator?.SetBool(WeaponDaggerHash, definition.WeaponId == "dagger");

            // 현재 장착한 무기를 등 슬롯에 표시(주먹은 아이콘이 없어 자동으로 숨겨짐).
            // 공격 모션 중엔 손에 무기가 그려지므로 SetAttackingState(true)가 등 표시를 가린다.
            BackWeaponVisual?.ShowWeapon(definition);
        }

        void SetAttackingState(bool attacking)
        {
            characterVisual?.SetAttacking(attacking);
            BackWeaponVisual?.SetHiddenForAttack(attacking);
        }

        public void ConfigureCombo(ComboDefinition definition)
        {
            if (definition != null)
                comboDefinition = definition;
        }

        public void ConfigurePowerAttack(PowerAttackDefinition definition)
        {
            if (definition != null)
                powerAttackDefinition = definition;
        }

        public void ResetComboState()
        {
            comboIndex = 0;
            comboWindowTimer = 0f;
            attackBuffered = false;
            bufferWindowOpen = false;
        }

        public void SetHitboxActive(bool active)
        {
            if (suppressAnimHitboxEvents)
                return;

            ApplyHitboxActive(active);
        }

        public void SetHitboxSize(Vector2 size)
        {
            if (hitboxCollider == null || hitboxVisual == null)
                return;

            hitboxCollider.size = size;
            hitboxVisual.size = size;
        }

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            movement = GetComponent<PlayerMovement>();
            grapple = GetComponent<GrapplingHookController>();
            combatMode = GetComponent<PlayerCombatMode>();
            bodyCollider = GetComponent<CapsuleCollider2D>();
            body = GetComponent<Rigidbody2D>();
            attackAction = playerInput.actions["Attack"];
            characterVisual = GetComponentInChildren<PlayerCharacterVisual>();
            if (characterVisual != null)
                visualAnimator = characterVisual.GetComponent<Animator>();

            var health = GetComponent<Health>();
            health.DestroyOnDeath = false;

            ResolveComboDefinition();
            ResolvePowerAttackDefinition();
            EnsureHitbox();
        }

        void Start()
        {
            if (combatMode == null)
                combatMode = GetComponent<PlayerCombatMode>();
        }

        void ResolveComboDefinition()
        {
            if (comboDefinition != null)
                return;

            comboDefinition = Resources.Load<ComboDefinition>(DefaultComboResourcePath);

#if UNITY_EDITOR
            if (comboDefinition == null)
            {
                comboDefinition = UnityEditor.AssetDatabase.LoadAssetAtPath<ComboDefinition>(
                    "Assets/_MainGame/Resources/Combos/FistCombo.asset");
            }
#endif

            if (comboDefinition == null)
                Debug.LogWarning("[PlayerFistCombat] ComboDefinition not assigned and FistCombo asset missing.");
        }

        void ResolvePowerAttackDefinition()
        {
            if (powerAttackDefinition != null)
                return;

            powerAttackDefinition = Resources.Load<PowerAttackDefinition>(DefaultPowerAttackResourcePath);

#if UNITY_EDITOR
            if (powerAttackDefinition == null)
            {
                powerAttackDefinition = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerAttackDefinition>(
                    "Assets/_MainGame/Resources/Combos/FistPowerAttack.asset");
            }
#endif

            if (powerAttackDefinition == null)
                Debug.LogWarning("[PlayerFistCombat] PowerAttackDefinition not assigned and FistPowerAttack asset missing.");
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
            attackCooldownCounter -= Time.deltaTime;

            if (comboIndex > 0)
            {
                comboWindowTimer -= Time.deltaTime;
                if (comboWindowTimer <= 0f)
                    comboIndex = 0;
            }

            if (grapple != null && grapple.IsActive)
                return;

            if (combatMode != null && combatMode.ShouldBlockNormalAttackInput())
                return;

            if (!attackAction.WasPressedThisFrame())
                return;

            if (isAttacking && bufferWindowOpen)
            {
                BufferAttackInput();
                return;
            }

            if (isAttacking)
                return;

            TryAttack();
        }

        public bool TryAttackFromCombatMode()
        {
            if (isAttacking || comboDefinition == null || comboDefinition.StepCount == 0)
                return false;

            if (attackCooldownCounter > 0f)
                return false;

            TryAttack();
            return isAttacking;
        }

        public bool CancelAttack()
        {
            if (!isAttacking)
                return false;

            if (attackCoroutine != null)
            {
                StopCoroutine(attackCoroutine);
                attackCoroutine = null;
            }

            isAttacking = false;
            isPowerAttacking = false;
            suppressAnimHitboxEvents = false;
            comboIndex = 0;
            attackBuffered = false;
            bufferWindowOpen = false;
            ApplyHitboxActive(false);
            SetAttackingState(false);
            return true;
        }

        void BufferAttackInput() => attackBuffered = true;

        void TryAttack(bool fromInputBuffer = false)
        {
            if (isAttacking || comboDefinition == null || comboDefinition.StepCount == 0)
                return;

            if (!fromInputBuffer && attackCooldownCounter > 0f)
                return;

            if (weaponDefinition != null && weaponDefinition.WeaponId == "dagger")
            {
                attackCoroutine = StartCoroutine(DaggerAutoComboRoutine());
                comboIndex = 0;
                comboWindowTimer = comboResetTime;
                attackCooldownCounter = finisherCooldown;
                return;
            }

            int hitStep = comboIndex + 1;
            attackCoroutine = StartCoroutine(StrikeRoutine(comboDefinition.GetStep(comboIndex), hitStep));
            comboIndex = (comboIndex + 1) % comboDefinition.StepCount;
            comboWindowTimer = comboResetTime;
            attackCooldownCounter = comboIndex == 0 ? finisherCooldown : comboStepCooldown;
        }

        IEnumerator DaggerAutoComboRoutine()
        {
            isAttacking = true;
            attackBuffered = false;
            bufferWindowOpen = false;
            SetAttackingState(true);

            int facing = movement != null ? movement.FacingDirection : 1;
            int stepCount = comboDefinition.StepCount;

            for (int i = 0; i < stepCount; i++)
            {
                var strike = comboDefinition.GetStep(i);

                if (visualAnimator != null)
                {
                    visualAnimator.SetInteger(AttackFistConHash, i + 1);
                    visualAnimator.SetTrigger(AttackFistHash);
                }

                ApplyHitboxStep(strike, facing);
                hitboxDamage.ResetHits();
                hitboxDamage.Configure(ResolveStrikeDamage(strike));

                if (strike.forwardNudge > 0f && body != null)
                    body.linearVelocity = new Vector2(facing * strike.forwardNudge, body.linearVelocity.y);

                float hold = strike.motionHold > 0f ? strike.motionHold : 0.1f;
                yield return new WaitForSeconds(hold);
            }

            ApplyHitboxActive(false);
            SetAttackingState(false);
            isAttacking = false;
            attackCoroutine = null;

            if (attackBuffered)
            {
                attackBuffered = false;
                TryAttack(fromInputBuffer: true);
            }
        }

        public bool TryStartPowerAttack()
        {
            if (!CanStartPowerAttack)
                return false;

            attackCoroutine = StartCoroutine(PowerStrikeRoutine());
            return true;
        }

        public bool TryPlayTransitionStrike(ComboDefinition transitionCombo, string animatorStateName = null)
        {
            if (transitionCombo == null || transitionCombo.StepCount == 0)
                return false;

            if (isAttacking)
                return false;

            attackCoroutine = StartCoroutine(TransitionStrikeRoutine(transitionCombo, animatorStateName));
            return true;
        }

        IEnumerator StrikeRoutine(ComboDefinition.Step strike, int hitStep)
        {
            isAttacking = true;
            attackBuffered = false;
            bufferWindowOpen = false;
            SetAttackingState(true);

            if (visualAnimator != null)
            {
                visualAnimator.SetInteger(AttackFistConHash, hitStep);
                visualAnimator.SetTrigger(AttackFistHash);
            }

            int strikeFacing = movement != null ? movement.FacingDirection : 1;
            ApplyHitboxStep(strike, strikeFacing);
            hitboxDamage.ResetHits();
            hitboxDamage.Configure(ResolveStrikeDamage(strike));
            ApplyHitboxActive(false);

            if (strike.forwardNudge > 0f && body != null)
                body.linearVelocity = new Vector2(strikeFacing * strike.forwardNudge, body.linearVelocity.y);

            float motionHold = strike.motionHold;
            float preBufferWait = Mathf.Max(0f, motionHold - comboInputBuffer);
            if (preBufferWait > 0f)
                yield return new WaitForSeconds(preBufferWait);

            bufferWindowOpen = true;
            float bufferElapsed = 0f;
            while (bufferElapsed < comboInputBuffer)
            {
                bufferElapsed += Time.deltaTime;
                yield return null;
            }

            bufferWindowOpen = false;
            ApplyHitboxActive(false);
            SetAttackingState(false);
            isAttacking = false;

            attackCoroutine = null;

            if (attackBuffered)
            {
                attackBuffered = false;
                TryAttack(fromInputBuffer: true);
            }
        }

        IEnumerator TransitionStrikeRoutine(ComboDefinition transitionCombo, string animatorStateName)
        {
            isAttacking = true;
            isPlayingTransitionMove = true;
            attackBuffered = false;
            bufferWindowOpen = false;
            SetAttackingState(true);

            if (visualAnimator != null && !string.IsNullOrEmpty(animatorStateName))
                visualAnimator.Play(animatorStateName, 0, 0f);

            int facing = movement != null ? movement.FacingDirection : 1;
            int stepCount = transitionCombo.StepCount;

            for (int i = 0; i < stepCount; i++)
            {
                var strike = transitionCombo.GetStep(i);

                ApplyHitboxStep(strike, facing);
                hitboxDamage.ResetHits();
                hitboxDamage.Configure(ResolveStrikeDamage(strike));

                if (body != null && (strike.forwardNudge > 0f || strike.launchVelocityY != 0f))
                {
                    float vx = strike.forwardNudge > 0f ? facing * strike.forwardNudge : body.linearVelocity.x;
                    float vy = strike.launchVelocityY != 0f ? strike.launchVelocityY : body.linearVelocity.y;
                    body.linearVelocity = new Vector2(vx, vy);
                }

                float hold = strike.motionHold > 0f ? strike.motionHold : 0.2f;
                yield return new WaitForSeconds(hold);
            }

            ApplyHitboxActive(false);
            SetAttackingState(false);
            isAttacking = false;
            isPlayingTransitionMove = false;
            attackCoroutine = null;
        }

        IEnumerator PowerStrikeRoutine()
        {
            isAttacking = true;
            isPowerAttacking = true;
            suppressAnimHitboxEvents = true;
            attackBuffered = false;
            bufferWindowOpen = false;
            SetAttackingState(true);
            ApplyHitboxActive(false);

            int facing = movement != null ? movement.FacingDirection : 1;
            float activeDuration = powerAttackDefinition.HitActiveDuration;
            float interval = powerAttackDefinition.StrikeInterval;
            int patternLength = powerAttackDefinition.PatternLength;

            for (int i = 0; i < patternLength; i++)
            {
                int con = powerAttackDefinition.GetPatternStep(i);
                var strike = powerAttackDefinition.GetHitboxForCon(con);
                bool isLastHit = i == patternLength - 1;

                if (visualAnimator != null)
                {
                    visualAnimator.SetInteger(AttackFistConHash, con);
                    visualAnimator.SetTrigger(AttackFistHash);
                }

                ApplyHitboxStep(strike, facing);
                hitboxDamage.ResetHits();
                hitboxDamage.Configure(powerAttackDefinition.DamagePerHit);

                float holdDuration = isLastHit
                    ? powerAttackDefinition.FinisherHoldDuration
                    : activeDuration;

                ApplyHitboxActive(true);
                yield return new WaitForSeconds(holdDuration);
                ApplyHitboxActive(false);

                if (!isLastHit)
                {
                    float waitRemainder = interval - activeDuration;
                    if (waitRemainder > 0f)
                        yield return new WaitForSeconds(waitRemainder);
                }
            }

            ApplyHitboxActive(false);
            SetAttackingState(false);
            isAttacking = false;
            isPowerAttacking = false;
            suppressAnimHitboxEvents = false;
            attackCoroutine = null;
        }

        void ApplyHitboxStep(ComboDefinition.Step strike, int facing)
        {
            float anchorY = bodyCollider != null ? bodyCollider.offset.y : 0f;
            hitboxRoot.localPosition = new Vector3(
                strike.hitboxOffset.x * facing,
                anchorY + strike.hitboxOffset.y,
                0f);
            hitboxCollider.size = strike.hitboxSize;
            hitboxVisual.size = strike.hitboxSize;
            hitboxVisual.color = strike.useRightHand
                ? hitboxColor
                : new Color(0.85f, 0.78f, 1f, 0.42f);
        }

        void ApplyHitboxActive(bool active)
        {
            if (hitboxCollider == null || hitboxVisual == null)
                return;

            if (active && !isAttacking)
                return;

            hitboxCollider.enabled = active;
            hitboxVisual.enabled = active;
        }

        static float ResolveStrikeDamage(ComboDefinition.Step strike, float fallbackDamage)
        {
            return strike.damage > 0f ? strike.damage : fallbackDamage;
        }

        float ResolveStrikeDamage(ComboDefinition.Step strike) => ResolveStrikeDamage(strike, damage);
    }
}
