using TMPro;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    public sealed class QuestLogEntryUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text questNameText;
        [SerializeField] private TMP_Text objectiveText;

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