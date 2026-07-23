using UnityEngine;

namespace ClockWork.Game
{
    [CreateAssetMenu(fileName = "WeaponDefinition", menuName = "Clock Work/Combat/Weapon Definition")]
    public class WeaponDefinition : ScriptableObject
    {
        [SerializeField] string weaponId = "fist";
        [SerializeField] string displayName = "Fist";
        [SerializeField] ComboDefinition combo;
        [SerializeField] PowerAttackDefinition powerAttack;
        [SerializeField] Sprite backIcon;
        [SerializeField] Vector2 backAttachOffset = Vector2.zero;

        public string WeaponId => weaponId;
        public string DisplayName => displayName;
        public ComboDefinition Combo => combo;
        public PowerAttackDefinition PowerAttack => powerAttack;
        public Sprite BackIcon => backIcon;
        public Vector2 BackAttachOffset => backAttachOffset;
    }
}
