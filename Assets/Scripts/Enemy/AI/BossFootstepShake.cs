using UnityEngine;
using SimpleSurvival.Cameras;

namespace SimpleSurvival.AI
{
    public sealed class BossFootstepShake : MonoBehaviour
    {
        [Header("Shake Settings")]
        [SerializeField] private float intensity = 0.15f;
        [SerializeField] private float duration = 0.2f;
        [SerializeField] private float maxDistanceToPlayer = 15f;

        [Header("Refs")]
        [SerializeField] private Transform player;

        private void Start()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
        }

        public void OnFootstep()
        {
            if (player == null)
            {
                CameraShake.Shake(intensity, duration);
                return;
            }

            float dist = Vector3.Distance(transform.position, player.position);
            if (dist > maxDistanceToPlayer) return;

            float falloff = 1f - Mathf.Clamp01(dist / maxDistanceToPlayer);
            CameraShake.Shake(intensity * falloff, duration);
        }
    }
}