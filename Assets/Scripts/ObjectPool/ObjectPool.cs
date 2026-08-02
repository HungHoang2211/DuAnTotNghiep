using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Core
{

    public class ObjectPool : MonoBehaviour
    {
        public static ObjectPool Instance { get; private set; }

        [SerializeField] private int _maxPoolSizePerPrefab = 20;

        private readonly Dictionary<GameObject, Stack<GameObject>> _pools
            = new Dictionary<GameObject, Stack<GameObject>>();

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
                obj.SetActive(true);
                _instanceToPrefab[obj] = prefab;
            }

            obj.SendMessage("OnSpawnFromPool", SendMessageOptions.DontRequireReceiver);
            return obj;
        }

        public GameObject Get(GameObject prefab, Vector3 position)
            => Get(prefab, position, Quaternion.identity);

        public void Return(GameObject obj)
        {
            if (obj == null) return;

            obj.SendMessage("OnReturnToPool", SendMessageOptions.DontRequireReceiver);

            if (!_instanceToPrefab.TryGetValue(obj, out GameObject prefab))
            {
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
                _instanceToPrefab.Remove(obj);
                Destroy(obj);
                return;
            }

            obj.SetActive(false);
            stack.Push(obj);
        }

        public void ReturnDelayed(GameObject obj, float delay)
        {
            if (obj == null) return;
            StartCoroutine(ReturnAfterDelay(obj, delay));
        }

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
                if (obj != null) return true;
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