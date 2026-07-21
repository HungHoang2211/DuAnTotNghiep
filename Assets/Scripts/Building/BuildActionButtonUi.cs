using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Building
{
    public enum BuildAction
    {
        Confirm,
        Cancel,
        Destroy,
        Upgrade
    }

    public sealed class BuildActionButtonUi : MonoBehaviour
    {
        [SerializeField] private BuildAction action;
        [SerializeField] private Button button;

        public BuildAction Action => action;
        public Button Button => button;
    }
}