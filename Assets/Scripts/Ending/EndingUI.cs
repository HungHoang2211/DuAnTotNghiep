using UnityEngine;
using UnityEngine.SceneManagement;
using SimpleSurvival.Quests;
using SimpleSurvival.SaveLoad;

namespace SimpleSurvival.World
{
    public class EndingUI : MonoBehaviour
    {
        [SerializeField] private string mainMenuScene = "Start";

        [Header("Panels")]
        [SerializeField] private GameObject rootPanel;
        [SerializeField] private GameObject comicPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject confirmNewGamePanel;

        [Header("Comic")]
        [SerializeField] private EndingComicReveal comicReveal;

        private QuestData completedQuest;
        private bool isPaused;

        private void Start()
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;

            rootPanel.SetActive(false);
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (!quest.MarksStoryComplete) return;

            completedQuest = quest;
            OpenEnding();
        }

        private void OpenEnding()
        {
            comicReveal.ResetReveal();
            comicPanel.SetActive(true);
            creditsPanel.SetActive(false);
            confirmNewGamePanel.SetActive(false);
            rootPanel.SetActive(true);
            comicReveal.RevealNext();   

            Time.timeScale = 0f;
            isPaused = true;
        }

        public void OnTapAdvance()
        {
            if (comicPanel.activeSelf)
            {
                if (!comicReveal.IsFullyRevealed)
                {
                    comicReveal.RevealNext();
                }
                else
                {
                    comicPanel.SetActive(false);
                    creditsPanel.SetActive(true);
                }
            }
        }

        public void OnContinuePressed()
        {
            if (completedQuest != null)
            {
                foreach (string mapId in completedQuest.MapsToLockOnComplete)
                    QuestManager.Instance?.LockMapPermanently(mapId);
            }

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
            if (isPaused)
            {
                Time.timeScale = 1f;
                isPaused = false;
            }
        }
    }
}