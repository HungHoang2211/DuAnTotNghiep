using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Makes an item usable as a weapon. Combat reads these values when the
    /// item is equipped in the weapon slot.
    /// </summary>
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Weapon", fileName = "WeaponAbility")]
    public sealed class WeaponAbility : ItemAbility
    {
        public const string Name = "Weapon";

        [SerializeField] private float damage;
        [SerializeField] private float attackSpeed;
        [SerializeField] private int bodypartVariantIndex;

        public override string AbilityName => Name;

        public float Damage => damage;
        public float AttackSpeed => attackSpeed;

        /// <summary>Which character bodypart variant to show when this is equipped.</summary>
        public int BodypartVariantIndex => bodypartVariantIndex;
    }
}
