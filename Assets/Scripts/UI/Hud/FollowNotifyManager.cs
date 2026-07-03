using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.UI.Hud
{
    public sealed class FollowNotifyManager : MonoBehaviour
    {
        public static FollowNotifyManager Instance { get; private set; }

        [Header("Slots")]
        [SerializeField] private FollowNotifyPopup slot1;
        [SerializeField] private FollowNotifyPopup slot2;

        [Header("Settings")]
        [SerializeField] private float pollInterval = 0.5f;
        [SerializeField] private float limitedCooldown = 2f;

        private readonly Queue<(string text, SpeechHudType type)> _queue = new Queue<(string, SpeechHudType)>();
        private bool _blocked;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnEnable()
        {
            StartCoroutine(PollRoutine());
        }

        private IEnumerator PollRoutine()
        {
            while (true)
            {
                if (_queue.Count > 0 && (!slot1.IsAnimated || !slot2.IsAnimated))
                {
                    var notify = _queue.Dequeue();
                    ShowOnFreeSlot(notify.text, notify.type);
                }
                yield return new WaitForSeconds(pollInterval);
            }
        }

        private void ShowOnFreeSlot(string text, SpeechHudType type)
        {
            if (!slot1.IsAnimated) slot1.Show(text, type);
            else if (!slot2.IsAnimated) slot2.Show(text, type);
        }

        public void Notify(string text, SpeechHudType type)
        {
            _queue.Enqueue((text, type));
        }

        public void NotifyLimited(string text, SpeechHudType type)
        {
            if (_blocked) return;
            _blocked = true;
            StartCoroutine(BlockTimer());
            _queue.Enqueue((text, type));
        }

        private IEnumerator BlockTimer()
        {
            yield return new WaitForSeconds(limitedCooldown);
            _blocked = false;
        }
    }
}