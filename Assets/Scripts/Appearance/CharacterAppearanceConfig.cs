using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Characters.Appearance
{
    [CreateAssetMenu(menuName = "Simple Survival/Character/Appearance Config", fileName = "CharacterAppearanceConfig")]
    public sealed class CharacterAppearanceConfig : ScriptableObject
    {
        [SerializeField] private int atlasSize = 512;
        [SerializeField] private TextureFormat atlasFormat = TextureFormat.RGBA32;
        [SerializeField] private List<BodypartSlotConfig> bodySlots = new List<BodypartSlotConfig>();
        [SerializeField] private List<CosmeticMeshConfig> cosmetics = new List<CosmeticMeshConfig>();

        public int AtlasSize => atlasSize;
        public TextureFormat AtlasFormat => atlasFormat;
        public IReadOnlyList<BodypartSlotConfig> BodySlots => bodySlots;
        public IReadOnlyList<CosmeticMeshConfig> Cosmetics => cosmetics;
    }
}