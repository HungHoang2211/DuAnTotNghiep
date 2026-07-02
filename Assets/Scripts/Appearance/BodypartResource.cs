using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance
{
    [CreateAssetMenu(menuName = "Simple Survival/Character/Bodypart Resource", fileName = "NewBodypartResource")]
    public sealed class BodypartResource : ScriptableObject
    {
        [SerializeField] private string bodypartId;
        [SerializeField] private Mesh mesh;
        [SerializeField] private Texture2D texture;
        [SerializeField] private Texture2D tintMask;
        [SerializeField] private Color tintColor = Color.white;
        [SerializeField] private List<string> hiddenCosmetics = new List<string>();

        public string BodypartId => bodypartId;
        public Mesh Mesh => mesh;
        public Texture2D Texture => texture;
        public Texture2D TintMask => tintMask;
        public Color TintColor => tintColor;
        public IReadOnlyList<string> HiddenCosmetics => hiddenCosmetics;
    }
}