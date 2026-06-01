using UnityEngine;

namespace Xyla.Utils
{
    /// SETUP: gắn script này vào một GameObject bất kỳ trong scene
    /// (vd một object "GameSettings" rỗng). Chỉ cần một bản trong scene.
    [DisallowMultipleComponent]
    public class FpsLimiter : MonoBehaviour
    {
        [Tooltip("Số FPS muốn khóa. Đặt 60 để test đồng nhất.")]
        [SerializeField] private int _targetFps = 60;

        [Tooltip("Hiện FPS thực tế ở góc màn hình (chỉ để kiểm tra).")]
        [SerializeField] private bool _showFpsOverlay = true;

        [Tooltip("Cỡ chữ FPS hiển thị.")]
        [SerializeField] private int _fpsFontSize = 60;

        private float _smoothedFps;

        private void Awake()
        {
            // Giữ object qua các scene để cài đặt không bị reset khi chuyển map.
            DontDestroyOnLoad(gameObject);
            ApplyFrameRate();
        }

        private void ApplyFrameRate()
        {
            // Bắt buộc tắt VSync, nếu không targetFrameRate bị bỏ qua.
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = _targetFps;
        }

        private void Update()
        {
            // Làm mượt số FPS hiển thị cho đỡ nhảy.
            float instantFps = 1f / Mathf.Max(Time.unscaledDeltaTime, 0.0001f);
            _smoothedFps = Mathf.Lerp(_smoothedFps, instantFps, 0.1f);
        }

#if UNITY_EDITOR
        // Cho phép đổi FPS ngay trong Inspector lúc đang Play.
        private void OnValidate()
        {
            if (Application.isPlaying) ApplyFrameRate();
        }
#endif

        private void OnGUI()
        {
            if (!_showFpsOverlay) return;

            var style = new GUIStyle
            {
                fontSize = _fpsFontSize,
                fontStyle = FontStyle.Bold
            };
            string text = $"FPS: {Mathf.RoundToInt(_smoothedFps)}";
            var rect = new Rect(16, 16, _fpsFontSize * 8f, _fpsFontSize * 1.6f);

            // Vẽ bóng đen phía sau cho dễ đọc trên nền sáng.
            style.normal.textColor = Color.black;
            GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);

            style.normal.textColor = Color.green;
            GUI.Label(rect, text, style);
        }
    }
}