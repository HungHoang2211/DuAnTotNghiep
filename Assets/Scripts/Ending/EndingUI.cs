using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleSurvival.Quests;
using SimpleSurvival.SaveLoad;

namespace SimpleSurvival.World
{
    public class EndingUI : MonoBehaviour
    {
        public static EndingUI Instance { get; private set; }

        [SerializeField] private string mainMenuScene = "Start";

        [Header("Panels")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private GameObject comicPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject confirmNewGamePanel;

        [Header("Comic")]
        [SerializeField] private EndingComicReveal comicReveal;

        private List<string> _pendingMapsToLock = new List<string>();
        private bool _endingActive;
        private bool _atCredits;
        private bool _isPaused;

        public bool WasInterrupted { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;

            rootPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            if (QuestManager.Instance != null)
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }

        public void Restore(EndingSaveData data)
        {
            WasInterrupted = data != null && data.active;
            _atCredits = data != null && data.atCredits;
            _pendingMapsToLock = data != null ? new List<string>(data.mapsToLockOnComplete) : new List<string>();
        }

        public EndingSaveData Capture()
        {
            return new EndingSaveData
            {
                active = _endingActive,
                atCredits = _atCredits,
                mapsToLockOnComplete = _pendingMapsToLock
            };
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (!quest.MarksStoryComplete) return;

            _pendingMapsToLock = new List<string>(quest.MapsToLockOnComplete);
            OpenEnding();
        }

        private void OpenEnding()
        {
            _endingActive = true;
            _atCredits = false;

            comicReveal.ResetReveal();
            comicPanel.SetActive(true);
            creditsPanel.SetActive(false);
            confirmNewGamePanel.SetActive(false);
            rootPanel.SetActive(true);
            comicReveal.RevealNext();

            Time.timeScale = 0f;
            _isPaused = true;
        }

        public void ResumeInterrupted()
        {
            if (!WasInterrupted) return;

            _endingActive = true;
            rootPanel.SetActive(true);

            if (_atCredits)
            {
                comicPanel.SetActive(false);
                creditsPanel.SetActive(true);
            }
            else
            {
                comicPanel.SetActive(true);
                creditsPanel.SetActive(false);
                comicReveal.ResetReveal();
                comicReveal.RevealNext();
            }

            confirmNewGamePanel.SetActive(false);
            Time.timeScale = 0f;
            _isPaused = true;
        }

        public void OnTapAdvance()
        {
            if (!comicPanel.activeSelf) return;

            if (!comicReveal.IsFullyRevealed)
            {
                comicReveal.RevealNext();
                return;
            }

            comicPanel.SetActive(false);
            creditsPanel.SetActive(true);
            _atCredits = true;
        }

        public void OnContinuePressed()
        {
            foreach (string mapId in _pendingMapsToLock)
                QuestManager.Instance?.LockMapPermanently(mapId);

            _endingActive = false;
            SaveService.Instance?.Save();
            ClosePanel();
        }

        public void OnNewGamePressed()
        {
            confirmNewGamePanel.SetActive(true);
        }

        public void OnConfirmNewGameYes()
        {
            SaveService.Instance?.DeleteSave();
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuScene);
        }

        public void OnConfirmNewGameNo()
        {
            confirmNewGamePanel.SetActive(false);
        }

        private void ClosePanel()
        {
            rootPanel.SetActive(false);
            if (_isPaused)
            {
                Time.timeScale = 1f;
                _isPaused = false;
            }
        }
    }
}