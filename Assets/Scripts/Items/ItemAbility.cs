using UnityEngine;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// Base for every capability an item can have (weapon, tool, equipment, ...).
    /// Each ability is its own asset, created from the Create menu, and dragged
    /// into an ItemData. Adding a new ability type means adding a new subclass —
    /// no existing class needs to change.
    /// </summary>
    public abstract class ItemAbility : ScriptableObject
    {
        /// <summary>
        /// Human-readable ability name, shown in tooltips. Each subclass returns
        /// its own constant so the UI never needs a type check.
        /// </summary>
        public abstract string AbilityName { get; }
    }
}
