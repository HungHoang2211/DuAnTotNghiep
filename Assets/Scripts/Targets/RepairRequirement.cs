using System;
using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Targets
{
    [Serializable]
    public sealed class RepairRequirement
    {
        [SerializeField] private ItemData itemData;
        [SerializeField] private int quantity = 1;

        public ItemData ItemData => itemData;
        public int Quantity => quantity;
    }
}