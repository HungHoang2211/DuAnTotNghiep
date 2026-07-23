using System.Collections.Generic;
using SimpleSurvival.Items;
using UnityEngine;

namespace SimpleSurvival.Building
{
    [CreateAssetMenu(menuName = "Simple Survival/Building Data", fileName = "NewBuilding")]
    public sealed class BuildingData : ScriptableObject
    {
        public const int MaxCostIngredients = 2;

        [System.Serializable]
        public sealed class Ingredient
        {
            [SerializeField] private ItemData item;
            [SerializeField] private int amount = 1;

            public ItemData Item => item;
            public int Amount => amount;
        }

        [Header("Identity")]
        [SerializeField] private string buildingId;
        [SerializeField] private string displayName;
        [TextArea][SerializeField] private string description;
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject prefab;

        [Header("Structure")]
        [SerializeField] private StructureType structureType;
        [SerializeField] private int footprintSizeX = 1;
        [SerializeField] private int footprintSizeZ = 1;
        [Tooltip("Chỉ áp dụng cho Furniture.")]
        [SerializeField] private FloorRequirement floorRequirement = FloorRequirement.RequiresGround;

        [Header("Cost")]
        [Tooltip("Chỉ áp dụng cho Floor/Wall. Furniture không dùng trường này.")]
        [SerializeField] private List<Ingredient> directCost = new List<Ingredient>();

        [Header("Upgrade")]
        [Tooltip("0 = tier thấp nhất. Dùng để so sánh Wall có được nâng vượt Floor không.")]
        [SerializeField] private int tierIndex;
        [SerializeField] private BuildingData nextTier;

        public string BuildingId => buildingId;
        public string DisplayName => displayName;
        public string Description => description;
        public Sprite Icon => icon;
        public GameObject Prefab => prefab;
        public StructureType StructureType => structureType;
        public int FootprintSizeX => footprintSizeX;
        public int FootprintSizeZ => footprintSizeZ;
        public FloorRequirement FloorRequirement => floorRequirement;
        public IReadOnlyList<Ingredient> DirectCost => directCost;
        public int TierIndex => tierIndex;
        public BuildingData NextTier => nextTier;

        private void OnValidate()
        {
            if (directCost.Count > MaxCostIngredients)
                directCost.RemoveRange(MaxCostIngredients, directCost.Count - MaxCostIngredients);

            if (footprintSizeX < 1) footprintSizeX = 1;
            if (footprintSizeZ < 1) footprintSizeZ = 1;
            if (tierIndex < 0) tierIndex = 0;
        }
    }
}