using UnityEngine;
using SimpleSurvival.Quests;

namespace SimpleSurvival.UI
{
    public sealed class TrailVisibilityBlocker : MonoBehaviour
    {
        private void Awake()
        {
            TrailVisibilityGate.Register(gameObject);
        }

        private void OnDestroy()
        {
            TrailVisibilityGate.Unregister(gameObject);
        }
    }
}