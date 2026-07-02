using UnityEngine;

namespace SimpleSurvival.World
{
    [RequireComponent(typeof(Collider))]
    public class MapEdgeTrigger : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";

        private void Reset()
        {
            GetComponent<Collider>().isTrigger = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(playerTag)) return;
            if (WorldMapUI.Instance == null) return;

            WorldMapUI.Instance.Open();
        }
    }
}