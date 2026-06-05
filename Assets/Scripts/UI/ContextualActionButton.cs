using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimpleSurvival.Items
{
    public sealed class ContextualActionButton : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerInteractor _interactor;
        [SerializeField] private Button _button;
        [SerializeField] private TMP_Text _label;

        [Header("Labels")]
        [SerializeField] private string _attackLabel = "Attack";
        [SerializeField] private string _harvestLabel = "Harvest";

        [Header("Icons (optional - them sprite sau)")]
        [SerializeField] private Image _icon;
        [SerializeField] private Sprite _attackSprite;
        [SerializeField] private Sprite _harvestSprite;

        private bool _isHarvestMode;

        private void OnEnable() => _button.onClick.AddListener(HandleClick);
        private void OnDisable() => _button.onClick.RemoveListener(HandleClick);

        private void Update()
        {
            bool should = _interactor.HasNearbyHarvestable;
            if (should == _isHarvestMode) return;
            _isHarvestMode = should;
            Refresh();
        }

        private void HandleClick()
        {
            if (_isHarvestMode) _interactor.OnInteractButton();
            else _interactor.OnAttackButton();
        }

        private void Refresh()
        {
            if (_label != null)
                _label.text = _isHarvestMode ? _harvestLabel : _attackLabel;
            if (_icon != null && _attackSprite != null && _harvestSprite != null)
                _icon.sprite = _isHarvestMode ? _harvestSprite : _attackSprite;
        }
    }
}
