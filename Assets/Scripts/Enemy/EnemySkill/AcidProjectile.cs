using UnityEngine;
using SimpleSurvival.Combat;

namespace SimpleSurvival.AI
{
    /// <summary>
    /// Gắn lên Prefab acid projectile (không gắn lên zombie). Bay thẳng theo hướng
    /// forward kể từ lúc spawn (kể cả chúc xuống), tự dừng lại khi:
    /// - Chạm player (gây damage)
    /// - Chạm mặt đất / vật cản (dùng Raycast quét mỗi frame để không bị xuyên qua)
    /// - Hết lifetime (bắn hụt hoàn toàn, không chạm gì)
    /// </summary>
    public sealed class AcidProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private GameObject hitEffectPrefab;

        [Header("Ground / Obstacle")]
        [Tooltip("Layer của mặt đất và vật cản. Dùng Raycast quét đường bay mỗi frame để acid dừng đúng lúc chạm đất, không xuyên qua.")]
        [SerializeField] private LayerMask groundLayer;
        [Tooltip("Sau khi chạm đất, acid sẽ nằm yên tại chỗ thêm khoảng thời gian này trước khi thực sự biến mất (đủ để particle chạy hết animation).")]
        [SerializeField] private float groundStayDuration = 2f;

        private float _damage;
        private GameObject _owner;
        private BaseEnemyController _controller;
        private bool _hit;
        private float _spawnTime;

        /// <summary>Gọi ngay sau khi Instantiate để truyền thông tin từ AcidAttackSkill.</summary>
        public void Init(float damage, GameObject owner, BaseEnemyController controller)
        {
            _damage = damage;
            _owner = owner;
            _controller = controller;
            _spawnTime = Time.time;
        }

        private void Update()
        {
            if (_hit) return;

            // Bắn trượt hoàn toàn (không chạm đất, không chạm player) trong suốt lifetime
            // -> tự huỷ. Không dùng Destroy(gameObject, lifetime) hẹn giờ ngay lúc Init vì
            // nó sẽ xung đột với thời gian giữ lại trên mặt đất (groundStayDuration) sau khi
            // đã chạm đất - hẹn giờ cũ có thể phá huỷ acid sớm hơn dự kiến.
            if (Time.time - _spawnTime >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 moveStep = transform.forward * speed * Time.deltaTime;
            float distance = moveStep.magnitude;

            // Quét trước đường acid SẮP bay tới trong frame này. Cách này tránh việc
            // acid "nhảy" xuyên qua mặt đất/vật cản mỏng khi di chuyển bằng
            // transform.position (không đi qua physics engine nên không tự có
            // continuous collision detection).
            // QueryTriggerInteraction.Ignore: đảm bảo Raycast chỉ tính Collider thường
            // (mặt đất thật), không tính Trigger Collider (kể cả Trigger Collider của
            // chính viên đạn này hay của Player) để tránh tự bắn trúng chính mình.
            if (groundLayer != 0 &&
                Physics.Raycast(transform.position, moveStep.normalized, out RaycastHit hit, distance, groundLayer, QueryTriggerInteraction.Ignore))
            {
                HandleGroundHit(hit.point);
                return;
            }

            transform.position += moveStep;
        }

        private void HandleGroundHit(Vector3 point)
        {
            _hit = true;
            transform.position = point;

            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, point, Quaternion.identity);

            LingerThenDestroy();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (_hit) return;
            if (!other.CompareTag("Player")) return;

            _hit = true;

            var damageable = other.GetComponent<IDamageable>()
                ?? other.GetComponentInChildren<IDamageable>()
                ?? other.GetComponentInParent<IDamageable>();

            if (damageable != null && !damageable.IsDead)
            {
                damageable.TakeDamage(_damage, _owner);
                if (_controller != null) _controller.NotifyDamageDealt();
            }

            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);

            LingerThenDestroy();
        }

        /// <summary>
        /// Dùng chung cho cả 2 trường hợp chạm đất và trúng player: dừng phát thêm
        /// particle mới, giữ acid nằm lại tại chỗ thêm groundStayDuration giây trước
        /// khi thực sự Destroy, thay vì biến mất ngay lập tức lúc va chạm.
        /// </summary>
        private void LingerThenDestroy()
        {
            var ps = GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            Destroy(gameObject, groundStayDuration);
        }
    }
}