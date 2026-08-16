using System;
using System.Collections.Generic;

namespace SimpleSurvival.SaveLoad
{
    [Serializable]
    public sealed class EndingSaveData
    {
        public bool active;
        public bool atCredits;
        public List<string> mapsToLockOnComplete = new List<string>();
    }
}