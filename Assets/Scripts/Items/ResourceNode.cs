using UnityEngine;

namespace SimpleSurvival.Items
{
    public class ResourceNode : MonoBehaviour, IHarvestable
    {
        [SerializeField] private ResourceNodeSO _data;
        [SerializeField] private GameObject _readyVisual;    // sprite day
        [SerializeField] private GameObject _depletedVisual; // sprite da harvest

        private bool _harvested;

        public bool CanHarvest => !_harvested && _data != null;

        public void Harvest(PlayerInventory playerInventory, int skillLevel = 0)
        {
            if (!CanHarvest) return;

            foreach (var (item, amount) in _data.Roll(skillLevel))
            {
                // Bỏ vào Pockets trước, tràn thì vào Backpack
                int overflow = playerInventory.Pockets.AddItem(item, amount);
                if (overflow > 0 && playerInventory.Backpack != null)
                    playerInventory.Backpack.AddItem(item, overflow);
                // TODO: hiện thông báo "Tui day" neu overflow con lai > 0
            }

            SetDepleted(true);
            Invoke(nameof(Respawn), _data.RespawnTime);
        }

        private void Respawn() => SetDepleted(false);

        private void SetDepleted(bool depleted)
        {
            _harvested = depleted;
            if (_readyVisual) _readyVisual.SetActive(!depleted);
            if (_depletedVisual) _depletedVisual.SetActive(depleted);
        }
    }
}
