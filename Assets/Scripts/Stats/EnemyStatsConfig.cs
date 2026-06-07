using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Enemy Stats Config", fileName = "EnemyStatsConfig")]
    public sealed class EnemyStatsConfig : BaseStatsConfig
    {
        [Header("Identity")]
        [SerializeField] private EnemyKind kind = EnemyKind.Zombie;
        [SerializeField] private string displayName = "Enemy";

        [Header("HP Bar")]
        [SerializeField] private Color hpBarColor = new Color(0.85f, 0.2f, 0.2f);

        public EnemyKind Kind => kind;
        public string DisplayName => displayName;
        public Color HPBarColor => hpBarColor;
    }
}