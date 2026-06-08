using UnityEngine;
using UnityEngine.EventSystems;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;
using SimpleSurvival.Actions;

namespace SimpleSurvival.UI
{
    public class AttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private PlayerActionController actionController;
        [SerializeField] private PlayerTargetChecker targetChecker;
        [SerializeField] private Transform pressRoot;

        private bool _isPressed;

        private void Update()
        {
            if (!_isPressed) return;
            if (actionController.CurrentAction.Type == ActionType.Attack) return;

            ITargetable enemy = targetChecker != null ? targetChecker.CurrentEnemy : null;
            actionController.RequestAttack(enemy);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _isPressed = true;
            if (pressRoot != null) pressRoot.localScale = Vector3.one * 0.9f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isPressed = false;
            if (pressRoot != null) pressRoot.localScale = Vector3.one;
        }
    }
}