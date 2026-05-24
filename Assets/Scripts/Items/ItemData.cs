using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Defines a TYPE of item (its shared, unchanging data). Created as an asset
    /// in the project. Every stone axe in the game points to the same ItemData.
    /// Per-instance state that changes during play (quantity, current durability)
    /// lives in ItemStack, never here.
    /// </summary>
    [CreateAssetMenu(menuName = "Simple Survival/Item Data", fileName = "NewItem")]
    public sealed class ItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemName;
        [SerializeField] private Sprite icon;
        [TextArea] [SerializeField] private string description;

        [Header("Stacking")]
        [SerializeField] private bool isStackable;
        [SerializeField] private int maxStack = 1;

        [Header("Durability")]
        [Tooltip("0 means this item never wears out (resources, consumables, backpacks).")]
        [SerializeField] private int maxDurability;

        [Header("Tags")]
        [Tooltip("Equipment slots accept only items carrying the matching tag.")]
        [SerializeField] private List<string> tags = new List<string>();

        [Header("Abilities")]
        [Tooltip("Drag ability assets here. An item with no abilities is a plain resource.")]
        [SerializeField] private List<ItemAbility> abilities = new List<ItemAbility>();

        public string ItemName => itemName;
        public Sprite Icon => icon;
        public string Description => description;

        public bool IsStackable => isStackable;
        public int MaxStack => isStackable ? maxStack : 1;

        public int MaxDurability => maxDurability;
        public bool IsDurable => maxDurability > 0;

        /// <summary>
        /// Returns the ability of the requested type, or null if this item
        /// does not have it. Lets callers ask "can this item do X" without
        /// ever doing a type check themselves.
        /// </summary>
        public TAbility GetAbility<TAbility>() where TAbility : ItemAbility
        {
            foreach (ItemAbility ability in abilities)
            {
                if (ability is TAbility match)
                {
                    return match;
                }
            }

            return null;
        }

        public bool HasAbility<TAbility>() where TAbility : ItemAbility
        {
            return GetAbility<TAbility>() != null;
        }

        public bool HasTag(string tag)
        {
            return tags.Contains(tag);
        }
    }
}
