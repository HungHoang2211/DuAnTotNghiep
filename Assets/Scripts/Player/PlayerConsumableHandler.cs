using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.Items;

namespace SimpleSurvival.Player
{
    public sealed class PlayerConsumableHandler : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private ItemActionPanel actionPanel;

        private void Awake()
        {
            if (playerStats == null)
                playerStats = GetComponentInChildren<PlayerStats>();
        }

        private void OnEnable()
        {
            if (actionPanel != null)
                actionPanel.OnUseConsumableRequested += HandleConsume;
        }

        private void OnDisable()
        {
            if (actionPanel != null)
                actionPanel.OnUseConsumableRequested -= HandleConsume;
        }

        private void HandleConsume(ItemStack stack)
        {
            if (stack == null || playerStats == null) return;

            ConsumableAbility ability = stack.ItemData.GetAbility<ConsumableAbility>();
            if (ability == null) return;

            if (AreAllTargetsFull(ability))
                return;

            if (ability.RestoreHp > 0f)
                playerStats.Heal(ability.RestoreHp);

            if (ability.RestoreHunger > 0f)
                playerStats.AddHunger(ability.RestoreHunger);

            if (ability.RestoreThirst > 0f)
                playerStats.AddThirst(ability.RestoreThirst);
        }

        private bool AreAllTargetsFull(ConsumableAbility ability)
        {
            bool hpTarget = ability.RestoreHp > 0f;
            bool hungerTarget = ability.RestoreHunger > 0f;
            bool thirstTarget = ability.RestoreThirst > 0f;

            bool hpFull = !hpTarget || playerStats.HP >= playerStats.MaxHP;
            bool hungerFull = !hungerTarget || playerStats.Hunger >= playerStats.MaxHunger;
            bool thirstFull = !thirstTarget || playerStats.Thirst >= playerStats.MaxThirst;

            return hpFull && hungerFull && thirstFull;
        }
    }
}