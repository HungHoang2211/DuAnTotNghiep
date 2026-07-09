using System;
using UnityEngine;
using SimpleSurvival.AI;

namespace SimpleSurvival.Targets
{
    public sealed class WitchEventTrap : TargetableBase
    {
        [Header("Điều kiện mở khoá")]
        [Tooltip("Chỉ tương tác được khi encounter này đã Cleared")]
        [SerializeField] private WitchEventEncounter encounter;

        [Header("Hiệu ứng khi kích hoạt")]
        [Tooltip("Prefab gộp 2 effect nổ (1 cha chứa 2 con) — chỉ cần Instantiate 1 lần")]
        [SerializeField] private GameObject effectPrefab;
        [Tooltip("Điểm spawn effect, để trống = dùng transform gốc")]
        [SerializeField] private Transform effectSpawnPoint;
        [SerializeField] private float effectLifetime = 5f;

        [Header("Sau khi kích hoạt")]
        [Tooltip("Nếu true: huỷ luôn GameObject trap sau khi hiệu ứng chạy xong")]
        [SerializeField] private bool destroyAfterTrigger = false;

        [Header("Kích hoạt")]
        [Tooltip("Thời gian giữ nút để kích hoạt bẫy (giống Unlock Duration của LootContainer)")]
        [SerializeField] private float triggerDuration = 3f;

        [Header("Boss Spawn")]
        [Tooltip("Spawn point sẽ spawn ZombieWitch khi bẫy được kích hoạt. " +
         "Spawn point này KHÔNG nằm trong danh sách của WitchEventEncounter " +
         "(vì nó chỉ spawn SAU khi trap trigger, không phải điều kiện để mở trap).")]
        [SerializeField] private EnemySpawnPoint witchSpawnPoint;

        public float TriggerDuration => triggerDuration;

        public override TargetType Type => TargetType.WitchEvent;

        public bool HasTriggered { get; private set; }
        public bool IsEncounterCleared => encounter == null || encounter.IsCleared;

        public event Action<WitchEventTrap> OnTriggered;

        public override bool CanBeTargeted()
        {
            if (!isActiveAndEnabled) return false;
            if (HasTriggered) return false;
            if (!IsEncounterCleared) return false;
            return true;
        }

        public void Trigger()
        {
            if (HasTriggered || !IsEncounterCleared) return;
            HasTriggered = true;

            SpawnEffect();
            SpawnWitch();
            OnTriggered?.Invoke(this);

            // Ẩn khỏi target zone / marker ngay lập tức
            FireOnDestroyed();

            if (destroyAfterTrigger)
                Destroy(gameObject, effectLifetime);
        }

        private void SpawnWitch()
        {
            if (witchSpawnPoint == null) return;
            witchSpawnPoint.Spawn();
        }

        private void SpawnEffect()
        {
            if (effectPrefab == null) return;
            Transform origin = effectSpawnPoint != null ? effectSpawnPoint : transform;
            Instantiate(effectPrefab, origin.position, origin.rotation);
        }
    }
}