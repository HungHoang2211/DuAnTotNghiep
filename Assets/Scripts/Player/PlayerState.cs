using System;
using UnityEngine;

namespace Xyla.Player
{
    //  CÁCH DÙNG (phía Enemy):
    //    var ps = player.GetComponent<PlayerState>();
    //    ps.OnStateChanged += HandlePlayerStateChange;
    //    if (ps.Current == EPlayerState.Sneak) { ... }

    public enum EPlayerState
    {
        Idle,
        Walk,
        Run,
        Sneak,        // ngồi, đứng yên
        SneakWalk,    // ngồi, di chuyển
    }

    public class PlayerState : MonoBehaviour
    {
        public EPlayerState Current { get; private set; } = EPlayerState.Idle;
        public EPlayerState Previous { get; private set; } = EPlayerState.Idle;

        // ── Events (enemy/NPC subscribe vào đây) ────────────────────────
        /// <summary>Bắn khi state thay đổi. (previous, current)</summary>
        public event Action<EPlayerState, EPlayerState> OnStateChanged;

        public event Action<float> OnNoiseEmitted;

        public const float NoiseWalk = 0.5f;
        public const float NoiseRun = 1.0f;
        public const float NoiseSneakWalk = 0.15f;   // rất khẽ

        private TopDownMover _mover;

        private void Awake()
        {
            _mover = GetComponent<TopDownMover>();
            if (_mover == null)
                Debug.LogError("[PlayerState] Không tìm thấy TopDownMover trên cùng GameObject!", this);
        }

        private void Update()
        {
            if (_mover == null) return;
            EPlayerState next = ResolveState();
            if (next != Current) Transition(next);
        }

        private EPlayerState ResolveState()
        {
            bool sneaking = _mover.IsSneaking;
            bool moving = _mover.IsMoving;
            bool running = _mover.IsRunning;

            if (sneaking) return moving ? EPlayerState.SneakWalk : EPlayerState.Sneak;
            if (running) return EPlayerState.Run;
            if (moving) return EPlayerState.Walk;
            return EPlayerState.Idle;
        }

        private void Transition(EPlayerState next)
        {
            Previous = Current;
            Current = next;
            OnStateChanged?.Invoke(Previous, Current);
        }

        public void EmitNoise(float level) => OnNoiseEmitted?.Invoke(level);

        // ── Helper queries (enemy dùng) ──────────────────────────────────
        public bool IsSneaking => Current == EPlayerState.Sneak
                                || Current == EPlayerState.SneakWalk;
        public bool IsMoving => Current == EPlayerState.Walk
                                || Current == EPlayerState.Run
                                || Current == EPlayerState.SneakWalk;
    }
}