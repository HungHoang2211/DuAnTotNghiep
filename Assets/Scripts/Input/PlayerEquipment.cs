using UnityEngine;

namespace SimpleSurvival.Items
{
    public sealed class PlayerEquipment : MonoBehaviour
    {
        private EquipmentSystem _system;

        public EquipmentSystem System => _system;

        private void Awake()
        {
            _system = new EquipmentSystem();
        }
    }
}