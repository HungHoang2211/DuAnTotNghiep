using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Consumable", fileName = "ConsumableAbility")]
    public sealed class ConsumableAbility : ItemAbility
    {
        public const string Name = "Consumable";

        [SerializeField] private float restoreHp;
        [SerializeField] private float restoreHunger;
        [SerializeField] private float restoreThirst;

        public override string AbilityName => Name;

        public float RestoreHp => restoreHp;
        public float RestoreHunger => restoreHunger;
        public float RestoreThirst => restoreThirst;
    }
}
