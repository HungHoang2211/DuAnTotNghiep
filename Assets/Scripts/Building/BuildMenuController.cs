using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class BuildMenuController : MonoBehaviour
    {
        [SerializeField] private BuildListItemUi itemPrefab;
        [SerializeField] private Transform contentParent;

        private readonly List<BuildListItemUi> spawnedItems = new List<BuildListItemUi>();
        private BuildListItemUi selectedItem;
        private Action<BuildingData> onBuildingSelected;
        private bool isPopulated;

        public void Populate(IReadOnlyList<BuildingData> buildings, Action<BuildingData> onSelected)
        {
            onBuildingSelected = onSelected;

            if (isPopulated) return;
            isPopulated = true;

            foreach (BuildingData building in buildings)
            {
                if (building.TierIndex != 0) continue;

                BuildListItemUi item = Instantiate(itemPrefab, contentParent);
                item.Init(building, HandleItemClicked);
                spawnedItems.Add(item);
            }
        }

        public void ClearSelection()
        {
            if (selectedItem != null)
                selectedItem.SetSelected(false);
            selectedItem = null;
        }

        private void HandleItemClicked(BuildListItemUi item)
        {
            if (selectedItem != null)
                selectedItem.SetSelected(false);

            selectedItem = item;
            selectedItem.SetSelected(true);

            onBuildingSelected?.Invoke(item.Building);
        }
    }
}