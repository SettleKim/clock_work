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



        static readonly int AttackFistHash = Animator.StringToHash("attack_fist");

        static readonly int AttackFistConHash = Animator.StringToHash("attack_fist_con");



        [Header("Combo Data")]

        [SerializeField] ComboDefinition comboDefinition;



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

        DamageDealer hitboxDamage;

        SpriteRenderer hitboxVisual;



        int comboIndex;

        float comboWindowTimer;

        float attackCooldownCounter;

        bool isAttacking;

        bool attackBuffered;

        bool bufferWindowOpen;



        public bool IsAttacking => isAttacking;

        public ComboDefinition ComboDefinition => comboDefinition;



        public void ConfigureCombo(ComboDefinition definition)

        {

            if (definition != null)

                comboDefinition = definition;

        }



        public void SetHitboxActive(bool active)

        {

            if (hitboxCollider == null || hitboxVisual == null)

                return;



            if (active && !isAttacking)

                return;



            hitboxCollider.enabled = active;

            hitboxVisual.enabled = active;

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

            bodyCollider = GetComponent<CapsuleCollider2D>();

            attackAction = playerInput.actions["Attack"];

            characterVisual = GetComponentInChildren<PlayerCharacterVisual>();

            if (characterVisual != null)

                visualAnimator = characterVisual.GetComponent<Animator>();



            var health = GetComponent<Health>();

            health.DestroyOnDeath = false;



            ResolveComboDefinition();

            EnsureHitbox();

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

                    "Assets/Game/Resources/Combos/FistCombo.asset");

            }

#endif



            if (comboDefinition == null)

                Debug.LogWarning("[PlayerFistCombat] ComboDefinition not assigned and FistCombo asset missing.");

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



            if (attackAction.WasPressedThisFrame())

            {

                if (isAttacking && bufferWindowOpen)

                    BufferAttackInput();

                else if (!isAttacking)

                    TryAttack();

            }

        }



        void BufferAttackInput() => attackBuffered = true;



        void TryAttack(bool fromInputBuffer = false)

        {

            if (isAttacking || comboDefinition == null || comboDefinition.StepCount == 0)

                return;



            if (!fromInputBuffer && attackCooldownCounter > 0f)

                return;



            int hitStep = comboIndex + 1;

            StartCoroutine(StrikeRoutine(comboDefinition.GetStep(comboIndex), hitStep));

            comboIndex = (comboIndex + 1) % comboDefinition.StepCount;

            comboWindowTimer = comboResetTime;



            attackCooldownCounter = comboIndex == 0 ? finisherCooldown : comboStepCooldown;

        }



        IEnumerator StrikeRoutine(ComboDefinition.Step strike, int hitStep)

        {

            isAttacking = true;

            attackBuffered = false;

            bufferWindowOpen = false;

            characterVisual?.SetAttacking(true);



            if (visualAnimator != null)

            {

                visualAnimator.SetInteger(AttackFistConHash, hitStep);

                visualAnimator.SetTrigger(AttackFistHash);

            }



            int facing = movement != null ? movement.FacingDirection : 1;

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

            hitboxDamage.ResetHits();

            hitboxDamage.Configure(damage);

            SetHitboxActive(false);



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

            SetHitboxActive(false);

            characterVisual?.SetAttacking(false);

            isAttacking = false;



            if (attackBuffered)

            {

                attackBuffered = false;

                TryAttack(fromInputBuffer: true);

            }

        }

    }

}


