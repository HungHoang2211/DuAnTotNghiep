using System;
using SimpleSurvival.Player;

namespace SimpleSurvival.Actions
{
    public class IdleAction : IAction
    {
        public ActionType Type => ActionType.Idle;

        public bool IsCompleted => false;

        public bool CanBeInterruptedBy(IAction newAction) => true;

        public event Action<IAction> Completed;

        private readonly PlayerActionController _controller;

        public IdleAction(PlayerActionController controller)
        {
            _controller = controller;
        }

        public void Init()
        {
        }

        public void Update(float deltaTime)
        {
        }

        public void Cancel()
        {
        }
    }
}