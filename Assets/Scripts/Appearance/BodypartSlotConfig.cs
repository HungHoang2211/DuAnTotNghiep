using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Characters.Appearance
{
    [CreateAssetMenu(menuName = "Simple Survival/Character/Bodypart Slot Config", fileName = "NewBodypartSlotConfig")]
    public sealed class BodypartSlotConfig : ScriptableObject
    {
        [SerializeField] private EquipSlot equipSlot;
        [SerializeField] private BodypartRenderMode renderMode;
        [SerializeField] private RectInt atlasRect;
        [SerializeField] private BodypartResource defaultResource;

        public EquipSlot EquipSlot => equipSlot;
        public BodypartRenderMode RenderMode => renderMode;
        public RectInt AtlasRect => atlasRect;
        public BodypartResource DefaultResource => defaultResource;
    }
}