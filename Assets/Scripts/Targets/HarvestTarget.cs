using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Targets
{
    [RequireComponent(typeof(HarvestStats))]
    public class HarvestTarget : TargetableBase
    {
        [Header("Drop")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private int minQuantity = 1;
        [SerializeField] private int maxQuantity = 1;

        [Header("Tool Requirement")]
        [SerializeField] private ToolType requiredTool = ToolType.Axe;

        private HarvestStats _stats;

        public ItemData ItemData => itemData;
        public ToolType RequiredTool => requiredTool;
        public HarvestStats Stats => _stats;
        public int RollQuantity() => Random.Range(minQuantity, maxQuantity + 1);

        public override TargetType Type => TargetType.Harvest;

        private void Awake()
        {
            _stats = GetComponent<HarvestStats>();
        }

        public override bool CanBeTargeted()
        {
            return isActiveAndEnabled && itemData != null && _stats != null && !_stats.IsDepleted;
        }
    }
}