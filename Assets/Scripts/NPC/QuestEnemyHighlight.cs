using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.Quests
{
    [RequireComponent(typeof(EnemyStats))]
    public sealed class QuestEnemyHighlight : MonoBehaviour
    {
        [SerializeField] private GameObject highlightVisual;

        private EnemyStats _stats;
        private bool _isActive;
        private bool _registered;
        private bool _isDead;

        public Transform HighlightTransform => transform;
        public EnemyStatsConfig EnemyConfig => _stats != null ? _stats.EnemyConfig : null;
        public bool IsAlive => !_isDead;

        private void Awake()
        {
            _stats = GetComponent<EnemyStats>();
        }

        private void OnEnable()
        {
            _isDead = false;
            if (_stats != null) _stats.OnDeath += HandleDeath;
            TryRegister();
        }

        private void Start()
        {
            TryRegister();
        }

        private void OnDisable()
        {
            if (_stats != null) _stats.OnDeath -= HandleDeath;

            if (QuestHighlightManager.Instance != null)
                QuestHighlightManager.Instance.UnregisterEnemyCandidate(this);
            _registered = false;
            SetHighlighted(false);
        }

        private void TryRegister()
        {
            if (_registered) return;
            if (QuestHighlightManager.Instance == null) return;
            QuestHighlightManager.Instance.RegisterEnemyCandidate(this);
            _registered = true;
        }

        private void HandleDeath(GameObject source)
        {
            _isDead = true;
            SetHighlighted(false);
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