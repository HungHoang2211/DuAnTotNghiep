using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class BuildGridFloor : BuildGrid
    {
        public BuildGridFloor(int cellSize, int gridSizeX, int gridSizeZ)
        {
            CellSize = cellSize;
            GridSizeX = gridSizeX;
            GridSizeZ = gridSizeZ;
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
            return Quaternion.identity;
        }

        public override BuildCellCoords GetGridCellCoords(Vector3 worldPosition)
        {
            int x = Mathf.RoundToInt(worldPosition.x / CellSize + GridSizeX / 2f - 0.5f);
            int z = Mathf.RoundToInt(worldPosition.z / CellSize + GridSizeZ / 2f - 0.5f);
            return new BuildCellCoords(x, z);
        }

        public override bool CheckAvailable(BuildCellCoords coords, BuildingData buildingData, BuildCellCoords? ignoreCoords = null)
        {
            if (!InBounds(coords)) return false;
            if (ignoreCoords.HasValue && Key(coords) == Key(ignoreCoords.Value)) return true;
            return !ContainsElement(coords);
        }
    }
}