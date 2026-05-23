using System.Collections.Generic;
using UnityEngine;

namespace Xyla.Core
{
    /// Generic Object Pool — dùng cho mọi thứ vốn bị Destroy (đạn, effect, enemy, damage text...).
    /// Thay vì Instantiate/Destroy liên tục gây GC spike, pool tái sử dụng object đã có.
    ///
    /// CÁCH DÙNG:
    ///   // Lấy object từ pool
    ///   var obj = ObjectPool.Instance.Get(prefab, position, rotation);
    ///
    ///   // Trả object về pool (thay vì Destroy)
    ///   ObjectPool.Instance.Return(obj);
    ///
    ///   // Hoặc tự động trả về sau X giây
    ///   ObjectPool.Instance.ReturnDelayed(obj, 2f);

    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [Tooltip("Số object tối đa mỗi prefab được giữ trong pool. " +
                 "Vượt quá sẽ Destroy thay vì cất vào pool.")]
        [SerializeField] private int _maxPoolSizePerPrefab = 20;

        // Key = prefab gốc, Value = stack các object đang chờ
        private readonly Dictionary<GameObject, Stack<GameObject>> _pools
            = new Dictionary<GameObject, Stack<GameObject>>();

        // Map từ instance → prefab gốc để biết trả về pool nào
        private readonly Dictionary<GameObject, GameObject> _instanceToPrefab
            = new Dictionary<GameObject, GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }


        /// Lấy object từ pool. Nếu pool trống thì Instantiate mới.
        public GameObject Get(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            if (prefab == null)
            {
                Debug.LogWarning("[ObjectPool] prefab is null.");
                return null;
            }

            GameObject obj;

            if (TryGetFromPool(prefab, out obj))
            {
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(prefab, position, rotation);
                _instanceToPrefab[obj] = prefab;
            }

            // Thông báo cho object biết nó vừa được lấy ra
            obj.SendMessage("OnSpawnFromPool", SendMessageOptions.DontRequireReceiver);
            return obj;
        }

        /// Overload không cần rotation.
        public GameObject Get(GameObject prefab, Vector3 position)
            => Get(prefab, position, Quaternion.identity);

        /// Trả object về pool. Gọi thay vì Destroy().
        public void Return(GameObject obj)
        {
            if (obj == null) return;

            // Thông báo cho object biết nó sắp bị cất vào pool
            obj.SendMessage("OnReturnToPool", SendMessageOptions.DontRequireReceiver);

            if (!_instanceToPrefab.TryGetValue(obj, out GameObject prefab))
            {
                // Object này không do pool tạo ra → Destroy bình thường
                Destroy(obj);
                return;
            }

            if (!_pools.TryGetValue(prefab, out Stack<GameObject> stack))
            {
                stack = new Stack<GameObject>();
                _pools[prefab] = stack;
            }

            if (stack.Count >= _maxPoolSizePerPrefab)
            {
                // Pool đã đầy → Destroy thật
                _instanceToPrefab.Remove(obj);
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            stack.Push(obj);
        }

        /// Trả object về pool sau X giây. Dùng cho effect, đạn hết hạn...
        public void ReturnDelayed(GameObject obj, float delay)
        {
            if (obj == null) return;
            StartCoroutine(ReturnAfterDelay(obj, delay));
        }

        /// Xóa toàn bộ pool của 1 prefab (dùng khi unload scene).
        public void ClearPool(GameObject prefab)
        {
            if (!_pools.TryGetValue(prefab, out Stack<GameObject> stack)) return;

            while (stack.Count > 0)
            {
                var obj = stack.Pop();
                if (obj != null)
                {
                    _instanceToPrefab.Remove(obj);
                    Destroy(obj);
                }
            }
            _pools.Remove(prefab);
        }

        /// Xóa toàn bộ pool (dùng khi quit hoặc load scene mới).
        public void ClearAll()
        {
            foreach (var prefab in _pools.Keys)
                ClearPool(prefab);
        }



        private bool TryGetFromPool(GameObject prefab, out GameObject obj)
        {
            obj = null;
            if (!_pools.TryGetValue(prefab, out Stack<GameObject> stack)) return false;

            while (stack.Count > 0)
            {
                obj = stack.Pop();
                if (obj != null) return true; // tìm được object hợp lệ
            }
            return false;
        }

        private System.Collections.IEnumerator ReturnAfterDelay(GameObject obj, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (obj != null) Return(obj);
        }
    }
}