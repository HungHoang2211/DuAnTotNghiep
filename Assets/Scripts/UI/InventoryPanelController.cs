using UnityEngine;
using UnityEngine.UI;
using SimpleSurvival.Loot;

namespace SimpleSurvival.UI
{
    public sealed class InventoryPanelController : MonoBehaviour
    {
        public static InventoryPanelController Instance { get; private set; }

        [Header("Root")]
        [SerializeField] private GameObject panelRoot;

        [Header("Sub Panels")]
        [SerializeField] private GameObject backpackPanel;
        [SerializeField] private GameObject statsPanel;
        [SerializeField] private LootPanel lootPanel;

        [Header("Buttons")]
        [SerializeField] private Button hudInventoryButton;
        [SerializeField] private Button closeButton;

        public event System.Action OnClosed;

        public bool IsOpen => panelRoot != null && panelRoot.activeSelf;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            if (panelRoot != null) panelRoot.SetActive(false);

            if (hudInventoryButton != null)
                hudInventoryButton.onClick.AddListener(OpenDefault);

            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (hudInventoryButton != null)
                hudInventoryButton.onClick.RemoveListener(OpenDefault);

            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        public void OpenDefault()
        {
            if (panelRoot == null) return;

            panelRoot.SetActive(true);
            if (backpackPanel != null) backpackPanel.SetActive(true);
            if (statsPanel != null) statsPanel.SetActive(true);
            if (lootPanel != null) lootPanel.Hide();
        }

        public void OpenLoot(LootContainer container)
        {
            if (panelRoot == null || lootPanel == null || container == null) return;

            panelRoot.SetActive(true);
            if (backpackPanel != null) backpackPanel.SetActive(true);
            if (statsPanel != null) statsPanel.SetActive(false);
            lootPanel.Show(container);
        }

        public void Close()
        {
            if (lootPanel != null) lootPanel.Hide();
            if (panelRoot != null) panelRoot.SetActive(false);
            OnClosed?.Invoke();
        }
    }
}