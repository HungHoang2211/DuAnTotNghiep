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
    }
}