using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.Quests
{
    public sealed class QuestPanelToggleButton : MonoBehaviour, IPointerDownHandler
    {
        [Header("Target")]
        [SerializeField] private GameObject panelContent;

        private bool _isOpen;

        private void Start()
        {
            _isOpen = false;
            ApplyState();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isOpen = !_isOpen;
            ApplyState();
        }

        private void ApplyState()
        {
            if (panelContent != null)
                panelContent.SetActive(_isOpen);
        }
    }
}