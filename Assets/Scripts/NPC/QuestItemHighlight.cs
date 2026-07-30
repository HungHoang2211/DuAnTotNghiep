using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Quests
{
    public sealed class QuestItemHighlight : MonoBehaviour
    {
        [SerializeField] private GameObject highlightVisual;

        private PickupTarget _pickupTarget;
        private HarvestTarget _harvestTarget;
        private bool _registered;
        private bool _isActive;

        public Transform HighlightTransform => transform;

        private void Awake()
        {
            _pickupTarget = GetComponent<PickupTarget>();
            _harvestTarget = GetComponent<HarvestTarget>();
        }

        private void OnEnable()
        {
            TryRegister();
        }

        private void Start()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            if (QuestHighlightManager.Instance != null)
                QuestHighlightManager.Instance.UnregisterItemCandidate(this);
            _registered = false;
            SetHighlighted(false);
        }

        private void TryRegister()
        {
            if (_registered) return;
            if (QuestHighlightManager.Instance == null) return;
            QuestHighlightManager.Instance.RegisterItemCandidate(this);
            _registered = true;
        }

        public bool MatchesPickup(ItemData item)
        {
            if (_pickupTarget == null || item == null) return false;
            foreach (var entry in _pickupTarget.Items)
            {
                if (entry.itemData == item) return true;
            }
            return false;
        }

        public bool MatchesHarvest(ItemData item)
        {
            return _harvestTarget != null && item != null && _harvestTarget.ItemData == item;
        }

        public void SetHighlighted(bool value)
        {
            if (_isActive == value) return;
            _isActive = value;

            if (highlightVisual != null)
                highlightVisual.SetActive(value);
        }
    }
}