using UnityEngine;
using SimpleSurvival.Actions;
using SimpleSurvival.Audio;

namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(PlayerActionController))]
    public class PlayerAnimationRelay : MonoBehaviour
    {
        [SerializeField] private PlayerSoundEmitter soundEmitter;

        private PlayerActionController _actionController;

        private void Awake()
        {
            _actionController = GetComponent<PlayerActionController>();
            if (soundEmitter == null) soundEmitter = GetComponentInParent<PlayerSoundEmitter>();
        }

        public void OnAttackHit()
        {
            if (_actionController.CurrentAction is AttackAction attack)
                attack.HandleHit();

            if (soundEmitter != null)
                soundEmitter.EmitAttackHit();
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

            if (soundEmitter != null)
                soundEmitter.EmitGatherHit();
        }

        public void OnGatherEnd()
        {
            if (_actionController.CurrentAction is GatherAction gather)
                gather.HandleEnd();
        }

        public void OnFootStep()
        {
            if (soundEmitter != null)
                soundEmitter.EmitFootstep();
        }
    }
}