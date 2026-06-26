using UnityEngine;

namespace SimpleSurvival.Actions
{
    [System.Serializable]
    public class MoveActionConfig
    {
        [Header("Speed Multipliers")]
        [Tooltip("Walk speed = TotalMoveSpeed × walkMultiplier")]
        public float walkMultiplier = 0.5f;
        [Tooltip("Run speed = TotalMoveSpeed × runMultiplier")]
        public float runMultiplier = 1.0f;
        [Tooltip("Sneak speed = TotalMoveSpeed × sneakMultiplier")]
        public float sneakMultiplier = 0.25f;

        [Header("Movement")]
        [Range(0f, 1f)] public float runThreshold = 0.6f;
        public float acceleration = 60f;
        public float rotationSmoothness = 12f;
        public float gravity = -20f;

        [Header("Sneak Collider")]
        public float sneakHeightReduction = 0.6f;
        public float sneakLerpSpeed = 10f;
        public LayerMask standUpCheckMask;
    }
}