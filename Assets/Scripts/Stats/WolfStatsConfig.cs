using UnityEngine;

namespace SimpleSurvival.Stats
{
    [CreateAssetMenu(menuName = "Simple Survival/Stats/Wolf Config", fileName = "WolfStatsConfig")]
    public sealed class WolfStatsConfig : EnemyStatsConfig
    {
        [Header("Wolf Specific")]
        [SerializeField] private float walkSpeed = 2f;
        [Tooltip("Player movement speed threshold for hearing detection. Below this, footsteps are silent.")]
        [SerializeField] private float footstepMinSpeed = 2f;

        public float WalkSpeed => walkSpeed;
        public float FootstepMinSpeed => footstepMinSpeed;
    }
}