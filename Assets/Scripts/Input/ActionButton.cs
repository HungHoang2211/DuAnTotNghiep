using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.Input
{
    public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum ActionType { Sneak, Sprint }
        public enum Mode { Hold, Toggle }

        [Header("References")]
        [Tooltip("PlayerInputReader nhận state từ button này.")]
        [SerializeField] private PlayerInputReader inputReader;

        [Header("Settings")]
        [Tooltip("Loại action button này điều khiển.")]
        [SerializeField] private ActionType action = ActionType.Sneak;

        [Tooltip("Hold = giữ để active. Toggle = tap để bật/tắt.")]
        [SerializeField] private Mode mode = Mode.Toggle;

        [Header("Visual (optional)")]
        [Tooltip("GameObject hiển thị khi active (icon highlight, glow, ...). Có thể null.")]
        [SerializeField] private GameObject activeIndicator;

        private bool _isActive = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (mode == Mode.Hold)
            {
                SetState(true);
            }
            else // Toggle
            {
                SetState(!_isActive);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (mode == Mode.Hold)
                SetState(false);
        }

        private void SetState(bool active)
        {
            _isActive = active;

            if (inputReader == null) return;

            switch (action)
            {
                case ActionType.Sneak:
                    inputReader.SetSneakFromUI(active);
                    break;
                case ActionType.Sprint:
                    inputReader.SetSprintFromUI(active);
                    break;
            }

            if (activeIndicator != null)
                activeIndicator.SetActive(active);
        }
        public void ResetState()
        {
            SetState(false);
        }
    }
}