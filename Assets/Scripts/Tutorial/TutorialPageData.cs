using UnityEngine;

namespace SimpleSurvival.UI.Tutorial
{
    [CreateAssetMenu(fileName = "TutorialPage_", menuName = "SimpleSurvival/Tutorial/Tutorial Page")]
    public class TutorialPageData : ScriptableObject
    {
        [SerializeField] private Sprite pageImage;
        [SerializeField] private int order;

        public Sprite PageImage => pageImage;
        public int Order => order;
    }
}