using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;

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

        [Header("Pickup Effect")]
        [SerializeField] private Renderer mainRenderer;

        [Header("Persistence")]
        [SerializeField] private bool persistAcrossSessions = false;
        [SerializeField] private string pickupId;

        public IReadOnlyList<PickupItemEntry> Items => items;
        public string PickupId => pickupId;

        public override TargetType Type => TargetType.Pickup;

        private bool _pickedUp;

        private void Awake()
        {
            if (persistAcrossSessions)
                HarvestSaveRegistry.Instance?.InitializePersistentPickup(this);
        }

        public override bool CanBeTargeted()
        {
            return isActiveAndEnabled && !_pickedUp && items != null && items.Count > 0;
        }

        public void OnPickedUp()
        {
            if (_pickedUp) return;
            _pickedUp = true;

            FireOnDestroyed();

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
            if (distanceCollider != null) distanceCollider.enabled = false;

            if (mainRenderer != null) mainRenderer.enabled = false;

            if (persistAcrossSessions)
                HarvestSaveRegistry.Instance?.NotifyPickupConsumed(pickupId);
        }
    }
}