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
        }

        void EnsurePlayerCombat()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null)
                return;

            if (player.GetComponent<Health>() == null)
                player.AddComponent<Health>();

            if (player.GetComponent<PlayerFistCombat>() == null)
            {
                var fistCombat = player.AddComponent<PlayerFistCombat>();
#if UNITY_EDITOR
                var fistCombo = UnityEditor.AssetDatabase.LoadAssetAtPath<ComboDefinition>(
                    "Assets/Game/Resources/Combos/FistCombo.asset");
                if (fistCombo != null)
                    fistCombat.ConfigureCombo(fistCombo);
#endif
            }

            PlayerVisualSetup.EnsureAndConfigure(player);
        }
    }
}
