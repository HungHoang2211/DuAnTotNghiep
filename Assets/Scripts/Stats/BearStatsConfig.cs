using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Bear Config", fileName = "BearStatsConfig")]
    public sealed class BearStatsConfig : EnemyStatsConfig
    {
        [Header("Bear Specific")]
        [Tooltip("Player movement speed threshold for hearing detection. Below this, footsteps are silent.")]
        [SerializeField] private float footstepMinSpeed = 2f;

        public float FootstepMinSpeed => footstepMinSpeed;
    }
}