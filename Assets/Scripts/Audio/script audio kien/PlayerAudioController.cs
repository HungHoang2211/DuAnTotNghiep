using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class PlayerAudioController : MonoBehaviour
    {
        [Header("Footstep Cues")]
        [SerializeField] private AudioCue walkCue;
        [SerializeField] private AudioCue runCue;
        [SerializeField] private AudioCue sneakCue;

        [Header("Consumable Cues")]
        [SerializeField] private AudioCue eatCue;
        [SerializeField] private AudioCue drinkCue;
        [SerializeField] private AudioCue healCue;

        [Header("Combat Cues")]
        [SerializeField] private AudioCue hurtCue;
        [SerializeField] private AudioCue deathCue;

        [Header("Inventory Cues")]
        [SerializeField] private AudioCue pickupCue;

        [Header("Survival Cues")]
        [SerializeField] private AudioCue healthWarningCue;
        [SerializeField] private AudioCue hungerAlertCue;
        [SerializeField] private AudioCue thirstAlertCue;

        [Header("Locomotion Params")]
        [SerializeField] private string moveModeParam = "MoveMode";
        [SerializeField] private string moveSpeedParam = "MoveSpeed";
        [SerializeField] private int sneakMoveMode = 1;
        [SerializeField] private float runSpeedThreshold = 3f;

        private Animator _animator;

        private void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public void PlayFootstep()
        {
            AudioManager.Instance.PlaySfx(ResolveFootstepCue());
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

        public void PlayEat()
        {
            AudioManager.Instance.PlaySfx(eatCue);
        }

        public void PlayDrink()
        {
            AudioManager.Instance.PlaySfx(drinkCue);
        }

        public void PlayHeal()
        {
            AudioManager.Instance.PlaySfx(healCue);
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

        public void PlayHungerAlert()
        {
            AudioManager.Instance.PlaySfx(hungerAlertCue);
        }

        public void PlayThirstAlert()
        {
            AudioManager.Instance.PlaySfx(thirstAlertCue);
        }

        public void StartHealthWarning()
        {
            AudioManager.Instance.StartLoop(healthWarningCue);
        }

        public void StopHealthWarning()
        {
            AudioManager.Instance.StopLoop(healthWarningCue);
        }
    }
}
