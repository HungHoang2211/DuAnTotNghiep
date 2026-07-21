using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class PlacedStructureView : MonoBehaviour
    {
        [SerializeField] private Renderer[] renderers;

        private Material[] originalMaterials;

        public BuildingData BuildingData { get; private set; }
        public BuildCellCoords Coords { get; private set; }
        public int RotationIndex { get; private set; }

        public void Init(BuildingData buildingData, BuildCellCoords coords, int rotationIndex)
        {
            BuildingData = buildingData;
            Coords = coords;
            RotationIndex = rotationIndex;

            if (renderers == null || renderers.Length == 0)
                renderers = GetComponentsInChildren<Renderer>();

            originalMaterials = new Material[renderers.Length];
            for (int i = 0; i < renderers.Length; i++)
                originalMaterials[i] = renderers[i].sharedMaterial;
        }

        public void SetCoords(BuildCellCoords coords)
        {
            Coords = coords;
        }

        public void SetRotationIndex(int rotationIndex)
        {
            RotationIndex = rotationIndex;
        }

        public void SetPreviewMaterial(Material material)
        {
            foreach (Renderer renderer in renderers)
                renderer.sharedMaterial = material;
        }

        public void ClearPreviewMaterial()
        {
            for (int i = 0; i < renderers.Length; i++)
                renderers[i].sharedMaterial = originalMaterials[i];
        }
    }
}