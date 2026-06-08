using System;

namespace SimpleSurvival.Actions
{
    public interface IAction
    {
        ActionType Type { get; }
        bool IsCompleted { get; }
        bool CanBeInterruptedBy(IAction newAction);
        void Init();
        void Update(float deltaTime);
        void Cancel();
        event Action<IAction> Completed;
    }
}