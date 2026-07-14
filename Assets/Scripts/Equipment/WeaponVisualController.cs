using UnityEngine;
using SimpleSurvival.Items;

namespace SimpleSurvival.Player
{
    public sealed class WeaponVisualController : MonoBehaviour
    {
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private PlayerToolSwapper toolSwapper;
        [SerializeField] private Transform rightHandAnchor;
        [SerializeField] private AudioSource weaponAudioSource;
        [SerializeField] private PlayerLeftHandIK leftHandIK;

        private GameObject _currentVisual;
        private WeaponVisualAnchors _currentAnchors;
        private GameObject _currentSourcePrefab;

        private void Awake()
        {
            if (playerEquipment == null) playerEquipment = GetComponentInChildren<PlayerEquipment>();
            if (toolSwapper == null) toolSwapper = GetComponent<PlayerToolSwapper>();
            if (leftHandIK == null) leftHandIK = GetComponent<PlayerLeftHandIK>();
        }

        private void Start()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged += HandleSlotChanged;

            if (toolSwapper != null)
                toolSwapper.OnToolVisualStateChanged += Rebuild;

            Rebuild();
        }

        private void OnDestroy()
        {
            if (playerEquipment != null && playerEquipment.System != null)
                playerEquipment.System.OnSlotChanged -= HandleSlotChanged;

            if (toolSwapper != null)
                toolSwapper.OnToolVisualStateChanged -= Rebuild;
        }

        private void HandleSlotChanged(EquipSlot slot, int slotIndex, ItemStack stack)
        {
            if (slot != EquipSlot.Weapon) return;
            Rebuild();
        }

        private void Rebuild()
        {
            GameObject targetPrefab = ResolveTargetPrefab();
            if (targetPrefab == _currentSourcePrefab) return;

            DespawnCurrent();
            if (targetPrefab == null) return;

            _currentVisual = Instantiate(targetPrefab, rightHandAnchor, false);
            _currentAnchors = _currentVisual.GetComponent<WeaponVisualAnchors>();
            _currentSourcePrefab = targetPrefab;

            UpdateLeftHandIK();
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
            if (leftHandIK == null) return;

            Transform target0 = _currentAnchors != null ? _currentAnchors.LeftHand0TargetIK : null;
            Transform target1 = _currentAnchors != null ? _currentAnchors.LeftHand1TargetIK : null;
            leftHandIK.SetTargets(target0, target1);
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