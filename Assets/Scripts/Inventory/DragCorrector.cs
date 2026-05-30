using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.UI
{
    /// <summary>
    /// Scales Unity's pixelDragThreshold by the screen's DPI so drag feels
    /// consistent across devices. Attach to the same GameObject as EventSystem.
    /// </summary>
    [RequireComponent(typeof(EventSystem))]
    public sealed class DragCorrector : MonoBehaviour
    {
        [Tooltip("Base drag threshold in pixels, calibrated for basePPI.")]
        [SerializeField] private int baseDragThreshold = 6;

        [Tooltip("The PPI this base threshold was calibrated for.")]
        [SerializeField] private int basePPI = 210;

        private void Start()
        {
            float dpi = Screen.dpi > 0 ? Screen.dpi : basePPI;
            int scaledThreshold = Mathf.RoundToInt(baseDragThreshold * dpi / basePPI);

            EventSystem eventSystem = GetComponent<EventSystem>();
            if (eventSystem != null)
                eventSystem.pixelDragThreshold = scaledThreshold;
        }
    }
}