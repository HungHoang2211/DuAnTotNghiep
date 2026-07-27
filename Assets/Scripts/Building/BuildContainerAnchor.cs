using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class BuildContainerAnchor : MonoBehaviour
    {
        [SerializeField] private Transform floorParent;
        [SerializeField] private Transform wallParent;
        [SerializeField] private Renderer gridOverlayRenderer;

        public Transform FloorParent => floorParent;
        public Transform WallParent => wallParent;
        public Renderer GridOverlayRenderer => gridOverlayRenderer;
    }
}