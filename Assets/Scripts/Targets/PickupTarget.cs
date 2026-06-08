using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Targets
{
    public class PickupTarget : TargetableBase
    {
        [Header("Item Drop")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity = 1;

        public ItemData ItemData => itemData;
        public int Quantity => quantity;

        public override TargetType Type => TargetType.Pickup;

        public override bool CanBeTargeted()
        {
            return isActiveAndEnabled && itemData != null;
        }
    }
}