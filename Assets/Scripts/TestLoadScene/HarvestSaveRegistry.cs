using System.Collections.Generic;
using SimpleSurvival.SaveLoad;
using SimpleSurvival.Targets;
using UnityEngine;

namespace SimpleSurvival.Stats
{
    public sealed class HarvestSaveRegistry : MonoBehaviour
    {
        public static HarvestSaveRegistry Instance { get; private set; }

        private readonly List<HarvestStats> active = new List<HarvestStats>();
        private readonly List<string> consumedPickupIds = new List<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // ---- Harvest node (Tree/Rock/Ore) ----

        public void InitializePersistentNode(HarvestStats node)
        {
            if (string.IsNullOrWhiteSpace(node.NodeId))
            {
                Debug.LogWarning($"[HarvestSaveRegistry] '{node.name}' bật Persist Across Sessions nhưng chưa có Node Id — roll mặc định.");
                active.Add(node);
                node.RestoreState(node.MaxHP, false);
                return;
            }

            HarvestNodeData saved = SaveService.Instance != null
                ? SaveService.Instance.GetHarvestNodeData(node.NodeId)
                : null;

            if (saved != null && saved.isDepleted)
            {
                Destroy(node.gameObject);
                return;
            }

            active.Add(node);
            node.RestoreState(saved != null ? saved.hp : node.MaxHP, false);
        }

        public List<HarvestNodeData> Capture()
        {
            active.RemoveAll(n => n == null);

            Dictionary<string, HarvestNodeData> merged = new Dictionary<string, HarvestNodeData>();

            if (SaveService.Instance != null)
            {
                foreach (HarvestNodeData data in SaveService.Instance.GetAllHarvestNodeData())
                    if (!string.IsNullOrWhiteSpace(data.nodeId))
                        merged[data.nodeId] = data;
            }

            foreach (HarvestStats n in active)
            {
                if (string.IsNullOrWhiteSpace(n.NodeId)) continue;
                merged[n.NodeId] = new HarvestNodeData { nodeId = n.NodeId, hp = n.HP, isDepleted = n.IsDepleted };
            }

            return new List<HarvestNodeData>(merged.Values);
        }

        // ---- Pickup (đá nhỏ, bụi cây...) ----

        public void InitializePersistentPickup(PickupTarget pickup)
        {
            if (string.IsNullOrWhiteSpace(pickup.PickupId))
            {
                Debug.LogWarning($"[HarvestSaveRegistry] '{pickup.name}' bật Persist Across Sessions nhưng chưa có Pickup Id — không lưu.");
                return;
            }

            if (SaveService.Instance != null
                && SaveService.Instance.GetAllPickedUpIds().Contains(pickup.PickupId))
            {
                Destroy(pickup.gameObject);
            }
        }

        public void NotifyPickupConsumed(string pickupId)
        {
            if (!string.IsNullOrWhiteSpace(pickupId))
                consumedPickupIds.Add(pickupId);
        }

        public List<string> CapturePickedUpIds()
        {
            HashSet<string> ids = new HashSet<string>();

            if (SaveService.Instance != null)
                foreach (string id in SaveService.Instance.GetAllPickedUpIds())
                    ids.Add(id);

            foreach (string id in consumedPickupIds)
                ids.Add(id);

            return new List<string>(ids);
        }
    }
}