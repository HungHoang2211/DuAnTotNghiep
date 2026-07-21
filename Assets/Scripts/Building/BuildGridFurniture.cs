using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class BuildGridFurniture : BuildGrid
    {
        private readonly BuildGridFloor floorGrid;

        public BuildGridFurniture(int cellSize, int gridSizeX, int gridSizeZ, BuildGridFloor floorGrid)
        {
            CellSize = cellSize;
            GridSizeX = gridSizeX;
            GridSizeZ = gridSizeZ;
            this.floorGrid = floorGrid;
        }

        public override Vector3 GetGridCellPosition(BuildCellCoords coords)
        {
            float y = floorGrid.ContainsElement(coords) ? 0.2f : 0f;
            return new Vector3(
                (coords.X - GridSizeX / 2f + 0.5f) * CellSize,
                y,
                (coords.Z - GridSizeZ / 2f + 0.5f) * CellSize);
        }

        public override Quaternion GetGridCellRotation(BuildCellCoords coords)
        {
            return Quaternion.identity;
        }

        public bool CheckAvailable(BuildCellCoords coords, FloorRequirement requirement, BuildCellCoords? ignoreCoords = null)
        {
            if (!InBounds(coords)) return false;
            if (ignoreCoords.HasValue && Key(coords) == Key(ignoreCoords.Value)) return true;
            if (ContainsElement(coords)) return false;

            bool hasFloor = floorGrid.ContainsElement(coords);
            return requirement == FloorRequirement.RequiresFloor ? hasFloor : !hasFloor;
        }
    }
}