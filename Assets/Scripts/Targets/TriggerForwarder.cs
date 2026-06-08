using UnityEngine;

namespace SimpleSurvival.Targets
{
    [RequireComponent(typeof(Collider))]
    public class TriggerForwarder : MonoBehaviour
    {
        [SerializeField] private PlayerTargetChecker checker;

        private void Awake()
        {
            if (checker == null)
                checker = GetComponent<PlayerTargetChecker>();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (checker != null) checker.OnTargetEnter(other);
        }

        private void OnTriggerExit(Collider other)
        {
            if (checker != null) checker.OnTargetExit(other);
        }
    }
}