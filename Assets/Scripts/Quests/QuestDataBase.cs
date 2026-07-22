using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    [CreateAssetMenu(menuName = "Simple Survival/Quests/Quest Database", fileName = "QuestDatabase")]
    public sealed class QuestDatabase : ScriptableObject
    {
        [SerializeField] private List<QuestData> quests = new List<QuestData>();

        private Dictionary<string, QuestData> lookup;

        private void BuildLookupIfNeeded()
        {
            if (lookup != null) return;

            lookup = new Dictionary<string, QuestData>();
            foreach (QuestData quest in quests)
            {
                if (quest == null || string.IsNullOrWhiteSpace(quest.QuestId)) continue;
                lookup[quest.QuestId] = quest;
            }
        }

        public bool TryGet(string questId, out QuestData quest)
        {
            BuildLookupIfNeeded();
            return lookup.TryGetValue(questId, out quest);
        }

#if UNITY_EDITOR
        [ContextMenu("Rebuild From Project")]
        private void RebuildFromProject()
        {
            quests.Clear();
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:QuestData");
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                QuestData quest = UnityEditor.AssetDatabase.LoadAssetAtPath<QuestData>(path);
                if (quest != null) quests.Add(quest);
            }
            UnityEditor.EditorUtility.SetDirty(this);
            lookup = null;
        }
#endif
    }
}