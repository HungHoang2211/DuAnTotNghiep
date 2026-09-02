using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.Items;
using SimpleSurvival.Audio;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Player
{
    public sealed class PlayerConsumableHandler : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerInventoryQueries inventoryQueries;

        private void Awake()
        {
            if (playerStats == null)
                playerStats = GetComponentInChildren<PlayerStats>();

            if (inventoryQueries == null)
                inventoryQueries = GetComponentInChildren<PlayerInventoryQueries>();
        }

        public bool TryConsume(ItemStack stack)
        {
            if (stack == null || playerStats == null) return false;

            ConsumableAbility ability = stack.ItemData.GetAbility<ConsumableAbility>();
            if (ability == null) return false;

            if (AreAllTargetsFull(ability))
                return false;

            ApplyEffects(ability);
            GrantLeftover(ability);

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

        private void GrantLeftover(ConsumableAbility ability)
        {
            if (ability.LeftoverItem == null || inventoryQueries == null) return;

            int remaining = inventoryQueries.AddItem(ability.LeftoverItem, ability.LeftoverQuantity);
            if (remaining > 0 && FollowNotifyManager.Instance != null)
                FollowNotifyManager.Instance.Notify("Inventory full!", SpeechHudType.Bad);
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