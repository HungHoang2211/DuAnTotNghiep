using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Item Data", fileName = "NewItem")]
    public sealed class ItemData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private Sprite icon;
        [TextArea][SerializeField] private string description;

        [Header("Stacking")]
        [SerializeField] private bool isStackable;
        [SerializeField] private int maxStack = 1;

        [Header("Durability")]
        [Tooltip("0 means this item never wears out (resources, consumables, backpacks).")]
        [SerializeField] private int maxDurability;

        [Header("Tags")]
        [Tooltip("Tick every role this item fills. Equipment slots accept only items carrying the matching tag.")]
        [SerializeField] private ItemTag tags;

        [Header("Abilities")]
        [Tooltip("Drag ability assets here. An item with no abilities is a plain resource.")]
        [SerializeField] private List<ItemAbility> abilities = new List<ItemAbility>();

        public string ItemId => itemId;
        public string ItemName => itemName;
        public Sprite Icon => icon;
        public string Description => description;
        public bool IsStackable => isStackable;
        public int MaxStack => isStackable ? maxStack : 1;
        public int MaxDurability => maxDurability;
        public bool IsDurable => maxDurability > 0;
        public TAbility GetAbility<TAbility>() where TAbility : ItemAbility
        {
            foreach (ItemAbility ability in abilities)
            {
                if (ability is TAbility match)
                    return match;
            }
            return null;
        }

        public bool HasAbility<TAbility>() where TAbility : ItemAbility
        {
            return GetAbility<TAbility>() != null;
        }

        public bool HasTag(ItemTag tag)
        {
            return (tags & tag) == tag;
        }
    }
}