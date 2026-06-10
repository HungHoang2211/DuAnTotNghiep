using UnityEngine;
using SimpleSurvival.Actions;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(PlayerActionController))]
    public class PlayerAnimationRelay : MonoBehaviour
    {
        private PlayerActionController _actionController;

        private void Awake()
        {
            _actionController = GetComponent<PlayerActionController>();
        }

        public void OnAttackHit()
        {
            if (_actionController.CurrentAction is AttackAction attack)
                attack.HandleHit();
        }

        public void OnAttackEnd()
        {
            if (_actionController.CurrentAction is AttackAction attack)
                attack.HandleEnd();
        }

        public void OnPickupHit()
        {
            if (_actionController.CurrentAction is PickupAction pickup)
                pickup.HandleHit();
        }

        public void OnPickupEnd()
        {
            if (_actionController.CurrentAction is PickupAction pickup)
                pickup.HandleEnd();
        }

        public void OnGatherHit()
        {
            if (_actionController.CurrentAction is GatherAction gather)
                gather.HandleHit();
        }

        public void OnGatherEnd()
        {
            if (_actionController.CurrentAction is GatherAction gather)
                gather.HandleEnd();
        }
    }
}