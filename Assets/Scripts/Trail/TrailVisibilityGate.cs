using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    public static class TrailVisibilityGate
    {
        private static readonly List<GameObject> _trackedPanels = new List<GameObject>();

        public static void Register(GameObject panel)
        {
            if (panel != null && !_trackedPanels.Contains(panel))
                _trackedPanels.Add(panel);
        }

        public static void Unregister(GameObject panel)
        {
            _trackedPanels.Remove(panel);
        }

        public static bool IsBlocked
        {
            get
            {
                for (int i = _trackedPanels.Count - 1; i >= 0; i--)
                {
                    if (_trackedPanels[i] == null)
                    {
                        _trackedPanels.RemoveAt(i);
                        continue;
                    }
                    if (_trackedPanels[i].activeInHierarchy) return true;
                }
                return false;
            }
        }
    }
}