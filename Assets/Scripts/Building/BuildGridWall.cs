using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class BuildGridWall : BuildGrid
    {
        private readonly BuildGridFloor floorGrid;
        private readonly HashSet<int> availableFromFloor = new HashSet<int>();

        public BuildGridWall(BuildGridFloor floorGrid)
        {
            this.floorGrid = floorGrid;
            CellSize = 1;
            GridSizeX = floorGrid.GridSizeX * 2 + 1;
            GridSizeZ = floorGrid.GridSizeZ * 2 + 1;

            floorGrid.OnChange += RebuildAvailableFromFloor;
            RebuildAvailableFromFloor();
        }

        public override Vector3 GetGridCellPosition(BuildCellCoords coords)
        {
            return new Vector3(
                (coords.X - GridSizeX / 2f + 0.5f) * CellSize,
                0f,
                (coords.Z - GridSizeZ / 2f + 0.5f) * CellSize);
        }

        public override Quaternion GetGridCellRotation(BuildCellCoords coords)
        {
            float yaw = (1 - (coords.Z + 1) % 2) * 90f;
            return Quaternion.Euler(0f, yaw, 0f);
        }

        public override BuildCellCoords GetGridCellCoords(Vector3 worldPosition)
        {
            BuildCellCoords floorCoords = floorGrid.GetGridCellCoords(worldPosition);
            BuildCellCoords best = GetWallCoords(floorCoords, 0);
            float bestDistance = Vector3.Distance(worldPosition, GetGridCellPosition(best));

            for (int side = 1; side < 4; side++)
            {
                BuildCellCoords candidate = GetWallCoords(floorCoords, side);
                float distance = Vector3.Distance(worldPosition, GetGridCellPosition(candidate));
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            return best;
        }

        public override bool CheckAvailable(BuildCellCoords coords, BuildingData buildingData, BuildCellCoords? ignoreCoords = null)
        {
            if (!InBounds(coords)) return false;
            if (ignoreCoords.HasValue && Key(coords) == Key(ignoreCoords.Value)) return true;
            if (ContainsElement(coords)) return false;
            return availableFromFloor.Contains(Key(coords));
        }

        public BuildCellCoords GetWallCoords(BuildCellCoords floorCoords, int side)
        {
            switch (side)
            {
                case 0: return new BuildCellCoords(floorCoords.X * 2 + 1, floorCoords.Z * 2 + 2);
                case 1: return new BuildCellCoords(floorCoords.X * 2 + 2, floorCoords.Z * 2 + 1);
                case 2: return new BuildCellCoords(floorCoords.X * 2 + 1, floorCoords.Z * 2);
                default: return new BuildCellCoords(floorCoords.X * 2, floorCoords.Z * 2 + 1);
            }
        }

        private void RebuildAvailableFromFloor()
        {
            availableFromFloor.Clear();
            foreach (int floorKey in floorGrid.OccupiedKeys())
            {
                BuildCellCoords floorCoords = floorGrid.KeyCoords(floorKey);
                for (int side = 0; side < 4; side++)
                {
                    BuildCellCoords wallCoords = GetWallCoords(floorCoords, side);
                    availableFromFloor.Add(Key(wallCoords));
                }
            }
        }
    }
}