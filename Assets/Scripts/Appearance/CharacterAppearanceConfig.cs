using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance
{
    [CreateAssetMenu(menuName = "Simple Survival/Character/Appearance Config", fileName = "CharacterAppearanceConfig")]
    public sealed class CharacterAppearanceConfig : ScriptableObject
    {
        [SerializeField] private int atlasSize = 512;
        [SerializeField] private TextureFormat atlasFormat = TextureFormat.RGBA32;
        [SerializeField] private List<BodypartSlotEntry> slots = new List<BodypartSlotEntry>();
        [SerializeField] private List<Color> haircutPalette = new List<Color>();

        public int AtlasSize => atlasSize;
        public TextureFormat AtlasFormat => atlasFormat;
        public IReadOnlyList<BodypartSlotEntry> Slots => slots;
        public IReadOnlyList<Color> HaircutPalette => haircutPalette;
    }
}