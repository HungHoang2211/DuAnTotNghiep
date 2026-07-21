using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class PlacedStructureView : MonoBehaviour
    {
        [SerializeField] private Transform rotatorTransform;
        [SerializeField] private GameObject selectorRoot;
        [SerializeField] private Renderer[] previewRenderers;

        private Material[] originalMaterials;

        public BuildingData BuildingData { get; private set; }
        public BuildCellCoords Coords { get; private set; }
        public int RotationIndex { get; private set; }

        public void Init(BuildingData buildingData, BuildCellCoords coords, int rotationIndex)
        {
            BuildingData = buildingData;
            Coords = coords;
            RotationIndex = rotationIndex;

            if (previewRenderers == null || previewRenderers.Length == 0)
                previewRenderers = rotatorTransform.GetComponentsInChildren<Renderer>();

            originalMaterials = new Material[previewRenderers.Length];
            for (int i = 0; i < previewRenderers.Length; i++)
                originalMaterials[i] = previewRenderers[i].sharedMaterial;

            SetSelected(false);
        }

        public void SetCoords(BuildCellCoords coords)
        {
            Coords = coords;
        }

        public void SetRotationIndex(int rotationIndex)
        {
            RotationIndex = rotationIndex;
        }

        public void SetWorldTransform(Vector3 position, Quaternion rotation)
        {
            transform.position = position;
            rotatorTransform.rotation = rotation;
        }

        public void SetSelected(bool selected)
        {
            if (selectorRoot != null)
                selectorRoot.SetActive(selected);
        }

        public void SetPreviewMaterial(Material material)
        {
            foreach (Renderer renderer in previewRenderers)
                renderer.sharedMaterial = material;
        }

        public void ClearPreviewMaterial()
        {
            for (int i = 0; i < previewRenderers.Length; i++)
                previewRenderers[i].sharedMaterial = originalMaterials[i];
        }
    }
}