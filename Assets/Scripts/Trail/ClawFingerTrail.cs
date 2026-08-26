using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class ClawFingerTrail : MonoBehaviour
    {
        private TrailRenderer _trail;

        private void Awake()
        {
            _trail = GetComponent<TrailRenderer>();
            if (_trail != null) _trail.emitting = false;
        }

        public void Activate()
        {
            if (_trail == null) return;
            _trail.Clear();
            _trail.emitting = true;
        }

        public void Deactivate()
        {
            if (_trail == null) return;
            _trail.emitting = false;
        }
    }
}