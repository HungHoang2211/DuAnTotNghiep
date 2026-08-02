using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.World;

namespace SimpleSurvival.Quests
{
    public sealed class TutorialQuestSequencer : MonoBehaviour
    {
        [SerializeField] private List<QuestData> questChain = new List<QuestData>();
        [SerializeField] private float delayBeforeNextQuest = 2f;

        private QuestData _currentQuest;
        private bool _highlightRevealed;

        public event Action<QuestData> OnNewQuestAvailable;
        public event Action OnAllQuestsCleared;
        public event Action OnCurrentQuestCompleted;

        private void Start()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return;

            manager.OnQuestReadyToTurnIn += HandleReadyToTurnIn;
            manager.OnQuestCompleted += HandleQuestCompleted;

            StartCoroutine(KickoffWhenReady());
        }

        private IEnumerator KickoffWhenReady()
        {
            yield return null;

            MapTransitionController transition = MapTransitionController.Instance;
            if (transition != null)
            {
                while (transition.IsTransitioning)
                    yield return null;
            }

            StartNextQuest();
        }

        private void OnDestroy()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return;

            manager.OnQuestReadyToTurnIn -= HandleReadyToTurnIn;
            manager.OnQuestCompleted -= HandleQuestCompleted;
        }

        private void StartNextQuest()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return;

            _currentQuest = null;
            _highlightRevealed = false;

            foreach (var quest in questChain)
            {
                if (quest == null) continue;
                if (manager.IsQuestCompleted(quest)) continue;

                _currentQuest = quest;
                break;
            }

            if (_currentQuest == null)
            {
                QuestHighlightManager.Instance?.ClearActiveQuest();
                OnAllQuestsCleared?.Invoke();
                return;
            }

            if (!manager.IsQuestActive(_currentQuest))
                manager.StartQuest(_currentQuest);

            OnNewQuestAvailable?.Invoke(_currentQuest);
        }

        /// <summary>
        /// Bật highlight visual cho vật được chỉ định của quest hiện tại (tutorial).
        /// Chỉ bật khi "quest" truyền vào đúng là quest tutorial đang active - tránh trường hợp
        /// người chơi click vào 1 entry quest khác (không thuộc tutorial chain) trong QuestLogUI
        /// mà vẫn vô tình bật highlight của quest tutorial.
        /// </summary>
        public void RevealQuestHighlight(QuestData quest)
        {
            if (quest == null || quest != _currentQuest || _highlightRevealed) return;
            _highlightRevealed = true;
            QuestHighlightManager.Instance?.SetActiveQuest(_currentQuest);
        }

        private void HandleReadyToTurnIn(QuestData quest)
        {
            if (quest != _currentQuest) return;

            QuestManager manager = QuestManager.Instance;
            if (manager != null)
                manager.CompleteQuest(quest);
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            if (quest != _currentQuest) return;

            OnCurrentQuestCompleted?.Invoke();
            StartCoroutine(StartNextQuestAfterDelay());
        }

        private IEnumerator StartNextQuestAfterDelay()
        {
            yield return new WaitForSeconds(delayBeforeNextQuest);
            StartNextQuest();
        }
    }
}