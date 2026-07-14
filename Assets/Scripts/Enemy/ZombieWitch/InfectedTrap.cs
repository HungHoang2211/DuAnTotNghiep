using System.Collections;
using UnityEngine;
using SimpleSurvival.Targets;

namespace SimpleSurvival.VFX
{
    /// <summary>
    /// Gắn lên object chứa mesh/renderer dùng shader "SimpleSurvival/InfectedThing"
    /// của WitchEventTrap. Lắng nghe WitchEventTrap.OnTriggered (đã có sẵn) và
    /// chuyển dần màu đỏ (infected) sang đen (đã kích hoạt) qua property _DeadAmount
    /// bằng MaterialPropertyBlock (không tạo material instance mới, không ảnh hưởng
    /// các Trap khác dùng chung material).
    /// </summary>
    public class InfectedTrapVisual : MonoBehaviour
    {
        [Header("Refs")]
        [Tooltip("Trap sẽ phát OnTriggered khi bị kích hoạt")]
        [SerializeField] private WitchEventTrap trap;

        [Tooltip("Các Renderer (mesh) dùng shader InfectedThing cần đổi màu")]
        [SerializeField] private Renderer[] renderers;

        [Header("Hiệu ứng chuyển màu")]
        [Tooltip("Thời gian chuyển từ đỏ sang đen (giây)")]
        [SerializeField] private float transitionDuration = 1.5f;
        [SerializeField] private AnimationCurve transitionCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        private static readonly int DeadAmountID = Shader.PropertyToID("_DeadAmount");

        private MaterialPropertyBlock _mpb;
        private Coroutine _routine;
        private float _currentDeadAmount;

        private void Awake()
        {
            _mpb = new MaterialPropertyBlock();
        }

        private void OnEnable()
        {
            if (trap != null) trap.OnTriggered += HandleTriggered;
        }

        private void OnDisable()
        {
            if (trap != null) trap.OnTriggered -= HandleTriggered;
        }

        private void HandleTriggered(WitchEventTrap _)
        {
            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(TransitionToDead());
        }

        private IEnumerator TransitionToDead()
        {
            float startValue = _currentDeadAmount;
            float elapsed = 0f;

            // Nếu transitionDuration = 0 thì đổi màu ngay lập tức
            if (transitionDuration <= 0f)
            {
                SetDeadAmount(1f);
                yield break;
            }

            while (elapsed < transitionDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / transitionDuration);
                float curved = transitionCurve.Evaluate(t);
                SetDeadAmount(Mathf.Lerp(startValue, 1f, curved));
                yield return null;
            }

            SetDeadAmount(1f);
            _routine = null;
        }

        private void SetDeadAmount(float value)
        {
            _currentDeadAmount = value;

            if (renderers == null) return;
            foreach (var r in renderers)
            {
                if (r == null) continue;
                r.GetPropertyBlock(_mpb);
                _mpb.SetFloat(DeadAmountID, value);
                r.SetPropertyBlock(_mpb);
            }
        }
    }
}