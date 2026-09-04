using UnityEngine;
using TMPro;
using SimpleSurvival.AI;
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

        [Header("NPC Fallback")]
        [SerializeField] private string npcDisplayName = "Emily";
        [SerializeField] private Color npcBarColor = Color.green;

        private EnemyStats _currentStats;
        private NPCEmilyStats _currentNpcStats;

        private void Awake()
        {
            if (panelRoot == null) panelRoot = gameObject;
            panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (targetChecker != null)
                targetChecker.OnEnemyChanged += HandleEnemyChanged;

            NPCEmilyStats.OnInstanceChanged += HandleEmilyInstanceChanged;
            NPCEmilyController.OnEscortStateChanged += HandleEmilyEscortStateChanged;

            HandleEnemyChanged(targetChecker != null ? targetChecker.CurrentEnemy : null);
        }

        private void OnDisable()
        {
            if (targetChecker != null)
                targetChecker.OnEnemyChanged -= HandleEnemyChanged;

            NPCEmilyStats.OnInstanceChanged -= HandleEmilyInstanceChanged;
            NPCEmilyController.OnEscortStateChanged -= HandleEmilyEscortStateChanged;

            UnbindEnemy();
            UnbindNpc();
        }

        private void HandleEnemyChanged(ITargetable target)
        {
            UnbindEnemy();

            MonoBehaviour mb = target as MonoBehaviour;
            EnemyStats stats = mb != null ? mb.GetComponentInParent<EnemyStats>() : null;

            if (stats == null)
            {
                ShowNpcFallback();
                return;
            }

            BindEnemy(stats);
        }

        private void HandleEmilyInstanceChanged(NPCEmilyStats instance)
        {
            if (_currentStats != null) return;
            ShowNpcFallback();
        }

        private void HandleEmilyEscortStateChanged(bool isEscorting)
        {
            if (_currentStats != null) return;
            ShowNpcFallback();
        }

        private void BindEnemy(EnemyStats stats)
        {
            UnbindNpc();

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

        private void ShowNpcFallback()
        {
            UnbindNpc();

            NPCEmilyController controller = NPCEmilyController.Instance;
            NPCEmilyStats instance = NPCEmilyStats.Instance;

            if (controller == null || !controller.IsEscorting || instance == null)
            {
                panelRoot.SetActive(false);
                return;
            }

            _currentNpcStats = instance;
            instance.OnHPChanged += HandleNpcHPChanged;

            if (enemyNameLabel != null) enemyNameLabel.text = npcDisplayName;
            if (hpBar != null) hpBar.SetBarColor(npcBarColor);

            float percent = instance.MaxHP > 0f ? instance.HP / instance.MaxHP : 0f;
            if (hpBar != null) hpBar.SetValue(percent);
            UpdateLabel(instance.HP);

            panelRoot.SetActive(true);
        }

        private void UnbindEnemy()
        {
            if (_currentStats == null) return;
            _currentStats.OnHPChanged -= HandleHPChanged;
            _currentStats = null;
        }

        private void UnbindNpc()
        {
            if (_currentNpcStats == null) return;
            _currentNpcStats.OnHPChanged -= HandleNpcHPChanged;
            _currentNpcStats = null;
        }

        private void HandleHPChanged(float current, float max)
        {
            float percent = current / max;
            if (hpBar != null) hpBar.AnimateValue(percent);
            UpdateLabel(current);
        }

        private void HandleNpcHPChanged(float current, float max)
        {
            float percent = max > 0f ? current / max : 0f;
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