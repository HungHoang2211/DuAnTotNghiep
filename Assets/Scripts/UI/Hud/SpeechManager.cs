using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Core;

namespace SimpleSurvival.UI.Hud
{
    public sealed class SpeechManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject speechHudPrefab;

        [Header("Default Offset")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 2.0f, 0f);

        private HudManager _hud;
        private readonly Dictionary<Transform, SpeechHudPopup> _active = new Dictionary<Transform, SpeechHudPopup>();

        private void Awake()
        {
            _hud = GetComponentInParent<HudManager>();
        }

        public void Show(Transform target, string text, SpeechHudType type)
        {
            Show(target, worldOffset, text, type);
        }

        public void Show(Transform target, Vector3 customOffset, string text, SpeechHudType type)
        {
            if (target == null || speechHudPrefab == null || _hud == null) return;

            if (_active.TryGetValue(target, out SpeechHudPopup existing) && existing != null)
            {
                existing.Show(target, customOffset, text, type, _hud.CanvasRect, _hud.GameCamera, _hud.UICamera);
                return;
            }

            GameObject go = ObjectPool.Instance.Get(speechHudPrefab, Vector3.zero);
            if (go == null) return;

            go.transform.SetParent(_hud.CanvasRect, false);

            SpeechHudPopup popup = go.GetComponent<SpeechHudPopup>();
            if (popup == null)
            {
                ObjectPool.Instance.Return(go);
                return;
            }

            popup.OnHidden += HandlePopupHidden;
            _active[target] = popup;
            popup.Show(target, customOffset, text, type, _hud.CanvasRect, _hud.GameCamera, _hud.UICamera);
        }

        private void HandlePopupHidden(SpeechHudPopup popup)
        {
            popup.OnHidden -= HandlePopupHidden;

            Transform keyToRemove = null;
            foreach (var kvp in _active)
            {
                if (kvp.Value == popup) { keyToRemove = kvp.Key; break; }
            }
            if (keyToRemove != null) _active.Remove(keyToRemove);

            ObjectPool.Instance.Return(popup.gameObject);
        }
    }
}