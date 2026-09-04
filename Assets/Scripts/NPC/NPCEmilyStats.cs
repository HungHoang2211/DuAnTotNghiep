using System;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class NPCEmilyStats : BaseStats
    {
        public static NPCEmilyStats Instance { get; private set; }

        public static event Action<NPCEmilyStats> OnInstanceChanged;

        protected override void Awake()
        {
            base.Awake();
            Instance = this;
            OnInstanceChanged?.Invoke(Instance);
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            OnInstanceChanged?.Invoke(null);
        }
    }
}