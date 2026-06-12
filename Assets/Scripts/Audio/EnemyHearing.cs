using System;
using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class EnemyHearing : MonoBehaviour
    {
        [Header("Hearing Sensitivity")]
        [Tooltip("Multiplier áp dụng lên radius của sound event. 1 = nghe đúng radius emitter set. <1 = ít nhạy. >1 = nhạy hơn.")]
        [SerializeField] private float sensitivity = 1f;

        public event Action<SoundEvent> OnSoundHeard;

        public void NotifySound(SoundEvent soundEvent)
        {
            float distance = Vector3.Distance(transform.position, soundEvent.Position);
            float effectiveRadius = soundEvent.HearingRadius * sensitivity;

            if (distance > effectiveRadius) return;

            OnSoundHeard?.Invoke(soundEvent);
        }
    }
}