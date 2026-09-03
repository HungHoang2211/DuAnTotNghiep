using System.Collections;
using UnityEngine;
using SimpleSurvival.Combat;
using SimpleSurvival.Core;

namespace SimpleSurvival.AI
{
    public sealed class AcidProjectile : MonoBehaviour
    {
        [SerializeField] private float speed = 8f;
        [SerializeField] private float lifetime = 4f;
        [SerializeField] private GameObject hitEffectPrefab;
        [SerializeField] private float hitEffectLifetime = 2f;

        [Header("Ground / Obstacle")]
        [SerializeField] private LayerMask groundLayer;
        [SerializeField] private float groundStayDuration = 2f;

        [Header("Player Hit Detection")]
        [SerializeField] private LayerMask playerLayer;
        [SerializeField] private float hitRadius = 0.25f;

        private float _damage;
        private GameObject _owner;
        private BaseEnemyController _controller;
        private bool _hit;
        private float _spawnTime;

        private ParticleSystem _ps;
        private Coroutine _returnRoutine;

        private void Awake()
        {
            _ps = GetComponentInChildren<ParticleSystem>();
        }

        public void Init(float damage, GameObject owner, BaseEnemyController controller)
        {
            _damage = damage;
            _owner = owner;
            _controller = controller;
            _spawnTime = Time.time;
        }

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

            if (Time.time - _spawnTime >= lifetime)
            {
                ReturnToPool();
                return;
            }

            Vector3 moveStep = transform.forward * speed * Time.deltaTime;
            float distance = moveStep.magnitude;
            Vector3 direction = moveStep.normalized;

            if (playerLayer != 0 &&
                Physics.SphereCast(transform.position, hitRadius, direction, out RaycastHit playerHit, distance, playerLayer, QueryTriggerInteraction.Ignore))
            {
                HandlePlayerHit(playerHit.collider);
                return;
            }

            if (groundLayer != 0 &&
                Physics.Raycast(transform.position, direction, out RaycastHit hit, distance, groundLayer, QueryTriggerInteraction.Ignore))
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

        private void HandlePlayerHit(Collider other)
        {
            _hit = true;
            transform.position = other.ClosestPoint(transform.position);

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

        private void OnTriggerEnter(Collider other)
        {
            if (_hit) return;
            if (!other.CompareTag("Player")) return;

            HandlePlayerHit(other);
        }

        private void SpawnHitEffect(Vector3 point)
        {
            if (hitEffectPrefab == null) return;

            var effect = ObjectPool.Instance.Get(hitEffectPrefab, point, Quaternion.identity);
            ObjectPool.Instance.ReturnDelayed(effect, hitEffectLifetime);
        }

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
                ObjectPool.Instance.Return(gameObject);
        }
    }
}