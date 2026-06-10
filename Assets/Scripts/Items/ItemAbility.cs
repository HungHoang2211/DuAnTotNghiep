using UnityEngine;

namespace SimpleSurvival.Items
{
    public abstract class ItemAbility : ScriptableObject
    {
        public abstract string AbilityName { get; }
    }
}
