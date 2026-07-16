using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Stats;
using SimpleSurvival.Items;
using SimpleSurvival.Loot;
using SimpleSurvival.SaveLoad;
using SimpleSurvival.World;

namespace SimpleSurvival.Player
{
    public sealed class PlayerDeathHandler : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private PlayerStats playerStats;
        [SerializeField] private PlayerInventory playerInventory;
        [SerializeField] private PlayerEquipment playerEquipment;
        [SerializeField] private DeathDialogController deathDialog;
        [SerializeField] private PlayerRagdollController ragdollController;

        [Header("Corpse")]
        [SerializeField] private Transform spawnPoint;
        private readonly List<Transform> _activeCorpses = new List<Transform>();
        private const float CorpseMinSeparation = 2f;
        private void Awake()
        {
            if (playerStats == null)
                playerStats = GetComponentInChildren<PlayerStats>();
            if (playerInventory == null)
                playerInventory = GetComponentInChildren<PlayerInventory>();
            if (playerEquipment == null)
                playerEquipment = GetComponentInChildren<PlayerEquipment>();
            if (ragdollController == null)
                ragdollController = GetComponentInChildren<PlayerRagdollController>();
        }

        private void Start()
        {
            if (playerStats != null)
                playerStats.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            if (playerStats != null)
                playerStats.OnDeath -= HandleDeath;
        }

        private void HandleDeath(GameObject source)
        {
            List<ItemStack> droppedItems = CollectAndClearAllItems();
            SpawnCorpse(droppedItems);

            string killerName = ResolveKillerName(source);
            if (deathDialog != null)
                deathDialog.Show(killerName);
        }

        public void Revive()
        {
            Debug.Log("[PlayerDeathHandler] Revive() called");
            if (playerStats == null) return;
            playerStats.Revive();

            if (ragdollController != null)
                ragdollController.ResetRagdoll();

            if (deathDialog != null)
                deathDialog.Hide();

            if (MapTransitionController.Instance != null && MapLoader.Instance != null)
            {
                string startMap = MapTransitionController.Instance.StartMapScene;
                if (MapLoader.Instance.CurrentMapScene == startMap)
                {
                    MapLoader.Instance.RepositionToSpawn();
                    SaveService.Instance?.Save();
                }
                else
                {
                    MapTransitionController.Instance.GoToMap(startMap);
                }
            }
        }
        private List<ItemStack> CollectAndClearAllItems()
        {
            List<ItemStack> collected = new List<ItemStack>();

            CollectAndClearInventory(playerInventory.Pockets, collected);
            if (playerInventory.Backpack != null)
                CollectAndClearInventory(playerInventory.Backpack, collected);

            foreach (EquipSlot slot in playerEquipment.System.Slots)
            {
                int slotCount = playerEquipment.System.SlotCount(slot);
                for (int i = 0; i < slotCount; i++)
                {
                    ItemStack stack = playerEquipment.System.GetSlot(slot, i);
                    if (stack == null) continue;

                    collected.Add(stack.Clone());
                    playerEquipment.System.SetSlotDirect(slot, i, null);
                }
            }

            playerInventory.ResizeBackpack(0);

            return collected;
        }

        private static void CollectAndClearInventory(InventorySystem inventory, List<ItemStack> collected)
        {
            for (int i = 0; i < inventory.SlotCount; i++)
            {
                ItemStack stack = inventory.GetSlot(i);
                if (stack == null) continue;

                collected.Add(stack.Clone());
                inventory.SetSlot(i, null);
            }
        }

        private void SpawnCorpse(List<ItemStack> items)
        {
            if (items.Count == 0)
                return;
            if (CorpseSaveRegistry.Instance == null)
                return;

            string homeMap = MapLoader.Instance != null ? MapLoader.Instance.CurrentMapScene : null;
            if (string.IsNullOrEmpty(homeMap))
                return;

            Vector3 desired = spawnPoint != null ? spawnPoint.position : transform.position;
            Vector3 position = ResolveCorpseSpawnPosition(desired);

            GameObject corpseObj = CorpseSaveRegistry.Instance.SpawnCorpseObject(homeMap, position, items);
            _activeCorpses.Add(corpseObj.transform);
        }

        private Vector3 ResolveCorpseSpawnPosition(Vector3 desired)
        {
            _activeCorpses.RemoveAll(c => c == null);

            Vector3 candidate = desired;
            int tries = 8;
            while (tries-- > 0 && IsPositionOccupied(candidate))
            {
                float angle = Random.Range(0f, 360f);
                candidate = desired + Quaternion.Euler(0f, angle, 0f) * Vector3.forward * CorpseMinSeparation;
            }
            return candidate;
        }

        private bool IsPositionOccupied(Vector3 pos)
        {
            foreach (Transform c in _activeCorpses)
            {
                if (c == null) continue;
                if (Vector3.Distance(c.position, pos) < CorpseMinSeparation)
                    return true;
            }
            return false;
        }

        private static string ResolveKillerName(GameObject source)
        {
            if (source == null) return null;

            EnemyStats enemyStats = source.GetComponentInParent<EnemyStats>();
            if (enemyStats == null || enemyStats.EnemyConfig == null) return null;

            return enemyStats.EnemyConfig.DisplayName;
        }
    }
}