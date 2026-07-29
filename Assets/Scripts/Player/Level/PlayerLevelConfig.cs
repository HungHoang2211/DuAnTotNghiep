using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Progression
{
    [CreateAssetMenu(menuName = "Simple Survival/Progression/Player Level Config", fileName = "PlayerLevelConfig")]
    public sealed class PlayerLevelConfig : ScriptableObject
    {
        [SerializeField] private List<int> expRequiredPerLevel = new List<int>();

        public int MaxLevel => expRequiredPerLevel.Count + 1;

        public int GetExpRequired(int level)
        {
            int index = level - 1;
            if (index < 0 || index >= expRequiredPerLevel.Count) return 0;
            return expRequiredPerLevel[index];
        }
    }
}