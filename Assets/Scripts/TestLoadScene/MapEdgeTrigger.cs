using UnityEngine;
using UnityEngine.Rendering;

namespace SimpleSurvival.World
{
    [RequireComponent(typeof(Collider))]
    public class MapEdgeTrigger : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Material zoneMaterial;
        [SerializeField] private Color zoneColor = new Color(0.2f, 1f, 0.3f, 0.35f);
        [SerializeField] private float pulseSpeed = 2f;
        [SerializeField] private float pulseAmount = 0.15f;
        [SerializeField] private float heightOffset = 0.02f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private MeshRenderer zoneRenderer;
        private MaterialPropertyBlock propertyBlock;

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void Awake()
        {
            BuildZoneVisual();
        }

        private void Update()
        {
            if (zoneRenderer == null) return;

            float pulse = Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            Color current = zoneColor;
            current.a = Mathf.Clamp01(zoneColor.a + pulse);

            propertyBlock.SetColor(BaseColorId, current);
            zoneRenderer.SetPropertyBlock(propertyBlock);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (WorldMapUI.Instance == null) return;

            WorldMapUI.Instance.Open();
        }

        private void BuildZoneVisual()
        {
            BoxCollider box = GetComponent<Collider>() as BoxCollider;
            if (box == null || zoneMaterial == null) return;

            GameObject zone = new GameObject("TeleportZoneVisual");
            zone.transform.SetParent(transform, false);
            zone.transform.localPosition = box.center + Vector3.up * heightOffset;
            zone.transform.localRotation = Quaternion.identity;
            zone.transform.localScale = new Vector3(box.size.x, 1f, box.size.z);

            MeshFilter filter = zone.AddComponent<MeshFilter>();
            filter.mesh = BuildQuadMesh();

            zoneRenderer = zone.AddComponent<MeshRenderer>();
            zoneRenderer.sharedMaterial = zoneMaterial;
            zoneRenderer.shadowCastingMode = ShadowCastingMode.Off;
            zoneRenderer.receiveShadows = false;

            propertyBlock = new MaterialPropertyBlock();
        }

        private static Mesh BuildQuadMesh()
        {
            Mesh mesh = new Mesh();

            mesh.vertices = new[]
            {
                new Vector3(-0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f),
                new Vector3(-0.5f, 0f, 0.5f)
            };

            mesh.uv = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(0f, 1f)
            };

            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}