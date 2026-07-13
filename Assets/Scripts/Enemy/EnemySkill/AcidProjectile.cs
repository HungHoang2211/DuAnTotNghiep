using System.Collections;
using UnityEngine;
using SimpleSurvival.Combat;
using SimpleSurvival.Core;

namespace SimpleSurvival.AI
{
    /// <summary>
    /// Gắn lên Prefab acid projectile (không gắn lên zombie). Bay thẳng theo hướng
    /// forward kể từ lúc spawn (kể cả chúc xuống), tự dừng lại khi:
    /// - Chạm player (gây damage)
    /// - Chạm mặt đất / vật cản (dùng Raycast quét mỗi frame để không bị xuyên qua)
    /// - Hết lifetime (bắn hụt hoàn toàn, không chạm gì)
    ///
    /// ĐÃ SỬA: toàn bộ vòng đời (projectile + hit effect) giờ dùng ObjectPool thay vì
    /// Instantiate/Destroy để tránh GC spike và leak trên Android.
    /// </summary>
    public sealed class AcidProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private GameObject hitEffectPrefab;

        [Tooltip("Thời gian hit effect tồn tại trước khi được trả về pool (đủ để particle chạy hết animation).")]
        [SerializeField] private float hitEffectLifetime = 2f;

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

        private ParticleSystem _ps;
        private Coroutine _returnRoutine;

        private void Awake()
        {
            // Cache 1 lần thay vì GetComponentInChildren mỗi lần va chạm.
            _ps = GetComponentInChildren<ParticleSystem>();
        }

        /// <summary>Gọi ngay sau khi lấy ra từ pool để truyền thông tin từ AcidAttackSkill.</summary>
        public void Init(float damage, GameObject owner, BaseEnemyController controller)
        {
            _damage = damage;
            _owner = owner;
            _controller = controller;
            _spawnTime = Time.time;
        }

        /// <summary>
        /// Gọi tự động bởi ObjectPool.Get() qua SendMessage. Reset lại toàn bộ state
        /// vì object này được TÁI SỬ DỤNG chứ không phải mới tinh.
        /// </summary>
        private void OnSpawnFromPool()
        {
            _hit = false;

            if (_returnRoutine != null)
            {
                StopCoroutine(_returnRoutine);
                _returnRoutine = null;
            }

            if (_ps != null)
            {
                _ps.Clear(true);
                _ps.Play(true);
            }
        }

        private void Update()
        {
            if (_hit) return;

            // Bắn trượt hoàn toàn (không chạm đất, không chạm player) trong suốt lifetime
            // -> trả về pool. Không hẹn giờ ngay lúc Init vì nó sẽ xung đột với thời gian
            // giữ lại trên mặt đất (groundStayDuration) sau khi đã chạm đất.
            if (Time.time - _spawnTime >= lifetime)
            {
                ReturnToPool();
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

            SpawnHitEffect(point);
            LingerThenReturn();
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

            SpawnHitEffect(transform.position);
            LingerThenReturn();
        }

        /// <summary>
        /// Lấy hit effect từ pool thay vì Instantiate, và tự động hẹn giờ trả về pool
        /// sau hitEffectLifetime giây. TRƯỚC ĐÂY effect này bị Instantiate mà không bao
        /// giờ Destroy/return -> leak object vĩnh viễn, đây là nguyên nhân chính gây
        /// crash trên Android sau một thời gian chơi.
        /// </summary>
        private void SpawnHitEffect(Vector3 point)
        {
            if (hitEffectPrefab == null) return;

            var effect = ObjectPool.Instance.Get(hitEffectPrefab, point, Quaternion.identity);
            ObjectPool.Instance.ReturnDelayed(effect, hitEffectLifetime);
        }

        /// <summary>
        /// Dùng chung cho cả 2 trường hợp chạm đất và trúng player: dừng phát thêm
        /// particle mới, giữ acid nằm lại tại chỗ thêm groundStayDuration giây trước
        /// khi thực sự trả về pool, thay vì biến mất ngay lập tức lúc va chạm.
        /// </summary>
        private void LingerThenReturn()
        {
            if (_ps != null) _ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);

            if (_returnRoutine != null) StopCoroutine(_returnRoutine);
            _returnRoutine = StartCoroutine(ReturnAfterDelay(groundStayDuration));
        }

        private IEnumerator ReturnAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool();
        }

        private void ReturnToPool()
        {
            _returnRoutine = null;

            var pooled = GetComponent<PooledObject>();
            if (pooled != null)
                pooled.ReturnToPool();
            else
                ObjectPool.Instance.Return(gameObject); // fallback nếu quên gắn PooledObject
        }
    }
}