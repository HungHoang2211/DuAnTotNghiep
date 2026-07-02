using UnityEngine;

namespace SimpleSurvival.Characters.Appearance
{
    [CreateAssetMenu(menuName = "Simple Survival/Character/Bodypart Resource", fileName = "NewBodypartResource")]
    public sealed class BodypartResource : ScriptableObject
    {
        [SerializeField] private string bodypartId;
        [SerializeField] private Mesh mesh;
        [SerializeField] private Texture2D texture;
        [SerializeField] private Texture2D regionMask;
        [SerializeField] private Texture2D detailTexture;
        [SerializeField] private Vector2 detailTiling = Vector2.one;
        [SerializeField] private Vector2 detailOffset;
        [SerializeField] private bool disableHaircut;
        [SerializeField] private bool disableBeard;

        public string BodypartId => bodypartId;
        public Mesh Mesh => mesh;
        public Texture2D Texture => texture;
        public Texture2D RegionMask => regionMask;
        public Texture2D DetailTexture => detailTexture;
        public Vector2 DetailTiling => detailTiling;
        public Vector2 DetailOffset => detailOffset;
        public bool DisableHaircut => disableHaircut;
        public bool DisableBeard => disableBeard;
    }
}