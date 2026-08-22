using SimpleSurvival.Items;
using SimpleSurvival.Stats;
using UnityEngine;


namespace SimpleSurvival.Audio
{
    public class PlayerAudioController : MonoBehaviour
    {
        [Header("Footstep Cues")]
        [SerializeField] private AudioCue walkCue;
        [SerializeField] private AudioCue runCue;
        [SerializeField] private AudioCue sneakCue;

        [Header("Combat Cues")]
        [SerializeField] private AudioCue hurtCue;
        [SerializeField] private AudioCue deathCue;

        [Header("Inventory Cues")]
        [SerializeField] private AudioCue pickupCue;

        [Header("Breathing Cue")]
        [SerializeField] private AudioCue breathingCue;

        [Header("Locomotion Params")]
        [SerializeField] private string moveModeParam = "MoveMode";
        [SerializeField] private string moveSpeedParam = "MoveSpeed";
        [SerializeField] private int sneakMoveMode = 1;
        [SerializeField] private float runSpeedThreshold = 0.5f;
        [SerializeField] private float minMoveSpeedForFootstep = 0.05f;

        [Header("Combat Swing Cues")]
        [SerializeField] private AudioCue fistsImpactCue;
        [SerializeField] private AudioCue melee1HImpactCue;
        [SerializeField] private AudioCue melee2HImpactCue;
        [SerializeField] private AudioCue pistolImpactCue;
        [SerializeField] private AudioCue rifleImpactCue;

        [Header("Combat Hit Cues")]
        [SerializeField] private AudioCue fistsHitCue;
        [SerializeField] private AudioCue melee1HHitCue;
        [SerializeField] private AudioCue melee2HHitCue;

        [Header("Gather Impact Cues")]
        [SerializeField] private AudioCue gatherAxeCue;
        [SerializeField] private AudioCue gatherPickaxeCue;

        [Header("References")]
        [SerializeField] private PlayerStats playerStats;

        private Animator _animator;
        private bool _wasRunning;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
            if (playerStats == null) playerStats = GetComponentInParent<PlayerStats>();
        }

        private void OnEnable()
        {
            if (playerStats != null)
            {
                playerStats.OnDamagedBy += HandleDamaged;
                playerStats.OnDeath += HandleDeath;
            }
        }

        private void OnDisable()
        {
            if (playerStats != null)
            {
                playerStats.OnDamagedBy -= HandleDamaged;
                playerStats.OnDeath -= HandleDeath;
            }

            StopBreathingIfNeeded();
        }

        private void Update()
        {
            if (AudioManager.Instance == null) return;

            bool isRunning = IsRunning();

            if (isRunning && !_wasRunning)
                AudioManager.Instance.StartLoop(breathingCue);
            else if (!isRunning && _wasRunning)
                AudioManager.Instance.StopLoop(breathingCue);

            _wasRunning = isRunning;
        }

        private void HandleDamaged(GameObject attacker)
        {
            PlayHurt();
        }

        private void HandleDeath(GameObject source)
        {
            PlayDeath();
            StopBreathingIfNeeded();
        }

        private void StopBreathingIfNeeded()
        {
            if (!_wasRunning) return;
            if (AudioManager.Instance != null)
                AudioManager.Instance.StopLoop(breathingCue);
            _wasRunning = false;
        }

        public void PlayAttackSwing(WeaponCategory category)
        {
            AudioManager.Instance.PlaySfx(ResolveAttackSwingCue(category));
        }

        public void PlayAttackHit(WeaponCategory category)
        {
            AudioCue cue = ResolveAttackHitCue(category) ?? ResolveAttackSwingCue(category);
            AudioManager.Instance.PlaySfx(cue);
        }

        private AudioCue ResolveAttackSwingCue(WeaponCategory category)
        {
            switch (category)
            {
                case WeaponCategory.Fists: return fistsImpactCue;
                case WeaponCategory.Melee1H: return melee1HImpactCue;
                case WeaponCategory.Melee2H: return melee2HImpactCue;
                case WeaponCategory.Pistol: return pistolImpactCue;
                case WeaponCategory.Rifle: return rifleImpactCue;
                default: return fistsImpactCue;
            }
        }

        private AudioCue ResolveAttackHitCue(WeaponCategory category)
        {
            switch (category)
            {
                case WeaponCategory.Fists:
                    return fistsHitCue;

                case WeaponCategory.Melee1H:
                    return melee1HHitCue;

                case WeaponCategory.Melee2H:
                    return melee2HHitCue;

                default:
                    return null;
            }
        }

        public void PlayFootstep()
        {
            if (_animator.GetFloat(moveSpeedParam) < minMoveSpeedForFootstep) return;

            AudioManager.Instance.PlaySfx(ResolveFootstepCue());
        }

        public void PlayGatherImpactAxe()
        {
            AudioManager.Instance.PlaySfx(gatherAxeCue);
        }

        public void PlayGatherImpactPickaxe()
        {
            AudioManager.Instance.PlaySfx(gatherPickaxeCue);
        }

        private AudioCue ResolveFootstepCue()
        {
            if (IsSneaking())
                return sneakCue;

            if (IsRunning())
                return runCue;

            return walkCue;
        }

        private bool IsSneaking()
        {
            return _animator.GetInteger(moveModeParam) == sneakMoveMode;
        }

        private bool IsRunning()
        {
            return _animator.GetFloat(moveSpeedParam) >= runSpeedThreshold;
        }

        public void PlayHurt()
        {
            AudioManager.Instance.PlaySfx(hurtCue);
        }

        public void PlayDeath()
        {
            AudioManager.Instance.PlaySfx(deathCue);
        }

        public void PlayPickup()
        {
            AudioManager.Instance.PlaySfx(pickupCue);
        }
    }
}