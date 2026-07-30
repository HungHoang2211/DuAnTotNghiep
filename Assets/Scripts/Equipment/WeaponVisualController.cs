using UnityEngine;
using SimpleSurvival.Actions;
using SimpleSurvival.Items;

namespace SimpleSurvival.Player
{
    public sealed class WeaponVisualController : MonoBehaviour
    {
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private PlayerToolSwapper toolSwapper;
        [SerializeField] private PlayerActionController actionController;
        [SerializeField] private Transform rightHandAnchor;
        [SerializeField] private AudioSource weaponAudioSource;
        [SerializeField] private PlayerLeftHandIK leftHandIK;
        [SerializeField] private string weaponVisualLayerName = "PlayerWeapon";
        [SerializeField] private bool useFixedWeaponLayer = true;

        private GameObject _currentVisual;
        private WeaponVisualAnchors _currentAnchors;
        private GameObject _currentSourcePrefab;

        private Transform _moveTarget0;
        private Transform _moveTarget1;
        private Transform _attackTarget0;
        private Transform _attackTarget1;

        private void Awake()
        {
            if (playerEquipment == null) playerEquipment = GetComponentInChildren<PlayerEquipment>();
            if (toolSwapper == null) toolSwapper = GetComponent<PlayerToolSwapper>();
            if (actionController == null) actionController = GetComponent<PlayerActionController>();
            if (leftHandIK == null) leftHandIK = GetComponent<PlayerLeftHandIK>();
        }

        private void Start()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged += HandleSlotChanged;

            if (toolSwapper != null)
                toolSwapper.OnToolVisualStateChanged += Rebuild;

            if (actionController != null)
                actionController.OnActionChanged += HandleActionChanged;

            Rebuild();
        }

        private void OnDestroy()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged -= HandleSlotChanged;

            if (toolSwapper != null)
                toolSwapper.OnToolVisualStateChanged -= Rebuild;

            if (actionController != null)
                actionController.OnActionChanged -= HandleActionChanged;
        }

        private void HandleSlotChanged(EquipSlot slot, int slotIndex, ItemStack stack)
        {
            if (slot != EquipSlot.Weapon) return;
            Rebuild();
        }

        private void HandleActionChanged(IAction oldAction, IAction newAction)
        {
            if (leftHandIK == null) return;

            switch (newAction.Type)
            {
                case ActionType.Attack:
                case ActionType.Gather:
                    leftHandIK.SetTargets(_attackTarget0, _attackTarget1);
                    break;

                case ActionType.Move:
                case ActionType.Idle:
                    leftHandIK.SetTargets(_moveTarget0, _moveTarget1);
                    break;

                default:
                    leftHandIK.SetTargets(null, null);
                    break;
            }
        }

        private void Rebuild()
        {
            GameObject targetPrefab = ResolveTargetPrefab();
            if (targetPrefab == _currentSourcePrefab) return;

            DespawnCurrent();
            if (targetPrefab == null) return;

            _currentVisual = Instantiate(targetPrefab, rightHandAnchor, false);
            ApplyLayerRecursively(_currentVisual, ResolveWeaponVisualLayer());

            _currentAnchors = _currentVisual.GetComponent<WeaponVisualAnchors>();
            _currentSourcePrefab = targetPrefab;

            UpdateLeftHandIK();
        }

        private static void ApplyLayerRecursively(GameObject target, int layer)
        {
            target.layer = layer;
            foreach (Transform child in target.transform)
                ApplyLayerRecursively(child.gameObject, layer);
        }
        private int ResolveWeaponVisualLayer()
        {
            if (!useFixedWeaponLayer)
                return rightHandAnchor.gameObject.layer;

            int layer = LayerMask.NameToLayer(weaponVisualLayerName);
            return layer >= 0 ? layer : rightHandAnchor.gameObject.layer;
        }

        private GameObject ResolveTargetPrefab()
        {
            if (toolSwapper != null && toolSwapper.IsSwapped)
                return toolSwapper.CurrentTool != null ? toolSwapper.CurrentTool.ToolVisualPrefab : null;

            ItemStack stack = playerEquipment.System.GetSlot(EquipSlot.Weapon, 0);
            WeaponAbility weapon = stack?.ItemData.GetAbility<WeaponAbility>();
            return weapon != null ? weapon.WeaponVisualPrefab : null;
        }

        private void DespawnCurrent()
        {
            if (_currentVisual != null) Destroy(_currentVisual);
            _currentVisual = null;
            _currentAnchors = null;
            _currentSourcePrefab = null;

            UpdateLeftHandIK();
        }

        private void UpdateLeftHandIK()
        {
            if (_currentAnchors != null)
            {
                _moveTarget0 = _currentAnchors.UseLeftHandIKOnMove ? _currentAnchors.LeftHand0TargetIK : null;
                _moveTarget1 = _currentAnchors.UseLeftHandIKOnMove ? _currentAnchors.LeftHand1TargetIK : null;
                _attackTarget0 = _currentAnchors.UseLeftHandIKOnAttack ? _currentAnchors.LeftHand0TargetIK : null;
                _attackTarget1 = _currentAnchors.UseLeftHandIKOnAttack ? _currentAnchors.LeftHand1TargetIK : null;
            }
            else
            {
                _moveTarget0 = null;
                _moveTarget1 = null;
                _attackTarget0 = null;
                _attackTarget1 = null;
            }

            leftHandIK?.SetTargets(_moveTarget0, _moveTarget1);
        }

        public bool IsCurrentWeaponRanged()
        {
            return _currentAnchors != null && _currentAnchors.IsRanged;
        }

        public void PlayMuzzleFlash()
        {
            if (_currentAnchors == null || !_currentAnchors.IsRanged) return;

            _currentAnchors.MuzzleFlashParticles.Play(true);

            if (_currentAnchors.ShellCasingParticles != null)
                _currentAnchors.ShellCasingParticles.Emit(1);

            if (weaponAudioSource != null && _currentAnchors.FireSfx != null)
                weaponAudioSource.PlayOneShot(_currentAnchors.FireSfx);
        }
    }
}