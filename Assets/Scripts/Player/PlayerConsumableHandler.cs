using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.Items;
using SimpleSurvival.Audio;

namespace SimpleSurvival.Player
{
    public sealed class PlayerConsumableHandler : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;

        private void Awake()
        {
            if (playerStats == null)
                playerStats = GetComponentInChildren<PlayerStats>();
        }

        public bool TryConsume(ItemStack stack)
        {
            if (stack == null || playerStats == null) return false;

            ConsumableAbility ability = stack.ItemData.GetAbility<ConsumableAbility>();
            if (ability == null) return false;

            if (AreAllTargetsFull(ability))
                return false;

            ApplyEffects(ability);

            if (UIAudioController.Instance != null)
                UIAudioController.Instance.PlayUseItem();

            return true;
        }

        private void ApplyEffects(ConsumableAbility ability)
        {
            if (ability.RestoreHp > 0f)
                playerStats.Heal(ability.RestoreHp);

            if (ability.RestoreHunger > 0f)
            {
                playerStats.AddHunger(ability.RestoreHunger);
                playerStats.PauseHungerDecay();
            }

            if (ability.RestoreThirst > 0f)
            {
                playerStats.AddThirst(ability.RestoreThirst);
                playerStats.PauseThirstDecay();
            }
        }

        private bool AreAllTargetsFull(ConsumableAbility ability)
        {
            bool hpTarget = ability.RestoreHp > 0f;
            bool hungerTarget = ability.RestoreHunger > 0f;
            bool thirstTarget = ability.RestoreThirst > 0f;

            bool hpFull = !hpTarget || Mathf.CeilToInt(playerStats.HP) >= playerStats.MaxHP;
            bool hungerFull = !hungerTarget || Mathf.CeilToInt(playerStats.Hunger) >= playerStats.MaxHunger;
            bool thirstFull = !thirstTarget || Mathf.CeilToInt(playerStats.Thirst) >= playerStats.MaxThirst;

            return hpFull && hungerFull && thirstFull;
        }
    }
}