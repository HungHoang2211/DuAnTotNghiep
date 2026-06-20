using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Loot
{
    public sealed class LootContainer : TargetableBase
    {
        [Header("Display")]
        [Tooltip("Optional. Overrides the LootTable's name. Leave empty to use table name.")]
        [SerializeField] private string displayNameOverride;

        [Header("Use Anchor")]
        [Tooltip("Optional. If set, distance is computed against this transform instead of root.")]
        [SerializeField] private Transform useTransform;

        [Header("Capacity")]
        [Tooltip("Optional. Overrides LootTable's slot count. 0 = use table value or fallback.")]
        [SerializeField] private int slotCountOverride = 0;
        [Tooltip("Used when no LootTable and no override.")]
        [SerializeField] private int fallbackSlotCount = 12;

        [Header("Loot Source")]
        [SerializeField] private LootTable lootTable;
        [SerializeField] private List<LootEntry> staticItems = new List<LootEntry>();

        [Header("Persistence")]
        [SerializeField] private bool persistWhenEmpty = true;
        [SerializeField] private float despawnDelayWhenEmpty = 2f;

        [Header("Decay")]
        [SerializeField] private float decayTimer = 0f;

        [Header("Open Animation (Optional)")]
        [Tooltip("Optional. Transform to rotate when container opens. Null for no animation.")]
        [SerializeField] private Transform openRotationTarget;
        [SerializeField] private Vector3 openRotation;

        private InventorySystem _inventory;
        private float _decayElapsed;
        private bool _hasBeenOpened;
        private bool _despawned;

        public override TargetType Type => TargetType.Container;

        public override Transform Transform => useTransform != null ? useTransform : transform;

        public InventorySystem Inventory => _inventory;
        public int SlotCount => _inventory != null ? _inventory.SlotCount : ResolveSlotCount();

        public bool IsEmpty
        {
            get
            {
                if (_inventory == null) return true;
                for (int i = 0; i < _inventory.SlotCount; i++)
                    if (_inventory.GetSlot(i) != null) return false;
                return true;
            }
        }

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(displayNameOverride)) return displayNameOverride;
                if (lootTable != null && !string.IsNullOrEmpty(lootTable.DisplayName)) return lootTable.DisplayName;
                return gameObject.name;
            }
        }

        public event Action<LootContainer> OnLooted;
        public event Action<LootContainer> OnOpened;

        private void Awake()
        {
            int resolvedSlotCount = ResolveSlotCount();
            _inventory = new InventorySystem(Mathf.Max(1, resolvedSlotCount));
            InitializeItems();
            _inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        private void OnDestroy()
        {
            if (_inventory != null)
                _inventory.OnInventoryChanged -= HandleInventoryChanged;
        }

        private void Update()
        {
            if (decayTimer <= 0f || _despawned) return;

            _decayElapsed += Time.deltaTime;
            if (_decayElapsed >= decayTimer)
                Despawn();
        }

        private int ResolveSlotCount()
        {
            if (slotCountOverride > 0) return slotCountOverride;
            if (lootTable != null) return lootTable.SlotCount;
            return fallbackSlotCount;
        }

        private void InitializeItems()
        {
            List<ItemStack> rolled = new List<ItemStack>();

            if (staticItems != null && staticItems.Count > 0)
            {
                foreach (var entry in staticItems)
                {
                    ItemStack stack = entry.Roll();
                    if (stack != null) rolled.Add(stack);
                }
            }
            else if (lootTable != null)
            {
                rolled.AddRange(lootTable.Roll());
            }

            int capacity = _inventory.SlotCount;

            if (rolled.Count > capacity)
            {
                Debug.LogWarning($"[LootContainer:{name}] Rolled {rolled.Count} items but only {capacity} slots. {rolled.Count - capacity} item(s) dropped.");
                rolled.RemoveRange(capacity, rolled.Count - capacity);
            }

            List<int> availableSlots = new List<int>(capacity);
            for (int i = 0; i < capacity; i++) availableSlots.Add(i);

            foreach (var stack in rolled)
            {
                int pickIndex = UnityEngine.Random.Range(0, availableSlots.Count);
                int slotIndex = availableSlots[pickIndex];
                availableSlots.RemoveAt(pickIndex);
                _inventory.SetSlot(slotIndex, stack);
            }
        }

        private void HandleInventoryChanged()
        {
            OnLooted?.Invoke(this);
            CheckEmptyAndDespawn();
        }

        public override bool CanBeTargeted()
        {
            if (!isActiveAndEnabled) return false;
            if (_despawned) return false;
            if (IsEmpty && !persistWhenEmpty) return false;
            return true;
        }

        public void Open()
        {
            if (_hasBeenOpened) return;
            _hasBeenOpened = true;

            if (openRotationTarget != null)
                openRotationTarget.localRotation = Quaternion.Euler(openRotation);

            OnOpened?.Invoke(this);
        }

        private void CheckEmptyAndDespawn()
        {
            if (IsEmpty && !persistWhenEmpty)
                Invoke(nameof(Despawn), despawnDelayWhenEmpty);
        }

        private void Despawn()
        {
            if (_despawned) return;
            _despawned = true;

            FireOnDestroyed();
            Destroy(gameObject);
        }
    }
}