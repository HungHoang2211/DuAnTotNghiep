using UnityEngine;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Quests
{
    public sealed class QuestItemHighlight : MonoBehaviour
    {
        [SerializeField] private GameObject highlightVisual;

        private PickupTarget _pickupTarget;
        private HarvestTarget _harvestTarget;
        private bool _subscribed;

        private void Awake()
        {
            _pickupTarget = GetComponent<PickupTarget>();
            _harvestTarget = GetComponent<HarvestTarget>();
        }

        private void OnEnable()
        {
            TrySubscribe();
            Refresh();
        }

        private void Start()
        {
            TrySubscribe();
            Refresh();
        }

        private void OnDisable()
        {
            if (QuestHighlightManager.Instance != null)
                QuestHighlightManager.Instance.OnHighlightChanged -= Refresh;
            _subscribed = false;
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (QuestHighlightManager.Instance == null) return;
            QuestHighlightManager.Instance.OnHighlightChanged += Refresh;
            _subscribed = true;
        }

        private void Refresh()
        {
            if (highlightVisual == null) return;

            QuestHighlightManager manager = QuestHighlightManager.Instance;
            bool shouldShow = false;

            if (manager != null)
            {
                if (_harvestTarget != null && manager.IsItemHarvestHighlighted(_harvestTarget.ItemData))
                    shouldShow = true;

                if (!shouldShow && _pickupTarget != null)
                {
                    foreach (var entry in _pickupTarget.Items)
                    {
                        if (manager.IsItemPickupHighlighted(entry.itemData))
                        {
                            shouldShow = true;
                            break;
                        }
                    }
                }
            }

            highlightVisual.SetActive(shouldShow);
        }
    }
}