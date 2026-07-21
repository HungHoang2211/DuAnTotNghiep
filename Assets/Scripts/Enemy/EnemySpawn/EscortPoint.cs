using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Loot;

namespace SimpleSurvival.Quests
{
    /// <summary>
    /// Gắn component này vào 1 GameObject trong scene để đánh dấu điểm đích hộ tống.
    /// pointId phải khớp với escortWaypointIds trong QuestObjectiveData (type = EscortNPC).
    /// </summary>
    public sealed class EscortPoint : MonoBehaviour
    {
        [SerializeField] private string pointId;

        [Tooltip("Để trống nếu đây chỉ là điểm đi qua. Gán rương nếu muốn Emily dừng lại chờ loot xong.")]
        [SerializeField] private LootContainer lootContainer;

        private static readonly Dictionary<string, EscortPoint> _registry = new Dictionary<string, EscortPoint>();

        public string PointId => pointId;
        public Vector3 Position => transform.position;
        public LootContainer LootContainer => lootContainer;

        private void OnEnable()
        {
            if (string.IsNullOrEmpty(pointId)) return;
            _registry[pointId] = this;
        }

        private void OnDisable()
        {
            if (string.IsNullOrEmpty(pointId)) return;
            if (_registry.TryGetValue(pointId, out var current) && current == this)
                _registry.Remove(pointId);
        }

        public static EscortPoint Find(string pointId)
        {
            if (string.IsNullOrEmpty(pointId)) return null;
            return _registry.TryGetValue(pointId, out var point) ? point : null;
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}