using UnityEngine;
using UnityEngine.EventSystems;
using SimpleSurvival.Actions;
using SimpleSurvival.Player;
using SimpleSurvival.Targets;

namespace SimpleSurvival.UI
{
    public class AttackButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        [Header("References")]
        [SerializeField] private PlayerActionController actionController;
        [SerializeField] private PlayerTargetChecker targetChecker;
        [SerializeField] private Transform pressRoot;

        public void OnPointerDown(PointerEventData eventData)
        {
            if (pressRoot != null) pressRoot.localScale = Vector3.one * 0.9f;

            actionController.SetAttackHeld(true);

            if (actionController.CurrentAction.Type != ActionType.Attack)
            {
                ITargetable enemy = targetChecker != null ? targetChecker.CurrentEnemy : null;
                actionController.RequestAttack(enemy);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (pressRoot != null) pressRoot.localScale = Vector3.one;

            actionController.SetAttackHeld(false);
        }
    }
}