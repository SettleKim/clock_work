using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerEnergyGauge))]
    [RequireComponent(typeof(PlayerFistCombat))]
    [RequireComponent(typeof(PlayerWeaponController))]
    [DefaultExecutionOrder(-50)]
    public class PlayerCombatMode : MonoBehaviour
    {
        enum State
        {
            Idle,
            TapWindow,
            HoldDrain,
            WListeningDuringAttack
        }

        const string DefaultSettingsResourcePath = "Combat/CombatModeSettings";

        [SerializeField] CombatModeSettings settings;

        PlayerInput playerInput;
        PlayerEnergyGauge energyGauge;
        PlayerFistCombat fistCombat;
        PlayerWeaponController weaponController;

        InputAction combatModeAction;
        InputAction attackAction;
        InputAction weaponNextAction;
        InputAction weaponSlot1Action;
        InputAction weaponSlot2Action;
        InputAction weaponSlot3Action;
        InputAction weaponSlot4Action;

        State state = State.Idle;
        float wCooldownTimer;
        float tapWindowTimer;
        float wPressTime;
        bool wHeld;
        bool holdThresholdTriggered;

        public bool IsTapWindow => state == State.TapWindow;
        public bool IsInCombatMode => state == State.TapWindow || state == State.HoldDrain;
        public bool BlocksGrapple => state == State.TapWindow;
        public float HoldMoveSpeedBonus =>
            state == State.HoldDrain && settings != null ? settings.HoldMoveSpeedBonus : 0f;
        public float TapWindowNormalized =>
            settings != null && settings.TapWindowDuration > 0f
                ? Mathf.Clamp01(tapWindowTimer / settings.TapWindowDuration)
                : 0f;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            energyGauge = GetComponent<PlayerEnergyGauge>();
            fistCombat = GetComponent<PlayerFistCombat>();
            weaponController = GetComponent<PlayerWeaponController>();
            ResolveSettings();
            BindInputActions();
        }

        void OnEnable()
        {
            BindInputActions();
        }

        void Start()
        {
            if (weaponController == null)
                weaponController = GetComponent<PlayerWeaponController>();
        }

        void ResolveSettings()
        {
            if (settings != null)
                return;

            settings = Resources.Load<CombatModeSettings>(DefaultSettingsResourcePath);

#if UNITY_EDITOR
            if (settings == null)
            {
                settings = UnityEditor.AssetDatabase.LoadAssetAtPath<CombatModeSettings>(
                    "Assets/_MainGame/Resources/Combat/CombatModeSettings.asset");
            }
#endif

            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<CombatModeSettings>();
                Debug.LogWarning("[PlayerCombatMode] CombatModeSettings asset missing — using runtime defaults.");
            }

            if (settings.HoldEntryCost <= 0f || settings.HoldDrainPerSecond <= 0f)
                Debug.LogWarning("[PlayerCombatMode] CombatModeSettings hold energy values invalid — check asset.");
        }

        void BindInputActions()
        {
            if (playerInput == null)
                return;

            combatModeAction = playerInput.actions.FindAction("PowerMode", false);
            attackAction = playerInput.actions.FindAction("Attack", false);
            weaponNextAction = playerInput.actions.FindAction("WeaponNext", false);
            weaponSlot1Action = playerInput.actions.FindAction("WeaponSlot1", false);
            weaponSlot2Action = playerInput.actions.FindAction("WeaponSlot2", false);
            weaponSlot3Action = playerInput.actions.FindAction("WeaponSlot3", false);
            weaponSlot4Action = playerInput.actions.FindAction("WeaponSlot4", false);
        }

        void Update()
        {
            if (settings == null || energyGauge == null || fistCombat == null)
                return;

            TickCooldown();
            TickTapWindow();

            switch (state)
            {
                case State.Idle:
                    HandleIdleWInput();
                    break;

                case State.TapWindow:
                    HandleTapWindowInput();
                    break;

                case State.HoldDrain:
                    HandleHoldDrainInput();
                    break;

                case State.WListeningDuringAttack:
                    HandleAttackCancelWInput();
                    break;
            }
        }

        void LateUpdate()
        {
            if (state == State.WListeningDuringAttack && !wHeld && !fistCombat.IsAttacking)
                state = State.Idle;
        }

        void TickCooldown()
        {
            if (wCooldownTimer > 0f)
                wCooldownTimer -= Time.unscaledDeltaTime;
        }

        void TickTapWindow()
        {
            if (state != State.TapWindow)
                return;

            tapWindowTimer -= Time.unscaledDeltaTime;
            if (tapWindowTimer <= 0f)
                EndTapWindowTimeout();
        }

        void HandleIdleWInput()
        {
            if (combatModeAction == null)
                return;

            if (combatModeAction.WasPressedThisFrame())
            {
                wPressTime = Time.unscaledTime;
                wHeld = true;
                holdThresholdTriggered = false;

                if (fistCombat.IsAttacking)
                    state = State.WListeningDuringAttack;
            }

            if (!wHeld)
                return;

            if (state == State.WListeningDuringAttack)
                return;

            if (combatModeAction.IsPressed())
            {
                if (!holdThresholdTriggered
                    && Time.unscaledTime - wPressTime >= settings.HoldThreshold)
                {
                    holdThresholdTriggered = true;
                    TryEnterHoldFromIdle();
                }

                return;
            }

            if (combatModeAction.WasReleasedThisFrame())
            {
                wHeld = false;
                float held = Time.unscaledTime - wPressTime;
                if (held < settings.HoldThreshold)
                    TryEnterTapFromIdle();
            }
        }

        void HandleAttackCancelWInput()
        {
            if (combatModeAction == null)
                return;

            if (combatModeAction.WasPressedThisFrame())
            {
                wPressTime = Time.unscaledTime;
                wHeld = true;
                holdThresholdTriggered = false;
            }

            if (!wHeld)
                return;

            if (combatModeAction.IsPressed())
            {
                if (!holdThresholdTriggered
                    && Time.unscaledTime - wPressTime >= settings.HoldThreshold)
                {
                    holdThresholdTriggered = true;
                    TryCancelAttackIntoHold();
                }

                return;
            }

            if (combatModeAction.WasReleasedThisFrame())
            {
                wHeld = false;
                float held = Time.unscaledTime - wPressTime;
                if (held < settings.HoldThreshold)
                    TryCancelAttackIntoTap();
                else
                    state = State.Idle;
            }
        }

        void TryEnterTapFromIdle()
        {
            if (wCooldownTimer > 0f || !energyGauge.HasEnough(settings.TapEntryCost))
                return;

            EnterTapWindow();
        }

        void TryEnterHoldFromIdle()
        {
            if (wCooldownTimer > 0f
                || settings.HoldEntryCost <= 0f
                || !energyGauge.HasEnough(settings.HoldEntryCost))
            {
                wHeld = false;
                return;
            }

            EnterHoldDrain();
        }

        void TryCancelAttackIntoTap()
        {
            if (!energyGauge.HasEnough(settings.TapEntryCost))
            {
                state = State.Idle;
                return;
            }

            if (!fistCombat.CancelAttack())
            {
                state = State.Idle;
                return;
            }

            EnterTapWindow();
        }

        void TryCancelAttackIntoHold()
        {
            if (settings.HoldEntryCost <= 0f || !energyGauge.HasEnough(settings.HoldEntryCost))
            {
                wHeld = false;
                state = State.Idle;
                return;
            }

            if (!fistCombat.CancelAttack())
            {
                wHeld = false;
                state = State.Idle;
                return;
            }

            EnterHoldDrain();
        }

        void HandleTapWindowInput()
        {
            if (TryHandlePriorityTapInput())
                return;

            if (combatModeAction != null && combatModeAction.WasPressedThisFrame())
                EndTapWindowCancel();
        }

        bool TryHandlePriorityTapInput()
        {
            if (attackAction != null && attackAction.WasPressedThisFrame())
            {
                ConfirmTapPowerAttack();
                return true;
            }

            if (weaponNextAction != null && weaponNextAction.WasPressedThisFrame())
            {
                ConfirmTapWeaponNext();
                return true;
            }

            if (weaponSlot1Action != null && weaponSlot1Action.WasPressedThisFrame())
            {
                ConfirmTapWeaponSlot(0);
                return true;
            }

            if (weaponSlot2Action != null && weaponSlot2Action.WasPressedThisFrame())
            {
                ConfirmTapWeaponSlot(1);
                return true;
            }

            if (weaponSlot3Action != null && weaponSlot3Action.WasPressedThisFrame())
            {
                ConfirmTapWeaponSlot(2);
                return true;
            }

            if (weaponSlot4Action != null && weaponSlot4Action.WasPressedThisFrame())
            {
                ConfirmTapWeaponSlot(3);
                return true;
            }

            return false;
        }

        void HandleHoldDrainInput()
        {
            if (!energyGauge.DrainContinuous(settings.HoldDrainPerSecond, Time.unscaledDeltaTime))
            {
                ExitCombatMode();
                return;
            }

            if (combatModeAction != null && !combatModeAction.IsPressed())
            {
                ExitCombatMode();
                return;
            }

            if (weaponSlot1Action != null && weaponSlot1Action.WasPressedThisFrame())
                weaponController?.TryInstantSwapToIndex(0);

            if (weaponSlot2Action != null && weaponSlot2Action.WasPressedThisFrame())
                weaponController?.TryInstantSwapToIndex(1);

            if (weaponSlot3Action != null && weaponSlot3Action.WasPressedThisFrame())
                weaponController?.TryInstantSwapToIndex(2);

            if (weaponSlot4Action != null && weaponSlot4Action.WasPressedThisFrame())
                weaponController?.TryInstantSwapToIndex(3);

            if (attackAction != null && attackAction.WasPressedThisFrame())
            {
                ExitCombatMode();
                fistCombat.TryAttackFromCombatMode();
                return;
            }
        }

        void EnterTapWindow()
        {
            state = State.TapWindow;
            tapWindowTimer = settings.TapWindowDuration;
            wHeld = false;
            CombatSlowMotion.Enter(settings.SlowMotionScale);
        }

        void EnterHoldDrain()
        {
            if (settings.HoldEntryCost <= 0f || !energyGauge.HasEnough(settings.HoldEntryCost))
                return;

            state = State.HoldDrain;
            wHeld = false;
            CombatSlowMotion.Enter(settings.SlowMotionScale);
        }

        void ConfirmTapPowerAttack()
        {
            if (!fistCombat.CanStartPowerAttack)
                return;

            if (!energyGauge.TrySpend(settings.TapConfirmCost))
            {
                EndTapWindowCancel();
                return;
            }

            ExitCombatMode();
            fistCombat.TryStartPowerAttack();
        }

        void ConfirmTapWeaponNext()
        {
            if (weaponController == null || !weaponController.CanTransitionToNext())
                return;

            if (!energyGauge.TrySpend(settings.TapConfirmCost))
            {
                EndTapWindowCancel();
                return;
            }

            weaponController.TryTransitionToNext();
            ExitCombatMode();
        }

        void ConfirmTapWeaponSlot(int slotIndex)
        {
            if (weaponController == null || !weaponController.CanTransitionToIndex(slotIndex))
                return;

            if (!energyGauge.TrySpend(settings.TapConfirmCost))
            {
                EndTapWindowCancel();
                return;
            }

            weaponController.TryTransitionToIndex(slotIndex);
            ExitCombatMode();
        }

        void EndTapWindowTimeout()
        {
            energyGauge.TrySpend(settings.TapTimeoutCost);
            ExitCombatMode();
        }

        void EndTapWindowCancel()
        {
            energyGauge.TrySpend(settings.TapTimeoutCost);
            ExitCombatMode();
        }

        void ExitCombatMode()
        {
            if (state == State.TapWindow || state == State.HoldDrain)
                CombatSlowMotion.Exit();

            state = State.Idle;
            wHeld = false;
            holdThresholdTriggered = false;
            wCooldownTimer = settings.WCooldownDuration;
        }

        public bool ShouldBlockNormalAttackInput()
        {
            return state == State.TapWindow
                || state == State.WListeningDuringAttack
                || state == State.HoldDrain;
        }
    }
}
