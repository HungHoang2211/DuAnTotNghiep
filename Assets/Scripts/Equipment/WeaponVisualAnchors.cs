using UnityEngine;

namespace SimpleSurvival.Items
{
    public sealed class WeaponVisualAnchors : MonoBehaviour
    {
        [SerializeField] private Transform leftHand0TargetIK;
        [SerializeField] private Transform leftHand1TargetIK;
        [SerializeField] private ParticleSystem muzzleFlashParticles;
        [SerializeField] private ParticleSystem shellCasingParticles;
        [SerializeField] private AudioClip fireSfx;
        [SerializeField] private AudioClip hitSfx;

        public Transform LeftHand0TargetIK => leftHand0TargetIK;
        public Transform LeftHand1TargetIK => leftHand1TargetIK;
        public ParticleSystem MuzzleFlashParticles => muzzleFlashParticles;
        public ParticleSystem ShellCasingParticles => shellCasingParticles;
        public AudioClip FireSfx => fireSfx;
        public AudioClip HitSfx => hitSfx;

        public bool IsRanged => muzzleFlashParticles != null;
    }
}