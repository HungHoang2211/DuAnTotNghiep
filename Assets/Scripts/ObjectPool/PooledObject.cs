using UnityEngine;


namespace SimpleSurvival.Core
{
    public class PooledObject : MonoBehaviour
    {
        [Tooltip("Tự động trả về pool sau bao nhiêu giây. 0 = không tự trả.")]
        [SerializeField] private float _lifetime = 0f;

        public GameObject SourcePrefab { get; set; }
        private void OnSpawnFromPool()
        {
            if (_lifetime > 0f)
                ObjectPool.Instance.ReturnDelayed(gameObject, _lifetime);
        }
        public void ReturnToPool()
        {
            ObjectPool.Instance.Return(gameObject);
        }
    }
}