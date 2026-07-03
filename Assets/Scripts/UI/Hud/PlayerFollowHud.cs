using UnityEngine;

namespace SimpleSurvival.UI.Hud
{
    public sealed class PlayerFollowHud : MonoBehaviour
    {
        [Header("Follow")]
        [SerializeField] private Transform followTarget;
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.4f, 0f);

        [Header("Canvas Refs")]
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Camera uiCamera;

        private RectTransform _rect;

        private void Awake()
        {
            _rect = (RectTransform)transform;
        }

        private void LateUpdate()
        {
            if (followTarget == null || canvasRect == null || gameCamera == null) return;

            Vector3 worldPos = followTarget.position + worldOffset;
            Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(gameCamera, worldPos);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, screenPoint, uiCamera, out Vector2 localPoint);

            _rect.localPosition = new Vector3(localPoint.x, localPoint.y, 0f);
        }
    }
}