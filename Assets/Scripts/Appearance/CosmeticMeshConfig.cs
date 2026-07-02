using UnityEngine;

namespace SimpleSurvival.Characters.Appearance
{
    [CreateAssetMenu(menuName = "Simple Survival/Character/Cosmetic Mesh Config", fileName = "NewCosmeticMeshConfig")]
    public sealed class CosmeticMeshConfig : ScriptableObject
    {
        [SerializeField] private string cosmeticName;
        [SerializeField] private BodypartResource defaultOption;

        public string CosmeticName => cosmeticName;
        public BodypartResource DefaultOption => defaultOption;
    }
}