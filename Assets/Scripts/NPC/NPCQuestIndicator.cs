using UnityEngine;
using SimpleSurvival.Quests;

namespace SimpleSurvival.AI
{
    public sealed class NPCQuestIndicator : MonoBehaviour
    {
        [SerializeField] private GameObject availableIcon;
        [SerializeField] private GameObject readyToTurnInIcon;

        public void SetState(NPCQuestState state)
        {
            if (availableIcon != null) availableIcon.SetActive(state == NPCQuestState.Available);
            if (readyToTurnInIcon != null) readyToTurnInIcon.SetActive(state == NPCQuestState.ReadyToTurnIn);
        }

        public void Hide()
        {
            if (availableIcon != null) availableIcon.SetActive(false);
            if (readyToTurnInIcon != null) readyToTurnInIcon.SetActive(false);
        }
    }
}