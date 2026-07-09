using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class BodyPartDetacher : MonoBehaviour
    {
        [System.Serializable]
        private class DetachablePart
        {
            public string id;
            public GameObject attachedMesh;
            public GameObject severedRagdoll;
            public Transform socket;
            public Rigidbody rootRigidbody;
            public Transform bloodEffectPoint;
        }

        [SerializeField] private DetachablePart[] parts;

        [Header("Fling Force")]
        [SerializeField] private float flingForce = 2f;
        [SerializeField] private float flingTorque = 3f;

        [Header("Despawn")]
        [SerializeField] private float despawnDelay = 10f;

        [Header("Blood Effect")]
        [SerializeField] private GameObject bloodEffectPrefab;

        private bool[] _detached;

        private void Awake()
        {
            _detached = new bool[parts.Length];
            foreach (var part in parts)
            {
                if (part.severedRagdoll != null)
                    part.severedRagdoll.SetActive(false);
            }
        }

        public int DetachRandom()
        {
            int count = 0;
            for (int i = 0; i < parts.Length; i++)
                if (!_detached[i]) count++;

            if (count == 0) return -1;

            int pick = Random.Range(0, count);
            int current = 0;
            for (int i = 0; i < parts.Length; i++)
            {
                if (_detached[i]) continue;
                if (current == pick)
                {
                    DetachAt(i);
                    return i;
                }
                current++;
            }

            return -1;
        }

        public void DetachAt(int index)
        {
            if (index < 0 || index >= parts.Length || _detached[index]) return;

            var part = parts[index];

            if (part.attachedMesh != null)
                part.attachedMesh.SetActive(false);

            if (part.severedRagdoll != null && part.socket != null)
            {
                part.severedRagdoll.transform.SetParent(null, true);
                part.severedRagdoll.transform.SetPositionAndRotation(part.socket.position, part.socket.rotation);
                part.severedRagdoll.SetActive(true);

                Destroy(part.severedRagdoll, despawnDelay);
            }

            if (part.rootRigidbody != null)
            {
                part.rootRigidbody.linearVelocity = Vector3.zero;
                part.rootRigidbody.angularVelocity = Vector3.zero;

                Vector3 force = (Random.insideUnitSphere + Vector3.up).normalized * flingForce;
                part.rootRigidbody.AddForce(force, ForceMode.Impulse);
                part.rootRigidbody.AddTorque(Random.insideUnitSphere * flingTorque, ForceMode.Impulse);
            }

            SpawnBloodEffect(part);

            _detached[index] = true;
        }

        private void SpawnBloodEffect(DetachablePart part)
        {
            if (bloodEffectPrefab == null) return;

            Transform point = part.bloodEffectPoint != null ? part.bloodEffectPoint : part.socket;
            if (point == null) return;

            Instantiate(bloodEffectPrefab, point.position, point.rotation);
        }

        public bool IsDetached(int index)
        {
            return index >= 0 && index < _detached.Length && _detached[index];
        }

        public void ResetForSpawn()
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].attachedMesh != null)
                    parts[i].attachedMesh.SetActive(true);

                if (parts[i].severedRagdoll != null)
                    parts[i].severedRagdoll.SetActive(false);

                _detached[i] = false;
            }
        }
    }
}