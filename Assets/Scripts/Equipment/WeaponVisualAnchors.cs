using UnityEngine;

namespace SimpleSurvival.Items
{
    public sealed class WeaponVisualAnchors : MonoBehaviour
    {
        [SerializeField] private Transform muzzlePoint;
        [SerializeField] private Transform leftHandGripPoint;
        [SerializeField] private GameObject muzzleFlashPrefab;
        [SerializeField] private AudioClip fireSfx;
        [SerializeField] private AudioClip hitSfx;
        [SerializeField] private AudioClip breakSfx;

        public Transform MuzzlePoint => muzzlePoint;
        public Transform LeftHandGripPoint => leftHandGripPoint;
        public GameObject MuzzleFlashPrefab => muzzleFlashPrefab;
        public AudioClip FireSfx => fireSfx;
        public AudioClip HitSfx => hitSfx;
        public AudioClip BreakSfx => breakSfx;

        public bool HasLeftHandGrip => leftHandGripPoint != null;
        public bool IsRanged => muzzlePoint != null;
    }
}