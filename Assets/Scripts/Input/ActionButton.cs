using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.Input
{
    public class ActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public enum ActionType { Sneak, Sprint }
        public enum Mode { Hold, Toggle }

        [Header("References")]
        [SerializeField] private PlayerInputReader inputReader;

        [Header("Settings")]
        [SerializeField] private ActionType action = ActionType.Sneak;
        [SerializeField] private Mode mode = Mode.Toggle;

        [Header("Visual")]
        [Tooltip("Icon Image của button. Sẽ swap sprite khi active.")]
        [SerializeField] private Image iconImage;
        [SerializeField] private Sprite inactiveSprite;
        [SerializeField] private Sprite activeSprite;

        [Tooltip("Optional: GameObject phụ hiển thị khi active (glow, ring, ...).")]
        [SerializeField] private GameObject activeIndicator;

        private bool _isActive = false;

        private void Awake()
        {
            ApplyVisual();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (mode == Mode.Hold)
                SetState(true);
            else
                SetState(!_isActive);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (mode == Mode.Hold)
                SetState(false);
        }

        private void SetState(bool active)
        {
            _isActive = active;

            if (inputReader != null)
            {
                switch (action)
                {
                    case ActionType.Sneak:
                        inputReader.SetSneakFromUI(active);
                        break;
                    case ActionType.Sprint:
                        inputReader.SetSprintFromUI(active);
                        break;
                }
            }

            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (iconImage != null)
            {
                Sprite target = _isActive ? activeSprite : inactiveSprite;
                if (target != null)
                    iconImage.sprite = target;
            }

            if (activeIndicator != null)
                activeIndicator.SetActive(_isActive);
        }

        public void ResetState()
        {
            SetState(false);
        }
    }
}