using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Building
{
    [CreateAssetMenu(menuName = "Simple Survival/Building Database", fileName = "BuildingDatabase")]
    public sealed class BuildingDatabase : ScriptableObject
    {
        [SerializeField] private List<BuildingData> buildings = new List<BuildingData>();

        private Dictionary<string, BuildingData> buildingsById;

        public IReadOnlyList<BuildingData> Buildings => buildings;

        public bool TryGet(string buildingId, out BuildingData building)
        {
            EnsureLookup();
            return buildingsById.TryGetValue(buildingId, out building);
        }

        public void SetBuildings(IEnumerable<BuildingData> source)
        {
            buildings = new List<BuildingData>(source);
            BuildLookup();
        }

        private void OnEnable()
        {
            BuildLookup();
        }

        private void EnsureLookup()
        {
            if (buildingsById == null)
                BuildLookup();
        }

        private void BuildLookup()
        {
            buildingsById = new Dictionary<string, BuildingData>();
            foreach (BuildingData building in buildings)
                Register(building);
        }

        private void Register(BuildingData building)
        {
            if (building == null) return;
            if (string.IsNullOrWhiteSpace(building.BuildingId)) return;
            if (buildingsById.ContainsKey(building.BuildingId)) return;

            buildingsById.Add(building.BuildingId, building);
        }
    }
}