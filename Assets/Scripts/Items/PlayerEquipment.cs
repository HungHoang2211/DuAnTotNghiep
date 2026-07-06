using UnityEngine;

namespace SimpleSurvival.Items
{
    public sealed class PlayerEquipment : MonoBehaviour
    {
        private readonly EquipmentSystem _system = new EquipmentSystem();

        public EquipmentSystem System => _system;
    }
}