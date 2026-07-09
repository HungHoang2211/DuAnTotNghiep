using System;
using UnityEngine;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Actions
{
    public class WitchEventAction : IAction
    {
        private static readonly int ParamUnlock = Animator.StringToHash("Unlock");
        private static readonly int ParamUnlockEnd = Animator.StringToHash("UnlockEnd");

        public ActionType Type => ActionType.Use;
        public bool IsCompleted { get; private set; }
        public event Action<IAction> Completed;

        private readonly PlayerActionController _controller;
        private readonly Animator _animator;
        private readonly WitchEventTrap _target;
        private bool _progressStarted;
        private bool _ended;

        public WitchEventAction(
            PlayerActionController controller,
            Animator animator,
            WitchEventTrap target)
        {
            _controller = controller;
            _animator = animator;
            _target = target;
        }

        public bool CanBeInterruptedBy(IAction newAction) => true;

        public void Init()
        {
            _controller.CancelSneak();
            FacingTarget();
            if (_animator != null) _animator.SetTrigger(ParamUnlock);

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
                    _target.TriggerDuration,
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
                _target.Trigger();

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