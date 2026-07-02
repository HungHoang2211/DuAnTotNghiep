using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Player
{
    public sealed class PlayerSpeechTrigger : MonoBehaviour
    {
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private Transform followTransform;
        [SerializeField] private float threshold = 20f;

        [Header("Messages")]
        [SerializeField] private string hungryMessage = "I'm hungry!";
        [SerializeField] private string thirstyMessage = "I'm thirsty!";

        private bool _hungryArmed = true;
        private bool _thirstyArmed = true;

        private void Awake()
        {
            if (playerStats == null)
                playerStats = GetComponentInChildren<PlayerStats>();
            if (followTransform == null)
                followTransform = transform;
        }

        private void OnEnable()
        {
            if (playerStats == null) return;
            playerStats.OnHungerChanged += HandleHungerChanged;
            playerStats.OnThirstChanged += HandleThirstChanged;
        }

        private void OnDisable()
        {
            if (playerStats == null) return;
            playerStats.OnHungerChanged -= HandleHungerChanged;
            playerStats.OnThirstChanged -= HandleThirstChanged;
        }

        private void HandleHungerChanged(float current, float max)
        {
            if (current < threshold)
            {
                if (_hungryArmed)
                {
                    _hungryArmed = false;
                    Fire(hungryMessage);
                }
            }
            else
            {
                _hungryArmed = true;
            }
        }

        private void HandleThirstChanged(float current, float max)
        {
            if (current < threshold)
            {
                if (_thirstyArmed)
                {
                    _thirstyArmed = false;
                    Fire(thirstyMessage);
                }
            }
            else
            {
                _thirstyArmed = true;
            }
        }

        private void Fire(string message)
        {
            HudManager hud = HudManager.Instance;
            if (hud == null || hud.Speech == null) return;
            hud.Speech.Show(followTransform, message, SpeechHudType.Bad);
        }
    }
}