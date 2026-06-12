using UnityEngine;

namespace SimpleSurvival.Audio
{
    public static class SoundBroadcaster
    {
        private static readonly Collider[] _buffer = new Collider[32];

        public static void Broadcast(SoundEvent soundEvent, LayerMask listenerLayers)
        {
            int count = Physics.OverlapSphereNonAlloc(
                soundEvent.Position,
                soundEvent.HearingRadius,
                _buffer,
                listenerLayers);

            for (int i = 0; i < count; i++)
            {
                EnemyHearing hearing = _buffer[i].GetComponentInParent<EnemyHearing>();
                if (hearing != null)
                    hearing.NotifySound(soundEvent);
            }
        }
    }
}