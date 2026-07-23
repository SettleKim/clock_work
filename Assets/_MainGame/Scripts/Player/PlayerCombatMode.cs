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
            ToggleActive
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

        public bool IsTapWindow => state == State.ToggleActive;
        public bool IsInCombatMode => state == State.ToggleActive;
        // 예전 TapWindow(짧은 판정 순간)만 갈고리를 막았고, 지속 홀드 중엔 막지 않았음.
        // 지금 토글은 그 지속 홀드에 해당하므로 갈고리를 막지 않는다.
        public bool BlocksGrapple => false;
        public float HoldMoveSpeedBonus =>
            state == State.ToggleActive && settings != null ? settings.HoldMoveSpeedBonus : 0f;

        // 토글 상태에선 더 이상 시간제한이 없어서, 남은 에너지 비율을 보여준다 (UI 게이지 용도 재사용).
        public float TapWindowNormalized =>
            energyGauge != null && energyGauge.MaxEnergy > 0f
                ? Mathf.Clamp01(energyGauge.CurrentEnergy / energyGauge.MaxEnergy)
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

            switch (state)
            {
                case State.Idle:
                    HandleIdleWInput();
                    break;

                case State.ToggleActive:
                    HandleToggleActiveInput();
                    break;
            }
        }

        void TickCooldown()
        {
            if (wCooldownTimer > 0f)
                wCooldownTimer -= Time.unscaledDeltaTime;
        }

        void HandleIdleWInput()
        {
            if (combatModeAction == null || !combatModeAction.WasPressedThisFrame())
                return;

            // 공격 중에 눌렀으면 먼저 공격을 취소하고 토글 진입을 시도한다.
            if (fistCombat.IsAttacking && !fistCombat.CancelAttack())
                return;

            TryEnterToggle();
        }

        void TryEnterToggle()
        {
            if (wCooldownTimer > 0f || !energyGauge.HasEnough(settings.TapEntryCost))
                return;

            state = State.ToggleActive;
            CombatSlowMotion.Enter(settings.SlowMotionScale);
        }

        void HandleToggleActiveInput()
        {
            if (!energyGauge.DrainContinuous(settings.HoldDrainPerSecond, Time.unscaledDeltaTime))
            {
                ExitCombatMode();
                return;
            }

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

        void EndTapWindowCancel()
        {
            energyGauge.TrySpend(settings.TapTimeoutCost);
            ExitCombatMode();
        }

        void ExitCombatMode()
        {
            if (state == State.ToggleActive)
                CombatSlowMotion.Exit();

            state = State.Idle;
            wCooldownTimer = settings.WCooldownDuration;
        }

        public bool ShouldBlockNormalAttackInput()
        {
            return state == State.ToggleActive;
        }
    }
}
