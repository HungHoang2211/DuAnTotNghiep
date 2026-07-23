using System;
using System.Collections.Generic;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class ActiveQuestData
    {
        public string questId;
        public List<int> objectiveProgress = new List<int>();
    }

    [Serializable]
    public sealed class WorldData
    {
        public List<string> completedQuestIds = new List<string>();
        public List<ActiveQuestData> activeQuests = new List<ActiveQuestData>();
        public bool storyCompleted;
        public List<string> permanentlyLockedMapIds = new List<string>();
    }
}