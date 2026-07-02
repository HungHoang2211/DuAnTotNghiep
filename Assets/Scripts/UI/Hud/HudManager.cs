using UnityEngine;

namespace SimpleSurvival.UI.Hud
{
    public sealed class HudManager : MonoBehaviour
    {
        public static HudManager Instance { get; private set; }

        [Header("Canvas Refs")]
        [SerializeField] private RectTransform canvasRect;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private Camera uiCamera;

        [Header("Sub-managers")]
        [SerializeField] private UnlockProgressManager unlockProgress;
        [SerializeField] private HpHudManager hpHud;
        [SerializeField] private SpeechManager speech;

        public RectTransform CanvasRect => canvasRect;
        public Camera GameCamera => gameCamera;
        public Camera UICamera => uiCamera;

        public UnlockProgressManager UnlockProgress => unlockProgress;
        public HpHudManager HpHud => hpHud;
        public SpeechManager Speech => speech;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }
    }
}