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
        const string GreatswordWeaponResourcePath = "Weapons/Greatsword";
        const string DaggerWeaponResourcePath = "Weapons/Dagger";

        [SerializeField] WeaponDefinition[] weaponQueue;

        PlayerInput playerInput;
        PlayerFistCombat combat;
        PlayerCombatMode combatMode;
        PlayerHammerGrapple hammerGrapple;
        InputAction weaponSlot1Action;
        InputAction weaponSlot2Action;
        InputAction weaponSlot3Action;
        InputAction weaponSlot4Action;
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

            if (weaponSlot3Action != null && weaponSlot3Action.WasPressedThisFrame())
                TryInstantSwapToIndex(2);

            if (weaponSlot4Action != null && weaponSlot4Action.WasPressedThisFrame())
                TryInstantSwapToIndex(3);

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

            // 특수 전환기: 망치 -> 주먹 = 그래플 이동기 (쌍 기반. 조합이 늘면 테이블화 권장)
            bool hammerToFist = equipped != null && equipped.WeaponId == "hammer" && weapon.WeaponId == "fist";

            combat.ConfigureWeapon(weapon);
            currentIndex = index;
            WeaponChanged?.Invoke(weapon);

            if (hammerToFist)
            {
                if (hammerGrapple == null)
                    hammerGrapple = GetComponent<PlayerHammerGrapple>();
                if (hammerGrapple != null && hammerGrapple.TryLaunch())
                    return true;
            }

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
            weaponSlot3Action = playerInput.actions.FindAction("WeaponSlot3", false);
            weaponSlot4Action = playerInput.actions.FindAction("WeaponSlot4", false);
            weaponNextAction = playerInput.actions.FindAction("WeaponNext", false);
        }

        void ResolveWeaponAssets()
        {
            if (weaponQueue != null && weaponQueue.Length > 0)
                return;

            var fist = Resources.Load<WeaponDefinition>(FistWeaponResourcePath);
            var hammer = Resources.Load<WeaponDefinition>(HammerWeaponResourcePath);
            var greatsword = Resources.Load<WeaponDefinition>(GreatswordWeaponResourcePath);
            var dagger = Resources.Load<WeaponDefinition>(DaggerWeaponResourcePath);

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

            if (greatsword == null)
            {
                greatsword = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    "Assets/_MainGame/Resources/Weapons/Greatsword.asset");
            }

            if (dagger == null)
            {
                dagger = UnityEditor.AssetDatabase.LoadAssetAtPath<WeaponDefinition>(
                    "Assets/_MainGame/Resources/Weapons/Dagger.asset");
            }
#endif

            if (fist != null && hammer != null && greatsword != null && dagger != null)
                weaponQueue = new[] { fist, hammer, greatsword, dagger };
            else
                Debug.LogWarning("[PlayerWeaponController] Weapon assets missing — queue not built.");
        }
    }
}
