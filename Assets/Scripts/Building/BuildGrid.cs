using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Building
{
    public abstract class BuildGrid
    {
        protected readonly Dictionary<int, PlacedStructureView> grid = new Dictionary<int, PlacedStructureView>();

        public int CellSize { get; protected set; }
        public int GridSizeX { get; protected set; }
        public int GridSizeZ { get; protected set; }

        public event Action OnChange;

        public abstract Vector3 GetGridCellPosition(BuildCellCoords coords);
        public abstract Quaternion GetGridCellRotation(BuildCellCoords coords);
        public abstract BuildCellCoords GetGridCellCoords(Vector3 worldPosition);
        public abstract bool CheckAvailable(BuildCellCoords coords, BuildingData buildingData, BuildCellCoords? ignoreCoords = null);

        public IEnumerable<PlacedStructureView> AllElements => grid.Values;
        public int Key(BuildCellCoords coords)
        {
            return coords.X * GridSizeX + coords.Z;
        }

        public BuildCellCoords KeyCoords(int key)
        {
            return new BuildCellCoords(key / GridSizeX, key % GridSizeX);
        }

        public bool InBounds(BuildCellCoords coords)
        {
            return coords.X >= 0 && coords.X < GridSizeX && coords.Z >= 0 && coords.Z < GridSizeZ;
        }

        public bool ContainsElement(BuildCellCoords coords)
        {
            return grid.ContainsKey(Key(coords));
        }

        public PlacedStructureView GetElement(BuildCellCoords coords)
        {
            grid.TryGetValue(Key(coords), out PlacedStructureView view);
            return view;
        }

        public void AddElement(BuildCellCoords coords, PlacedStructureView view)
        {
            grid[Key(coords)] = view;
            RaiseChange();
        }

        public void RemoveElement(BuildCellCoords coords)
        {
            grid.Remove(Key(coords));
            RaiseChange();
        }

        public IEnumerable<int> OccupiedKeys()
        {
            return grid.Keys;
        }

        protected void RaiseChange()
        {
            OnChange?.Invoke();
        }

        public BuildCellCoords? FindFreeCellSpiral(BuildCellCoords start, Func<BuildCellCoords, bool> isAvailable)
        {
            int x = 0;
            int z = 0;
            int dx = 0;
            int dz = -1;
            int steps = Mathf.Max(GridSizeX, GridSizeZ);
            int maxIterations = steps * steps;

            for (int i = 0; i < maxIterations; i++)
            {
                BuildCellCoords candidate = new BuildCellCoords(start.X + x, start.Z + z);
                if (InBounds(candidate) && isAvailable(candidate))
                    return candidate;

                if (x == z || (x < 0 && x == -z) || (x > 0 && x == 1 - z))
                {
                    int temp = dx;
                    dx = -dz;
                    dz = temp;
                }
                x += dx;
                z += dz;
            }

            return null;
        }
    }
}