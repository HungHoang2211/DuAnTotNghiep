using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SimpleSurvival.Player
{
    public sealed class DeathDialogController : MonoBehaviour
    {
        [Header("Refs")]
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI textReason;
        [SerializeField] private Button reviveButton;
        [SerializeField] private PlayerDeathHandler deathHandler;

        private void Awake()
        {
            if (root != null)
                root.SetActive(false);

            if (reviveButton != null)
                reviveButton.onClick.AddListener(HandleReviveClicked);
        }

        private void OnDestroy()
        {
            if (reviveButton != null)
                reviveButton.onClick.RemoveListener(HandleReviveClicked);
        }

        public void Show(string killerName)
        {
            if (textReason != null)
            {
                bool hasKiller = !string.IsNullOrEmpty(killerName);
                textReason.gameObject.SetActive(hasKiller);
                if (hasKiller)
                    textReason.text = $"by {killerName}";
            }

            if (root != null)
                root.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        private void HandleReviveClicked()
        {
            if (deathHandler != null)
                deathHandler.Revive();
        }
    }
}