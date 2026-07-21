using SimpleSurvival.Building;
using UnityEngine;

namespace SimpleSurvival.Items
{
    [CreateAssetMenu(menuName = "Simple Survival/Abilities/Placeable Ability", fileName = "NewPlaceableAbility")]
    public sealed class PlaceableAbility : ItemAbility
    {
        [SerializeField] private BuildingData buildingData;

        public BuildingData BuildingData => buildingData;

        public override string AbilityName => "Placeable";
    }
}