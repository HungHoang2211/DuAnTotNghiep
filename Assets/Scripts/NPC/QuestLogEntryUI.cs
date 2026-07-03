using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Quests
{
    public sealed class QuestLogEntryUI : MonoBehaviour
    {
        [SerializeField] private Text questNameText;
        [SerializeField] private Text objectiveText;

        public void SetQuestName(string value)
        {
            if (questNameText != null) questNameText.text = value;
        }

        public void SetObjectiveText(string value)
        {
            if (objectiveText != null) objectiveText.text = value;
        }
    }
}