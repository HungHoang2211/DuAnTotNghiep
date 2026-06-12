using UnityEngine;

namespace SimpleSurvival.Audio
{
    public enum SoundType
    {
        Footstep,
        AttackHit,
        GatherHit,
        Gunshot,
    }

    public struct SoundEvent
    {
        public Vector3 Position;
        public float HearingRadius;
        public SoundType Type;

        public SoundEvent(Vector3 position, float hearingRadius, SoundType type)
        {
            Position = position;
            HearingRadius = hearingRadius;
            Type = type;
        }
    }
}