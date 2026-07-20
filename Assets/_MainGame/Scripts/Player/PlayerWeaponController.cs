using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ClockWork.Game
{
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(PlayerFistCombat))]
    public class PlayerWeaponController : MonoBehaviour
    {
        const string FistWeaponResourcePath = "Weapons/Fist";
        const string HammerWeaponResourcePath = "Weapons/Hammer";

        [SerializeField] WeaponDefinition[] weaponQueue;

        PlayerInput playerInput;
        PlayerFistCombat combat;
        PlayerCombatMode combatMode;
        InputAction weaponSlot1Action;
        InputAction weaponSlot2Action;
        InputAction weaponNextAction;
        int currentIndex;

        public WeaponDefinition CurrentWeapon => combat != null ? combat.EquippedWeapon : null;
        public int CurrentIndex => currentIndex;
        public int WeaponCount => weaponQueue != null ? weaponQueue.Length : 0;

        public event Action<WeaponDefinition> WeaponChanged;

        void Awake()
        {
            playerInput = GetComponent<PlayerInput>();
            combat = GetComponent<PlayerFistCombat>();
            combatMode = GetComponent<PlayerCombatMode>();
            BindInputActions();
            ResolveWeaponAssets();
        }

        void OnEnable()
        {
            BindInputActions();
        }

        void Start()
        {
            if (weaponQueue != null && weaponQueue.Length > 0)
                EquipWeaponAtIndex(0, playTransition: false);
            else
                Debug.LogWarning("[PlayerWeaponController] Weapon queue is empty.");
        }

        void Update()
        {
            if (combatMode != null && combatMode.IsInCombatMode)
                return;

            if (combat != null && combat.IsAttacking)
                return;

            if (weaponSlot1Action != null && weaponSlot1Action.WasPressedThisFrame())
                TryInstantSwapToIndex(0);

            if (weaponSlot2Action != null && weaponSlot2Action.WasPressedThisFrame())
                TryInstantSwapToIndex(1);

            if (weaponNextAction != null && weaponNextAction.WasPressedThisFrame())
                TryInstantSwapToIndex(GetNextIndex());
        }

        public bool TryInstantSwapToIndex(int index)
        {
            if (combatMode != null && combatMode.IsTapWindow)
                return false;

            return EquipWeaponAtIndex(index, playTransition: false);
        }

        public bool TryTransitionToIndex(int index)
        {
            var weapon = GetWeaponAt(index);
            if (weapon == null || combat == null)
                return false;

            if (combat.IsAttacking)
                return false;

            var equipped = combat.EquippedWeapon;
            if (equipped != null && equipped.WeaponId == weapon.WeaponId)
                return false;

            combat.ConfigureWeapon(weapon);
            currentIndex = index;
            WeaponChanged?.Invoke(weapon);

            if (weapon.TransitionCombo != null)
                combat.TryPlayTransitionStrike(weapon.TransitionCombo);

            return true;
        }

        public bool TryTransitionToNext()
        {
            int nextIndex = GetNextIndex();
            return TryTransitionToIndex(nextIndex);
        }

        public bool CanTransitionToNext() => CanTransitionToIndex(GetNextIndex());

        public bool CanTransitionToIndex(int index)
        {
            var weapon = GetWeaponAt(index);
            if (weapon == null || combat == null || combat.IsAttacking)
                return false;

            var equipped = combat.EquippedWeapon;
            return equipped == null || equipped.WeaponId != weapon.WeaponId;
        }

        int GetNextIndex()
        {
            if (weaponQueue == null || weaponQueue.Length == 0)
                return 0;

            return (currentIndex + 1) % weaponQueue.Length;
        }

        WeaponDefinition GetWeaponAt(int index)
        {
            if (weaponQueue == null || weaponQueue.Length == 0)
                return null;

            if (index < 0 || index >= weaponQueue.Length)
                return null;

            return weaponQueue[index];
        }

        bool EquipWeaponAtIndex(int index, bool playTransition)
        {
            var weapon = GetWeaponAt(index);
            if (weapon == null || combat == null)
                return false;

            if (combat.IsAttacking)
                return false;

            var equipped = combat.EquippedWeapon;
            if (equipped != null && equipped.WeaponId == weapon.WeaponId)
                return false;

            combat.ConfigureWeapon(weapon);
            currentIndex = index;
            WeaponChanged?.Invoke(weapon);

            if (playTransition && weapon.TransitionCombo != null)
                combat.TryPlayTransitionStrike(weapon.TransitionCombo);

            return true;
        }

        void BindInputActions()
        {
            if (playerInput == null)
                return;

            weaponSlot1Action = playerInput.actions.FindAction("WeaponSlot1", false);
            weaponSlot2Action = playerInput.actions.FindAction("WeaponSlot2", false);
            weaponNextAction = playerInput.actions.FindAction("WeaponNext", false);
        }

        void ResolveWeaponAssets()
        {
            if (weaponQueue != null && weaponQueue.Length > 0)
                return;

            var fist = Resources.Load<WeaponDefinition>(FistWeaponResourcePath);
            var hammer = Resources.Load<WeaponDefinition>(HammerWeaponResourcePath);

#if UNITY_EDITOR
            if (fist == null)
            {
                fist = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    "Assets/_MainGame/Resources/Weapons/Fist.asset");
            }

            if (hammer == null)
            {
                hammer = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    "Assets/_MainGame/Resources/Weapons/Hammer.asset");
            }
#endif

            if (fist != null && hammer != null)
                weaponQueue = new[] { fist, hammer };
            else
                Debug.LogWarning("[PlayerWeaponController] Weapon assets missing — queue not built.");
        }
    }
}
