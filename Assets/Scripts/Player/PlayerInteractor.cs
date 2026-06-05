using UnityEngine;
using Xyla.Player;
namespace SimpleSurvival.Items
{
    [RequireComponent(typeof(PlayerInventory))]
    public class PlayerInteractor : MonoBehaviour
    {
        [SerializeField] private float _interactRadius = 1.2f;
        [SerializeField] private LayerMask _harvestLayer;
        [SerializeField] private GameObject _interactButtonUI; 
        [SerializeField] private PlayerInputReader _inputReader;

        private PlayerInventory _inventory;
        private IHarvestable _nearest;

        public bool HasNearbyHarvestable => _nearest != null;

        private void Awake()
            => _inventory = GetComponent<PlayerInventory>();

        private void Update()
        {
            DetectNearest();
            if (UnityEngine.Input.GetKeyDown(KeyCode.E)) TryHarvest();
        }

        private void DetectNearest()
        {
            var hits = Physics.OverlapSphere(
                transform.position, _interactRadius, _harvestLayer);

            _nearest = null;
            foreach (var hit in hits)
                if (hit.TryGetComponent<IHarvestable>(out var h) && h.CanHarvest)
                { _nearest = h; break; }

            if (_interactButtonUI != null)
                _interactButtonUI.SetActive(_nearest != null);
        }
        private void TryHarvest()
            => _nearest?.Harvest(_inventory, skillLevel: 0);

        public void OnInteractButton() => TryHarvest();

        public void OnAttackButton() => _inputReader.OnAttackButtonDown();

#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, _interactRadius);
        }
#endif
    }
}
