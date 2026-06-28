using System;
using UnityEngine;
using SimpleSurvival.Player;
using SimpleSurvival.Loot;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Actions
{
    public class UnlockAction : IAction
    {
        private static readonly int ParamUnlock = Animator.StringToHash("Unlock");
        private static readonly int ParamUnlockEnd = Animator.StringToHash("UnlockEnd");

        public ActionType Type => ActionType.Use;
        public bool IsCompleted { get; private set; }
        public event Action<IAction> Completed;

        private readonly PlayerActionController _controller;
        private readonly Animator _animator;
        private readonly LootContainer _target;
        private readonly Action _onComplete;
        private readonly float _duration;
        private bool _progressStarted;
        private bool _ended;

        public UnlockAction(
            PlayerActionController controller,
            Animator animator,
            LootContainer target,
            Action onComplete)
        {
            _controller = controller;
            _animator = animator;
            _target = target;
            _onComplete = onComplete;
            _duration = target != null ? target.UnlockDuration : 0f;
        }

        public bool CanBeInterruptedBy(IAction newAction)
        {
            return true;
        }

        public void Init()
        {
            _controller.CancelSneak();
            FacingTarget();
            _animator.SetTrigger(ParamUnlock);

            if (_target == null || !_target.CanBeTargeted())
            {
                Finish();
                return;
            }

            HudManager hud = HudManager.Instance;
            if (hud != null && hud.UnlockProgress != null)
            {
                hud.UnlockProgress.Show(
                    _target.Transform,
                    _duration,
                    OnProgressComplete);
                _progressStarted = true;
            }
            else
            {
                OnProgressComplete();
            }
        }

        public void Update(float deltaTime) { }

        public void Cancel()
        {
            if (_progressStarted)
            {
                HudManager hud = HudManager.Instance;
                if (hud != null && hud.UnlockProgress != null)
                    hud.UnlockProgress.Stop();
                _progressStarted = false;
            }
            Finish();
        }

        private void OnProgressComplete()
        {
            if (_ended) return;

            if (_target != null && _target.CanBeTargeted())
            {
                _target.MarkUnlocked();
                _target.Open();
                _onComplete?.Invoke();
            }

            _progressStarted = false;
            Finish();
        }

        private void Finish()
        {
            if (_ended) return;
            _ended = true;

            if (_animator != null)
                _animator.SetTrigger(ParamUnlockEnd);

            IsCompleted = true;
            Completed?.Invoke(this);
        }

        private void FacingTarget()
        {
            if (_target == null) return;
            Vector3 toTarget = _target.Transform.position - _controller.PlayerTransform.position;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude < 0.001f) return;
            _controller.PlayerTransform.rotation = Quaternion.LookRotation(toTarget, Vector3.up);
        }
    }
}