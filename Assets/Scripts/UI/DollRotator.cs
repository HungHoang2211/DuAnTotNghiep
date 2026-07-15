using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.UI
{
    public sealed class DollRotator : MonoBehaviour, IDragHandler
    {
        [SerializeField] private Transform dollRoot;
        [SerializeField] private float rotateSpeed = 0.3f;

        public void OnDrag(PointerEventData eventData)
        {
            if (dollRoot == null) return;

            float deltaX = eventData.delta.x;
            dollRoot.Rotate(Vector3.up, -deltaX * rotateSpeed, Space.World);
        }
    }
}