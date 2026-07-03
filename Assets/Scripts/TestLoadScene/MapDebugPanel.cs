using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    public class MapDebugPanel : MonoBehaviour
    {
        [SerializeField] private Button toBaseButton;
        [SerializeField] private Button toForestButton;
        [SerializeField] private string baseScene = "Base";
        [SerializeField] private string forestScene = "ForestFarm";

        private void Awake()
        {
            if (toBaseButton != null)
                toBaseButton.onClick.AddListener(GoToBase);

            if (toForestButton != null)
                toForestButton.onClick.AddListener(GoToForest);
        }

        private void GoToBase()
        {
            if (MapTransitionController.Instance != null)
                MapTransitionController.Instance.GoToMap(baseScene);
        }

        private void GoToForest()
        {
            if (MapTransitionController.Instance != null)
                MapTransitionController.Instance.GoToMap(forestScene);
        }
    }
}