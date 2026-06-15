using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Stats;
using SimpleSurvival.Core;

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

        [Header("Pool")]
        [Tooltip("Delay (giây) trước khi return về pool sau khi depleted. Đủ thời gian cho animation/physics đổ.")]
        [SerializeField] private float despawnDelay = 5f;

        [Header("Depleted Effect — Rigidbody (optional, cho cây đổ)")]
        [Tooltip("Rigidbody của target. isKinematic=true ban đầu, set false khi depleted để physics activate.")]
        [SerializeField] private Rigidbody fallRigidbody;

        [Tooltip("Torque random apply lên rigidbody khi depleted. Tweak để cây đổ realistic.")]
        [SerializeField] private float fallTorqueAmount = 50f;

        [Header("Depleted Effect — Animator (optional, cho đá vỡ)")]
        [Tooltip("Animator cho animation vỡ. Trigger animation khi depleted.")]
        [SerializeField] private Animator brokenAnimator;

        [Tooltip("Tên trigger animation vỡ trên Animator.")]
        [SerializeField] private string breakTrigger = "Break";

        [Header("Depleted Effect — Fracture Swap (optional)")]
        [Tooltip("GameObject mảnh vỡ (fracture). Enable khi depleted, swap với mesh chính.")]
        [SerializeField] private GameObject fractureObject;

        [Tooltip("Renderer mesh chính. Disable khi enable fractureObject.")]
        [SerializeField] private Renderer mainRenderer;

        private HarvestStats _stats;

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
            ObjectPool.Instance.ReturnDelayed(gameObject, despawnDelay);
        }

        private void DisableTargetability()
        {
            var col = GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }

        private void PlayDepletedEffect()
        {
            if (fallRigidbody != null)
            {
                fallRigidbody.isKinematic = false;
                if (fallTorqueAmount > 0f)
                {
                    Vector3 torque = Random.insideUnitSphere * fallTorqueAmount;
                    torque.y = 0f;
                    fallRigidbody.AddTorque(torque, ForceMode.Impulse);
                }
            }

            if (fractureObject != null)
            {
                fractureObject.SetActive(true);
                if (mainRenderer != null)
                    mainRenderer.enabled = false;
            }

            if (brokenAnimator != null && !string.IsNullOrEmpty(breakTrigger))
            {
                brokenAnimator.SetTrigger(breakTrigger);
            }
        }

        protected override void OnSpawnFromPool()
        {
            base.OnSpawnFromPool();

            var col = GetComponent<Collider>();
            if (col != null) col.enabled = true;

            if (fallRigidbody != null)
            {
                fallRigidbody.isKinematic = true;
                fallRigidbody.linearVelocity = Vector3.zero;
                fallRigidbody.angularVelocity = Vector3.zero;
            }

            if (fractureObject != null) fractureObject.SetActive(false);
            if (mainRenderer != null) mainRenderer.enabled = true;
        }
    }
}