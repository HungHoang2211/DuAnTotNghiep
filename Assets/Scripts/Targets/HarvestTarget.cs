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
        [Tooltip("Delay (giây) trước khi hide visual sau khi depleted. Nên >= dissolveStartDelay + dissolveDuration.")]
        [SerializeField] private float hideVisualDelay = 3.5f;

        [Header("Manual Fall (cho cây)")]
        [Tooltip("Transform của mesh cây (Spruce_0). Sẽ rotate quanh pivot của transform này khi cây ngã.")]
        [SerializeField] private Transform fallTransform;

        [Tooltip("Thời gian cây ngã hoàn toàn (giây).")]
        [SerializeField] private float fallDuration = 1.5f;

        [Tooltip("Góc ngã cuối cùng (độ). 85-90 = nằm ngang.")]
        [SerializeField] private float fallEndAngle = 85f;

        [Header("Depleted Effect — Animator (optional, cho đá vỡ)")]
        [Tooltip("Animator cho animation vỡ. Trigger animation khi depleted.")]
        [SerializeField] private Animator brokenAnimator;

        [Tooltip("Tên trigger animation vỡ trên Animator.")]
        [SerializeField] private string breakTrigger = "Break";

        [Header("Depleted Effect — Fracture Swap (optional)")]
        [Tooltip("GameObject mảnh vỡ (fracture). Enable khi depleted, swap với mesh chính.")]
        [SerializeField] private GameObject fractureObject;

        [Tooltip("Renderer mesh chính. Disable khi enable fractureObject HOẶC khi hideVisualDelay timeout.")]
        [SerializeField] private Renderer mainRenderer;

        [Header("Depleted Effect — Dissolve Material (optional)")]
        [Tooltip("Renderer có material với shader SimpleSurvival/FoliageAlpha (có _Dissolve property).")]
        [SerializeField] private Renderer dissolveRenderer;

        [Tooltip("Thời gian dissolve animation (giây).")]
        [SerializeField] private float dissolveDuration = 1.5f;

        [Tooltip("Delay (giây) sau khi cây bị chặt trước khi bắt đầu dissolve. Để cây ngã xong trước khi tan biến.")]
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
            // Cây: manual tween rotation (không dùng physics)
            if (fallTransform != null)
            {
                StartCoroutine(ManualFallRoutine(fallTransform));
            }

            // Đá/quặng: fracture swap
            if (fractureObject != null)
            {
                fractureObject.SetActive(true);
                if (mainRenderer != null)
                    mainRenderer.enabled = false;
            }

            // Đá/quặng: animator trigger
            if (brokenAnimator != null && !string.IsNullOrEmpty(breakTrigger))
            {
                brokenAnimator.SetTrigger(breakTrigger);
            }

            // Dissolve shader animation
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