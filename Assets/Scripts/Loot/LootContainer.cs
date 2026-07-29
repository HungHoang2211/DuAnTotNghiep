using System;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Items;
using SimpleSurvival.SaveLoad;
using SimpleSurvival.Targets;

namespace SimpleSurvival.Loot
{
    public sealed class LootContainer : TargetableBase, IUnlockable
    {
        [Header("Display")]
        [Tooltip("Optional. Overrides the LootTable's name. Leave empty to use table name.")]
        [SerializeField] private string displayNameOverride;
        [Tooltip("Optional. Overrides the LootTable's icon. Leave null to use table icon.")]
        [SerializeField] private Sprite displayIconOverride;

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

        [Header("Unlock")]
        [Tooltip("Time required to unlock this container. 0 = no unlock needed (open immediately).")]
        [SerializeField] private float unlockDuration = 0f;

        [Header("Persistence")]
        [SerializeField] private bool persistWhenEmpty = true;
        [SerializeField] private float despawnDelayWhenEmpty = 2f;
        [Tooltip("True = nhớ trạng thái loot xuyên suốt các lần chơi (không tự roll lại), kể cả ở map farm. Cần Container Id.")]
        [SerializeField] private bool persistAcrossSessions = false;
        [Tooltip("Bắt buộc điền tay, duy nhất, nếu Persist Across Sessions = true.")]
        [SerializeField] private string containerId;

        [Header("Decay")]
        [SerializeField] private float decayTimer = 0f;

        [Header("Open Animation (Optional)")]
        [SerializeField] private Transform openRotationTarget;
        [SerializeField] private Vector3 openRotation;

        [Header("Runtime Init")]
        [Tooltip("Nếu true, container không tự init ở Awake. Phải gọi InitializeRuntime sau (vd từ EnemyCorpseHandler).")]
        [SerializeField] private bool deferInitialization = false;

        [Header("Lock")]
        [SerializeField] private bool startLocked = false;

        private bool _isLocked;
        public bool IsLocked => _isLocked;

        public void SetLocked(bool locked)
        {
            _isLocked = locked;
        }

        private InventorySystem _inventory;
        private float _decayElapsed;
        private bool _hasBeenOpened;
        private bool _isUnlocked;
        private bool _despawned;
        private bool _isInitialized;

        public override TargetType Type => TargetType.Container;
        public override Transform Transform => useTransform != null ? useTransform : transform;

        public InventorySystem Inventory => _inventory;
        public int SlotCount => _inventory != null ? _inventory.SlotCount : ResolveSlotCount();
        public float UnlockDuration => unlockDuration;
        public bool IsUnlocked => _isUnlocked || unlockDuration <= 0f;
        public string ContainerId => containerId;

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

        public Sprite DisplayIcon
        {
            get
            {
                if (displayIconOverride != null) return displayIconOverride;
                if (lootTable != null && lootTable.DisplayIcon != null) return lootTable.DisplayIcon;
                return null;
            }
        }

        public event Action<LootContainer> OnLooted;
        public event Action<LootContainer> OnOpened;
        public event Action<LootContainer> OnUnlocked;

        private void Awake()
        {
            _isLocked = startLocked;

            if (persistAcrossSessions)
            {
                ContainerSaveRegistry.Instance?.InitializePersistentContainer(this);
                return;
            }
            if (deferInitialization) return;
            InitializeInternal();
        }

        public void InitializeRuntime(LootTable runtimeTable, float runtimeUnlockDuration = 0f)
        {
            if (_isInitialized) return;
            lootTable = runtimeTable;
            unlockDuration = runtimeUnlockDuration;
            InitializeInternal();
        }

        public void InitializeDefault()
        {
            InitializeInternal();
        }

        public void InitializeEmpty()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            int resolvedSlotCount = ResolveSlotCount();
            _inventory = new InventorySystem(Mathf.Max(1, resolvedSlotCount));
            _inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        private void InitializeInternal()
        {
            if (_isInitialized) return;
            _isInitialized = true;

            int resolvedSlotCount = ResolveSlotCount();
            _inventory = new InventorySystem(Mathf.Max(1, resolvedSlotCount));
            InitializeItems();
            _inventory.OnInventoryChanged += HandleInventoryChanged;
        }

        protected override void OnDestroy()
        {
            if (_inventory != null)
                _inventory.OnInventoryChanged -= HandleInventoryChanged;
            base.OnDestroy();
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

            for (int i = 0; i < rolled.Count; i++)
                _inventory.SetSlot(i, rolled[i]);
        }
        public void InitializeRuntimeWithStacks(List<ItemStack> stacks, float runtimeUnlockDuration = 0f)
        {
            if (_isInitialized) return;
            _isInitialized = true;

            unlockDuration = runtimeUnlockDuration;
            int count = stacks != null ? stacks.Count : 0;
            _inventory = new InventorySystem(Mathf.Max(1, count));

            if (stacks != null)
            {
                for (int i = 0; i < stacks.Count; i++)
                    _inventory.SetSlot(i, stacks[i]);
            }

            _inventory.OnInventoryChanged += HandleInventoryChanged;
        }
        private void HandleInventoryChanged()
        {
            OnLooted?.Invoke(this);
            CheckEmptyAndDespawn();
        }

        public override bool CanBeTargeted()
        {
            if (_isLocked) return false;
            if (!isActiveAndEnabled) return false;
            if (_despawned) return false;
            if (!_isInitialized) return false;
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

        public void MarkUnlocked()
        {
            if (_isUnlocked) return;
            _isUnlocked = true;
            OnUnlocked?.Invoke(this);
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