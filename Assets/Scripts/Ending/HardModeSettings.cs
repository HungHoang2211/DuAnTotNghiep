using SimpleSurvival.Quests;
using UnityEngine;

namespace SimpleSurvival.Stats
{
    public sealed class HardModeSettings : MonoBehaviour
    {
        public static HardModeSettings Instance { get; private set; }

        [SerializeField] private bool hardModeEnabled = true;

        public const float DamageMultiplier = 3f;
        public const float HpMultiplier = 2f;
        public const float ArmorMultiplier = 1.5f;
        public const float SpeedMultiplier = 1.2f;

        public static bool IsActive =>
            Instance != null && Instance.hardModeEnabled &&
            QuestManager.Instance != null && QuestManager.Instance.StoryCompleted;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}