using UnityEngine;
using TMPro;
using SimpleSurvival.Stats;
using SimpleSurvival.Targets;

namespace SimpleSurvival.UI.HealthBar
{
    public class EnemyHealthUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerTargetChecker targetChecker;
        [SerializeField] private GameObject panelRoot;

        [Header("Bar")]
        [SerializeField] private BarAnimation hpBar;

        [Header("Labels")]
        [SerializeField] private TMP_Text hpAmountLabel;
        [SerializeField] private TMP_Text enemyNameLabel;

        private EnemyStats _currentStats;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (targetChecker != null)
                targetChecker.OnEnemyChanged += HandleEnemyChanged;

            if (targetChecker != null)
                HandleEnemyChanged(targetChecker.CurrentEnemy);
        }

        private void OnDisable()
        {
            if (targetChecker != null)
                targetChecker.OnEnemyChanged -= HandleEnemyChanged;

            UnbindCurrent();
        }

        private void HandleEnemyChanged(ITargetable target)
        {
            UnbindCurrent();

            if (target == null)
            {
                panelRoot.SetActive(false);
                return;
            }

            MonoBehaviour mb = target as MonoBehaviour;
            if (mb == null)
            {
                panelRoot.SetActive(false);
                return;
            }

            EnemyStats stats = mb.GetComponent<EnemyStats>();
            if (stats == null)
            {
                panelRoot.SetActive(false);
                return;
            }

            BindEnemy(stats);
            Debug.Log($"[EnemyHealthUI] Enemy changed: {(target as MonoBehaviour)?.name ?? "null"}");
        }

        private void BindEnemy(EnemyStats stats)
        {
            _currentStats = stats;
            stats.OnHPChanged += HandleHPChanged;

            EnemyStatsConfig config = GetConfig(stats);

            if (config != null)
            {
                if (enemyNameLabel != null) enemyNameLabel.text = config.DisplayName;
                if (hpBar != null) hpBar.SetBarColor(config.HPBarColor);
            }

            float percent = stats.HP / stats.MaxHP;
            if (hpBar != null) hpBar.SetValue(percent);
            UpdateLabel(stats.HP);

            panelRoot.SetActive(true);
        }

        private void UnbindCurrent()
        {
            if (_currentStats != null)
            {
                _currentStats.OnHPChanged -= HandleHPChanged;
                _currentStats = null;
            }
        }

        private void HandleHPChanged(float current, float max)
        {
            float percent = current / max;
            if (hpBar != null) hpBar.AnimateValue(percent);
            UpdateLabel(current);
        }

        private void UpdateLabel(float hp)
        {
            if (hpAmountLabel != null)
                hpAmountLabel.text = ((int)hp).ToString();
        }

        private EnemyStatsConfig GetConfig(EnemyStats stats)
        {
            var configField = typeof(BaseStats).GetField("baseConfig",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            return configField?.GetValue(stats) as EnemyStatsConfig;
        }
    }
}