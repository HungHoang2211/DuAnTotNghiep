using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Targets
{
    [Serializable]
    public class PickupItemEntry
    {
        public ItemData itemData;
        public int quantity = 1;
    }

    public class PickupTarget : TargetableBase
    {
        [Header("Item Drops")]
        [SerializeField] private List<PickupItemEntry> items = new List<PickupItemEntry>();

        public IReadOnlyList<PickupItemEntry> Items => items;

        public override TargetType Type => TargetType.Pickup;

        public override bool CanBeTargeted()
        {
            return isActiveAndEnabled && items != null && items.Count > 0;
        }
    }
}