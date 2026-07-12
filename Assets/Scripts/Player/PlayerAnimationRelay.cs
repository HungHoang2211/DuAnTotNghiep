using SimpleSurvival.Actions;
using SimpleSurvival.Audio;
using SimpleSurvival.Items;
using UnityEngine;


namespace SimpleSurvival.Player
{
    [RequireComponent(typeof(PlayerActionController))]
    public class PlayerAnimationRelay : MonoBehaviour
    {
        [SerializeField] private PlayerSoundEmitter soundEmitter;
        [SerializeField] private WeaponVisualController weaponVisual;
        [SerializeField] private PlayerAudioController audioController;

        private PlayerActionController _actionController;

        private void Awake()
        {
            _actionController = GetComponent<PlayerActionController>();
            if (soundEmitter == null) soundEmitter = GetComponentInParent<PlayerSoundEmitter>();
            if (weaponVisual == null) weaponVisual = GetComponent<WeaponVisualController>();
            if (audioController == null) audioController = GetComponentInParent<PlayerAudioController>();
        }

        private WeaponCategory ResolveWeaponAudioCategory(SimpleSurvival.Items.ItemStack weaponStack)
        {
            if (weaponStack == null) return WeaponCategory.Fists;

            WeaponAbility weapon = weaponStack.ItemData.GetAbility<WeaponAbility>();
            if (weapon == null) return WeaponCategory.Fists;

            return weapon.Category;
        }

        public void OnAttackHit()
        {
            if (_actionController.CurrentAction is AttackAction attack)
            {
                attack.HandleHit();

                if (audioController != null)
                    audioController.PlayAttackImpact(ResolveWeaponAudioCategory(attack.WeaponStack));
            }

            bool isRangedWeapon = weaponVisual != null && weaponVisual.IsCurrentWeaponRanged();
            if (!isRangedWeapon && soundEmitter != null)
                soundEmitter.EmitAttackHit();

            if (weaponVisual != null)
                weaponVisual.PlayMuzzleFlash();
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

            if (audioController != null)
                audioController.PlayPickup();
        }

        public void OnPickupEnd()
        {
            if (_actionController.CurrentAction is PickupAction pickup)
                pickup.HandleEnd();
        }

        public void OnGatherHit()
        {
            if (_actionController.CurrentAction is GatherAction gather)
            {
                gather.HandleHit();

                if (audioController != null)
                    DispatchGatherAudio(gather);
            }

            if (soundEmitter != null)
                soundEmitter.EmitGatherHit();
        }

        private void DispatchGatherAudio(GatherAction gather)
        {
            ItemStack toolStack = gather.ToolStack;
            if (toolStack == null) return;

            ToolAbility tool = toolStack.ItemData.GetAbility<ToolAbility>();
            if (tool == null) return;

            if (tool.ToolType == ToolType.Axe)
                audioController.PlayGatherImpactAxe();
            else if (tool.ToolType == ToolType.Pickaxe)
                audioController.PlayGatherImpactPickaxe();
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

            if (audioController != null)
                audioController.PlayFootstep();
        }
    }
}