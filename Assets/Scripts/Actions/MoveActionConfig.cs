using System;
using UnityEngine;

namespace SimpleSurvival.Actions
{
    [Serializable]
    public class MoveActionConfig
    {
        public float walkSpeed = 3f;
        public float runSpeed = 6f;
        public float sneakSpeed = 1.5f;

        [Range(0.3f, 0.95f)]
        public float runThreshold = 0.6f;

        public float acceleration = 60f;

        [Range(1f, 30f)]
        public float rotationSmoothness = 12f;

        public float gravity = -20f;

        public float sneakHeightReduction = 0.6f;
        public float sneakLerpSpeed = 10f;
        public LayerMask standUpCheckMask = ~0;
    }
}