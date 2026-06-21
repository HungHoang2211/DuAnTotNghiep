using System;
using UnityEngine;
using SimpleSurvival.Core;

namespace SimpleSurvival.UI
{
    public sealed class UnlockProgressBarManager : MonoBehaviour
    {
        public static UnlockProgressBarManager Instance { get; private set; }

        [Header("Prefab")]
        [SerializeField] private GameObject unlockProgressBarPrefab;

        [Header("References")]
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Camera uiCamera;

        [Header("Settings")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);

        private UnlockProgressBar _current;
        private Action _pendingCallback;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Show(Transform target, float duration, Action onComplete)
        {
            Stop();

            if (unlockProgressBarPrefab == null || target == null) return;

            GameObject go = ObjectPool.Instance.Get(unlockProgressBarPrefab, Vector3.zero);
            if (go == null) return;

            go.transform.SetParent(canvasRect, false);

            _current = go.GetComponent<UnlockProgressBar>();
            if (_current == null)
            {
                ObjectPool.Instance.Return(go);
                return;
            }

            _pendingCallback = onComplete;
            _current.OnComplete += HandleComplete;
            _current.Show(target, worldOffset, duration, gameCamera, uiCamera, canvasRect);
        }

        public void Stop()
        {
            if (_current == null) return;
            _current.OnComplete -= HandleComplete;
            ObjectPool.Instance.Return(_current.gameObject);
            _current = null;
            _pendingCallback = null;
        }

        private void HandleComplete()
        {
            Action callback = _pendingCallback;
            UnlockProgressBar bar = _current;

            _pendingCallback = null;
            _current = null;

            if (bar != null)
            {
                bar.OnComplete -= HandleComplete;
                ObjectPool.Instance.Return(bar.gameObject);
            }

            callback?.Invoke();
        }
    }
}