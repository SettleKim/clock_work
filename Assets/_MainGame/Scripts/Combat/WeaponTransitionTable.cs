using UnityEngine;

namespace ClockWork.Game
{
    /// <summary>
    /// 무기 쌍(순서 무관) 전용 전환 콤보 테이블.
    /// 예: 주먹↔대검, 주먹↔망치처럼 "이 두 무기 사이"에서만 나오는 콤보를 등록한다.
    /// 표에 없는 조합은 특수 전환기 없이 그냥 무기만 바뀐다.
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponTransitionTable", menuName = "Clock Work/Combat/Weapon Transition Table")]
    public class WeaponTransitionTable : ScriptableObject
    {
        [System.Serializable]
        public struct Entry
        {
            public WeaponDefinition weaponA;
            public WeaponDefinition weaponB;
            public ComboDefinition combo;

            [Tooltip("이 콤보 재생 시 직접 재생할 Animator 상태 이름 (visual.controller)")]
            public string animatorStateName;
        }

        [SerializeField] Entry[] entries;

        public bool TryGetTransition(WeaponDefinition from, WeaponDefinition to, out ComboDefinition combo, out string animatorStateName)
        {
            combo = null;
            animatorStateName = null;

            if (entries == null || from == null || to == null)
                return false;

            foreach (var entry in entries)
            {
                bool matchesForward = entry.weaponA == from && entry.weaponB == to;
                bool matchesReverse = entry.weaponA == to && entry.weaponB == from;

                if (!matchesForward && !matchesReverse)
                    continue;

                combo = entry.combo;
                animatorStateName = entry.animatorStateName;
                return combo != null;
            }

            return false;
        }
    }
}
