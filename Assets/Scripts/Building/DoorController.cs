using UnityEngine;

namespace SimpleSurvival.Building
{
    public sealed class DoorController : MonoBehaviour
    {
        [SerializeField] private Transform hingeTransform;
        [SerializeField] private float closedAngle = 0f;
        [SerializeField] private float openAngle = 90f;
        [SerializeField] private float rotationSpeed = 5f;
        [SerializeField] private string playerTag = "Player";

        private float targetAngle;
        private float currentAngle;

        private void Awake()
        {
            currentAngle = closedAngle;
            targetAngle = closedAngle;
            hingeTransform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
        }

        private void Update()
        {
            if (Mathf.Approximately(currentAngle, targetAngle)) return;

            currentAngle = Mathf.MoveTowardsAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime * 60f);
            hingeTransform.localRotation = Quaternion.Euler(0f, currentAngle, 0f);
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            targetAngle = openAngle;
        }

        private void OnTriggerExit(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            targetAngle = closedAngle;
        }
    }
}