using UnityEngine;
using Xyla.Core;

namespace Xyla.Core
{
 
    /// Gắn lên bất kỳ prefab nào dùng pool để:
    /// - Tự trả về pool sau _lifetime giây (thay vì Destroy).
    /// - Reset state khi được lấy ra từ pool.

    public class PooledObject : MonoBehaviour
    {
        [Tooltip("Tự động trả về pool sau bao nhiêu giây. 0 = không tự trả.")]
        [SerializeField] private float _lifetime = 0f;

        /// Prefab gốc — tự động set bởi ObjectPool.
        public GameObject SourcePrefab { get; set; }

        // Gọi bởi ObjectPool.Get() qua SendMessage
        private void OnSpawnFromPool()
        {
            if (_lifetime > 0f)
                ObjectPool.Instance.ReturnDelayed(gameObject, _lifetime);
        }

        /// Trả về pool ngay lập tức.
        public void ReturnToPool()
        {
            ObjectPool.Instance.Return(gameObject);
        }
    }
}