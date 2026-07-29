using UnityEngine;
using SimpleSurvival.Pets;

namespace SimpleSurvival.Targets
{
    public sealed class DogHouseInteractable : MonoBehaviour
    {
        [SerializeField] private DogController dogController;

        [SerializeField] private Transform lieDownPoint;

        public void OnPlayerInteract(GameObject player)
        {
            DogController controller = dogController != null ? dogController : DogController.Instance;
            if (controller == null) return;

            Transform anchor = lieDownPoint != null ? lieDownPoint : transform;
            controller.RequestToggleHome(anchor);
        }
    }
}