using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Targets
{
    [RequireComponent(typeof(HarvestStats))]
    public class HarvestTarget : TargetableBase
    {
        [Header("Drop")]
        [SerializeField] private ItemData itemData;
        [SerializeField] private int minQuantity = 1;
        [SerializeField] private int maxQuantity = 1;

        [Header("Tool Requirement")]
        [SerializeField] private ToolType requiredTool = ToolType.Axe;

        [Header("Despawn Timing")]
        [SerializeField] private float hideVisualDelay = 5f;

        [Header("Manual Fall (cho cây)")]
        [SerializeField] private Transform fallTransform;
        [SerializeField] private float fallDuration = 1.5f;
        [SerializeField] private float fallEndAngle = 85f;

        [Header("Fracture Swap (cho đá vỡ)")]
        [SerializeField] private GameObject fractureObject;
        [SerializeField] private Renderer mainRenderer;

        [Header("Dissolve Material (optional)")]
        [SerializeField] private Renderer dissolveRenderer;
        [SerializeField] private float dissolveDuration = 1.5f;
        [SerializeField] private float dissolveStartDelay = 1.5f;

        private static readonly int DissolveProp = Shader.PropertyToID("_Dissolve");

        private HarvestStats _stats;
        private Material _dissolveMaterial;

        public ItemData ItemData => itemData;
        public ToolType RequiredTool => requiredTool;
        public HarvestStats Stats => _stats;
        public int RollQuantity() => Random.Range(minQuantity, maxQuantity + 1);

        public override TargetType Type => TargetType.Harvest;

        private void Awake()
        {
            _stats = GetComponent<HarvestStats>();
            if (_stats != null)
                _stats.OnDepleted += HandleDepleted;
        }

        protected override void OnDestroy()
        {
            if (_stats != null)
                _stats.OnDepleted -= HandleDepleted;
            base.OnDestroy();
        }

        public override bool CanBeTargeted()
        {
            return isActiveAndEnabled && itemData != null && _stats != null && !_stats.IsDepleted;
        }

        private void HandleDepleted()
        {
            Debug.Log($"[Harvest] Depleted called. fractureObject={fractureObject?.name ?? "NULL"}, mainRenderer={mainRenderer?.name ?? "NULL"}");
            FireOnDestroyed();
            DisableTargetability();
            PlayDepletedEffect();
            Invoke(nameof(HideVisual), hideVisualDelay);
        }

        private void DisableTargetability()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;

            if (distanceCollider != null) distanceCollider.enabled = false;
            if (navObstacle != null) navObstacle.enabled = false;

            if (fallTransform != null)
            {
                var rbCollider = fallTransform.GetComponent<Collider>();
                if (rbCollider != null) rbCollider.enabled = false;
            }
        }

        private void PlayDepletedEffect()
        {
            if (fallTransform != null)
            {
                StartCoroutine(ManualFallRoutine(fallTransform));
            }

            if (fractureObject != null)
            {
                Debug.Log($"[Harvest] Activating fracture: {fractureObject.name}, was active: {fractureObject.activeSelf}");
                fractureObject.SetActive(true);
                Debug.Log($"[Harvest] After SetActive: {fractureObject.activeSelf}, activeInHierarchy: {fractureObject.activeInHierarchy}");

                if (mainRenderer != null)
                {
                    Debug.Log($"[Harvest] Disabling mainRenderer: {mainRenderer.name}, was enabled: {mainRenderer.enabled}");
                    mainRenderer.enabled = false;
                    Debug.Log($"[Harvest] After disable: {mainRenderer.enabled}");
                }
            }

            if (dissolveRenderer != null)
            {
                _dissolveMaterial = dissolveRenderer.material;
                Invoke(nameof(StartDissolve), dissolveStartDelay);
            }
        }

        private System.Collections.IEnumerator ManualFallRoutine(Transform target)
        {
            if (target == null) yield break;

            float randomY = Random.Range(0f, 360f);
            Vector3 fallDirection = Quaternion.Euler(0f, randomY, 0f) * Vector3.forward;
            Vector3 fallAxis = Vector3.Cross(Vector3.up, fallDirection).normalized;

            Quaternion startRot = target.rotation;
            Quaternion endRot = Quaternion.AngleAxis(fallEndAngle, fallAxis) * startRot;

            float elapsed = 0f;
            while (elapsed < fallDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fallDuration);
                t = t * t;
                target.rotation = Quaternion.Slerp(startRot, endRot, t);
                yield return null;
            }

            target.rotation = endRot;
        }

        private void StartDissolve()
        {
            StartCoroutine(DissolveRoutine());
        }

        private System.Collections.IEnumerator DissolveRoutine()
        {
            float elapsed = 0f;
            while (elapsed < dissolveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / dissolveDuration);
                if (_dissolveMaterial != null)
                    _dissolveMaterial.SetFloat(DissolveProp, t);
                yield return null;
            }
        }

        private void HideVisual()
        {
            if (mainRenderer != null) mainRenderer.enabled = false;
            if (dissolveRenderer != null && dissolveRenderer != mainRenderer)
                dissolveRenderer.enabled = false;
            if (fractureObject != null) fractureObject.SetActive(false);
        }
    }
}