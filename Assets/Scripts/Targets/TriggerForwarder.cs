using UnityEngine;

namespace SimpleSurvival.Targets
{
    [RequireComponent(typeof(Collider))]
    public class TriggerForwarder : MonoBehaviour
    {
        [SerializeField] private TargetZone zone;

        private void Awake()
        {
            if (zone == null)
                zone = GetComponent<TargetZone>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (zone != null) zone.OnTargetEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (zone != null) zone.OnTargetExit(other);
        }
    }
}