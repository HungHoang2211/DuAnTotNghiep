using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Targets
{
    public sealed class RepairableTower : TargetableBase, IUnlockable
    {
        [SerializeField] private List<RepairRequirement> requiredItems = new List<RepairRequirement>();
        [SerializeField] private float unlockDuration = 3f;
        [SerializeField] private BaseStats bigOneStats;

        [Header("Quest")]
        [SerializeField] private string towerId;

        private bool _isUnlocked;
        private bool _repaired;

        public override TargetType Type => TargetType.Repairable;
        public IReadOnlyList<RepairRequirement> RequiredItems => requiredItems;
        public float UnlockDuration => unlockDuration;
        public bool IsRepaired => _repaired;
        public string TowerId => towerId;

        public event Action<RepairableTower> OnRepaired;

        public override bool CanBeTargeted()
        {
            if (_repaired) return false;
            if (bigOneStats != null && !bigOneStats.IsDead) return false;
            return base.CanBeTargeted();
        }

        public void MarkUnlocked()
        {
            _isUnlocked = true;
        }

        public void Open()
        {
            if (_repaired) return;
            _repaired = true;
            OnRepaired?.Invoke(this);
        }
    }
}