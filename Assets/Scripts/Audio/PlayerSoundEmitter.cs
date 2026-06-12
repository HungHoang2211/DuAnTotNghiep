using UnityEngine;
using SimpleSurvival.Input;
using SimpleSurvival.Player;

namespace SimpleSurvival.Audio
{
    public class PlayerSoundEmitter : MonoBehaviour
    {
        [Header("Footstep Radius")]
        [SerializeField] private float sneakFootstepRadius = 2f;
        [SerializeField] private float walkFootstepRadius = 5f;
        [SerializeField] private float runFootstepRadius = 10f;

        [Header("Action Sound Radius")]
        [SerializeField] private float attackHitRadius = 6f;
        [SerializeField] private float gatherHitRadius = 8f;
        [SerializeField] private float gunshotRadius = 25f;

        [Header("Listener Filter")]
        [Tooltip("Layer mask để OverlapSphere lọc enemy listener.")]
        [SerializeField] private LayerMask listenerLayers = ~0;

        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;

        private void Awake()
        {
            if (inputReader == null) inputReader = GetComponentInParent<PlayerInputReader>();
        }

        public void EmitFootstep()
        {
            float radius = ResolveFootstepRadius();
            if (radius <= 0f) return;
            Emit(radius, SoundType.Footstep);
        }

        public void EmitAttackHit()
        {
            Emit(attackHitRadius, SoundType.AttackHit);
        }

        public void EmitGatherHit()
        {
            Emit(gatherHitRadius, SoundType.GatherHit);
        }

        public void EmitGunshot()
        {
            Emit(gunshotRadius, SoundType.Gunshot);
        }

        private void Emit(float radius, SoundType type)
        {
            SoundEvent evt = new SoundEvent(transform.position, radius, type);
            SoundBroadcaster.Broadcast(evt, listenerLayers);
        }

        private float ResolveFootstepRadius()
        {
            if (inputReader != null && inputReader.IsSneakHeld) return sneakFootstepRadius;
            return walkFootstepRadius;
            // TODO: thêm run mode khi có
        }
    }
}