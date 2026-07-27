using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class BuildContainerAnchor : MonoBehaviour
    {
        [SerializeField] private Transform floorParent;
        [SerializeField] private Transform wallParent;

        public Transform FloorParent => floorParent;
        public Transform WallParent => wallParent;
    }
}