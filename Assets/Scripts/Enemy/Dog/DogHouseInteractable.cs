using UnityEngine;
using SimpleSurvival.Pets;

namespace SimpleSurvival.Targets
{
    public sealed class DogHouseInteractable : MonoBehaviour
    {
        [Tooltip("Con chó sẽ được gọi về/thả đi từ ngôi nhà này.")]
        [SerializeField] private DogController dogController;

        [Tooltip("Điểm chó sẽ đi tới và nằm xuống. Để trống sẽ dùng transform của chính ngôi nhà.")]
        [SerializeField] private Transform lieDownPoint;

        public void OnPlayerInteract(GameObject player)
        {
            if (dogController == null) return;

            Transform anchor = lieDownPoint != null ? lieDownPoint : transform;
            dogController.RequestToggleHome(anchor);
        }
    }
}