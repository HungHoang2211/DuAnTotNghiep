using System;
using UnityEngine;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;
using SimpleSurvival.Items;
using SimpleSurvival.UI.Hud;

namespace SimpleSurvival.Actions
{
    public class PickupAction : IAction
    {
        private static readonly int ParamPickup = Animator.StringToHash("Pickup");

        public ActionType Type => ActionType.Use;
        public bool IsCompleted { get; private set; }
        public event Action<IAction> Completed;

        private readonly PlayerActionController _controller;
        private readonly Animator _animator;
        private readonly PlayerInventoryQueries _inventoryQueries;
        private readonly PickupTarget _target;

        private bool _pickupApplied;

        public PickupAction(
            PlayerActionController controller,
            Animator animator,
            PlayerInventoryQueries inventoryQueries,
            PickupTarget target)
        {
            _controller = controller;
            _animator = animator;
            _inventoryQueries = inventoryQueries;
            _target = target;
        }

        public bool CanBeInterruptedBy(IAction newAction)
        {
            if (newAction.Type == ActionType.Move) return false;
            return true;
        }

        public void Init()
        {
            _controller.CancelSneak();
            FacingTarget();
            _animator.SetTrigger(ParamPickup);
        }

        public void Update(float deltaTime) { }

        public void Cancel()
        {
            IsCompleted = true;
        }

        public void HandleHit()
        {
            if (_pickupApplied) return;
            _pickupApplied = true;

            if (_target == null || !_target.CanBeTargeted()) return;
            if (_inventoryQueries == null) return;

            int totalAdded = 0;

            foreach (var entry in _target.Items)
            {
                if (entry == null || entry.itemData == null || entry.quantity <= 0) continue;

                int remaining = _inventoryQueries.AddItem(entry.itemData, entry.quantity);
                int added = entry.quantity - remaining;
                totalAdded += added;

                if (added > 0)
                {
                    Debug.Log($"[Pickup] +{added} {entry.itemData.ItemName}");
                    if (FollowNotifyManager.Instance != null)
                        FollowNotifyManager.Instance.Notify($"+{added} {entry.itemData.ItemName}", SpeechHudType.Neutral);
                }

                if (remaining > 0)
                    Debug.Log($"[Pickup] Lost {remaining} {entry.itemData.ItemName} (inventory full)");
            }

            if (totalAdded == 0)
            {
                Debug.Log("[Pickup] Your inventory is full!");
                if (FollowNotifyManager.Instance != null)
                    FollowNotifyManager.Instance.Notify("Inventory full!", SpeechHudType.Bad);
                return;
            }

            // Disable collider + hide mesh (giống harvest)
            _target.OnPickedUp();
        }

        public void HandleEnd()
        {
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