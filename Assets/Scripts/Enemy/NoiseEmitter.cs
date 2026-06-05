using UnityEngine;
using Xyla.Player;

/// <summary>
/// Gắn lên Player cùng với FootstepPlayer.
/// Tự đọc TopDownMover để biết player đang Walk/Run/Sneak
/// và phát noise mỗi khi FootstepPlayer phát tiếng bước chân.
///
/// SETUP:
///   1. Gắn component này lên Player GameObject.
///   2. Kéo TopDownMover vào _mover (hoặc tự tìm nếu cùng GameObject).
///   3. Chỉnh _stepInterval khớp với interval nhỏ nhất của FootstepPlayer (run = 0.28s).
///   4. Không cần sửa bất kỳ script Player nào.
/// </summary>
public class NoiseEmitter : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Kéo TopDownMover vào đây. Nếu bỏ trống sẽ tự tìm trên cùng GameObject.")]
    [SerializeField] private TopDownMover _mover;

    [Header("Noise Levels")]
    [Tooltip("Mức noise khi Walk. Wolf/Deer dùng để so ngưỡng.")]
    [SerializeField] private float _noiseWalk = 1.0f;
    [Tooltip("Mức noise khi Run.")]
    [SerializeField] private float _noiseRun = 2.0f;
    [Tooltip("Mức noise khi Sneak — phải thấp hơn ngưỡng của Wolf/Deer để không bị phát hiện.")]
    [SerializeField] private float _noiseSneakWalk = 0.2f;

    [Header("Timing")]
    [Tooltip("Bao lâu noise còn hiệu lực sau mỗi bước chân (giây). " +
             "Đặt >= runStepInterval của FootstepPlayer (0.28s) để không bị miss.")]
    [SerializeField] private float _noiseDuration = 0.35f;

    // ── Public API — Wolf/Deer đọc ──────────────────────
    /// <summary>Mức noise của bước chân gần nhất.</summary>
    public float LastNoiseLevel { get; private set; }

    /// <summary>Noise còn hiệu lực nếu player vừa bước chân trong _noiseDuration giây.</summary>
    public bool IsActive => Time.time - _lastNoiseTime <= _noiseDuration;

    // ── Internal ────────────────────────────────────────
    private float _lastNoiseTime = -999f;
    private float _nextEmitTime = 0f;

    // Sync interval với FootstepPlayer (nhỏ nhất = runStepInterval)
    private float StepInterval => _mover == null ? 0.28f
        : _mover.IsSneaking ? 0.55f
        : _mover.IsRunning ? 0.28f
        : 0.45f;

    private void Awake()
    {
        if (_mover == null)
            _mover = GetComponent<TopDownMover>();
    }

    private void Update()
    {
        if (_mover == null) return;

        // Chỉ emit khi player đang di chuyển
        if (!_mover.IsMoving)
        {
            _nextEmitTime = Time.time; // reset để emit ngay khi di chuyển lại
            return;
        }

        if (Time.time < _nextEmitTime) return;

        // Phát noise theo trạng thái hiện tại
        float level = _mover.IsSneaking ? _noiseSneakWalk
                    : _mover.IsRunning ? _noiseRun
                    : _noiseWalk;

        LastNoiseLevel = level;
        _lastNoiseTime = Time.time;
        _nextEmitTime = Time.time + StepInterval;
    }
}