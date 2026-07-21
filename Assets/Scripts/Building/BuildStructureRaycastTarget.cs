using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class BuildStructureRaycastTarget : MonoBehaviour
    {
        [SerializeField] private PlacedStructureView owner;

        public PlacedStructureView Owner => owner;
    }
}