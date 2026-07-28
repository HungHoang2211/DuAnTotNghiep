using UnityEngine;
using SimpleSurvival.AI;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Quests
{
    [RequireComponent(typeof(EnemyStats))]
    public sealed class QuestEnemyHighlight : MonoBehaviour
    {
        [SerializeField] private GameObject highlightVisual;

        private EnemyStats _stats;
        private BaseEnemyController _controller;
        private bool _isActive;
        private bool _subscribed;

        private void Awake()
        {
            _stats = GetComponent<EnemyStats>();
            _controller = GetComponent<BaseEnemyController>();
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

            SetActive(false);
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
            QuestHighlightManager manager = QuestHighlightManager.Instance;
            bool shouldShow = manager != null && _stats != null && manager.IsEnemyHighlighted(_stats.EnemyConfig);
            SetActive(shouldShow);
        }

        private void SetActive(bool value)
        {
            if (_isActive == value) return;
            _isActive = value;

            if (highlightVisual != null)
                highlightVisual.SetActive(value);

            if (_controller != null)
                _controller.SetQuestLocked(value);
        }
    }
}