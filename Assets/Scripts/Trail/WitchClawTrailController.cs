using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class WitchClawTrailController : MonoBehaviour
    {
        [Header("Left Hand (5 finger trails)")]
        [SerializeField] private List<ClawFingerTrail> leftHandTrails = new List<ClawFingerTrail>();

        [Header("Right Hand (5 finger trails)")]
        [SerializeField] private List<ClawFingerTrail> rightHandTrails = new List<ClawFingerTrail>();

        public void ActivateLeft()
        {
            foreach (var trail in leftHandTrails)
                trail?.Activate();
        }

        public void ActivateRight()
        {
            foreach (var trail in rightHandTrails)
                trail?.Activate();
        }

        public void ActivateBoth()
        {
            ActivateLeft();
            ActivateRight();
        }

        public void DeactivateAll()
        {
            foreach (var trail in leftHandTrails)
                trail?.Deactivate();
            foreach (var trail in rightHandTrails)
                trail?.Deactivate();
        }
    }
}