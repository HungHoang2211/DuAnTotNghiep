using UnityEngine;
using SimpleSurvival.Core;

namespace SimpleSurvival.UI.Hud
{
    public sealed class HpHudManager : MonoBehaviour
    {
        [Header("Prefab")]
        [SerializeField] private GameObject hpHudPrefab;

        [Header("Default Offset")]
        [Tooltip("World space offset trên đầu entity. Mặc định khoảng vai/giữa thân.")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.2f, 0f);

        private HudManager _hud;

        private void Awake()
        {
            _hud = GetComponentInParent<HudManager>();
        }

        public void Spawn(Transform followTarget, float amount, HpHudType type)
        {
            Spawn(followTarget, worldOffset, amount, type);
        }

        public void Spawn(Transform followTarget, Vector3 customOffset, float amount, HpHudType type)
        {
            Debug.Log($"[HpHudManager] Spawn called, target={followTarget?.name}, amount={amount}, type={type}");

            if (followTarget == null || hpHudPrefab == null || _hud == null)
            {
                Debug.Log($"[HpHudManager] Early return: followTarget={followTarget != null}, prefab={hpHudPrefab != null}, hud={_hud != null}");
                return;
            }

            GameObject go = ObjectPool.Instance.Get(hpHudPrefab, Vector3.zero);
            Debug.Log($"[HpHudManager] Pool returned go={go?.name}, active={go?.activeSelf}");

            if (go == null) return;

            go.transform.SetParent(_hud.CanvasRect, false);

            HpHudPopup popup = go.GetComponent<HpHudPopup>();
            if (popup == null)
            {
                Debug.Log($"[HpHudManager] HpHudPopup null!");
                ObjectPool.Instance.Return(go);
                return;
            }

            string text = FormatAmount(amount, type);
            popup.Show(followTarget, customOffset, _hud.CanvasRect, _hud.GameCamera, _hud.UICamera, text, type);
            Debug.Log($"[HpHudManager] Show called, text={text}");
        }

        private string FormatAmount(float amount, HpHudType type)
        {
            int rounded = Mathf.CeilToInt(Mathf.Abs(amount));
            return type == HpHudType.Heal ? $"+{rounded}" : $"-{rounded}";
        }
    }
}