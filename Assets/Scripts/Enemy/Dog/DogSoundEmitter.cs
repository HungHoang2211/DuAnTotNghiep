using UnityEngine;
using SimpleSurvival.Audio;
using SimpleSurvival.Input;

namespace SimpleSurvival.Pets
{
    public sealed class DogSoundEmitter : MonoBehaviour
    {
        [Header("Footstep Radius")]
        [SerializeField] private float footstepRadius = 4f;

        [Header("Listener Filter")]
        [SerializeField] private LayerMask listenerLayers = ~0;

        [Header("References")]
        [SerializeField] private PlayerInputReader playerInputReader;

        // Animation Event: gắn vào clip Walk/Run của Dog
        public void OnFootStep()
        {
            if (playerInputReader != null && playerInputReader.IsSneakHeld) return;

            SoundEvent evt = new SoundEvent(transform.position, footstepRadius, SoundType.Footstep);
            SoundBroadcaster.Broadcast(evt, listenerLayers);
        }
    }
}