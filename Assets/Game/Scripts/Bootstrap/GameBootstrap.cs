using UnityEngine;

namespace ClockWork.Game
{
    /// <summary>
    /// 새 게임 루프의 진입점. 씬에 하나만 둡니다.
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        [SerializeField] string welcomeMessage = "Clock Work — 새 게임 시작";

        void Awake()
        {
            Debug.Log(welcomeMessage);
            EnsurePlayerCombat();
            EnsureEnergyGaugeUI();
            EnsureWeaponSlotUI();
            EnsureCombatTapWindowUI();
        }

        void EnsurePlayerCombat()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            if (player.GetComponent<Health>() == null)
                player.AddComponent<Health>();

            if (player.GetComponent<PlayerEnergyGauge>() == null)
                player.AddComponent<PlayerEnergyGauge>();

            if (player.GetComponent<PlayerFistCombat>() == null)
            {
                var fistCombat = player.AddComponent<PlayerFistCombat>();
#if UNITY_EDITOR
                var fistCombo = UnityEditor.AssetDatabase.LoadAssetAtPath<ComboDefinition>(
                    "Assets/Game/Resources/Combos/FistCombo.asset");
                if (fistCombo != null)
                    fistCombat.ConfigureCombo(fistCombo);

                var fistPowerAttack = UnityEditor.AssetDatabase.LoadAssetAtPath<PowerAttackDefinition>(
                    "Assets/Game/Resources/Combos/FistPowerAttack.asset");
                if (fistPowerAttack != null)
                    fistCombat.ConfigurePowerAttack(fistPowerAttack);
#endif
            }

            if (player.GetComponent<PlayerWeaponController>() == null)
                player.AddComponent<PlayerWeaponController>();

            if (player.GetComponent<PlayerCombatMode>() == null)
                player.AddComponent<PlayerCombatMode>();

            PlayerVisualSetup.EnsureAndConfigure(player);
        }

        void EnsureEnergyGaugeUI()
        {
            if (FindFirstObjectByType<EnergyGaugeUI>() != null)
                return;

            gameObject.AddComponent<EnergyGaugeUI>();
        }

        void EnsureWeaponSlotUI()
        {
            if (FindFirstObjectByType<WeaponSlotUI>() != null)
                return;

            gameObject.AddComponent<WeaponSlotUI>();
        }

        void EnsureCombatTapWindowUI()
        {
            if (FindFirstObjectByType<CombatTapWindowUI>() != null)
                return;

            gameObject.AddComponent<CombatTapWindowUI>();
        }
    }
}
