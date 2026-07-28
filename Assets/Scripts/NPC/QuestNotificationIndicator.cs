using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Quests
{
    public sealed class QuestNotificationIndicator : MonoBehaviour
    {
        [SerializeField] private Image icon;
        [SerializeField] private TutorialQuestSequencer sequencer;
        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color activeColor = Color.yellow;
        [SerializeField] private float blinkInterval = 0.4f;

        private Coroutine _blinkRoutine;
        private bool _hasActiveQuest;

        private void OnEnable()
        {
            if (sequencer != null)
            {
                sequencer.OnNewQuestAvailable += HandleNewQuestAvailable;
                sequencer.OnAllQuestsCleared += HandleAllQuestsCleared;
            }

            ApplyColor(defaultColor);
        }

        private void OnDisable()
        {
            if (sequencer != null)
            {
                sequencer.OnNewQuestAvailable -= HandleNewQuestAvailable;
                sequencer.OnAllQuestsCleared -= HandleAllQuestsCleared;
            }

            StopBlink();
        }

        private void HandleNewQuestAvailable(QuestData quest)
        {
            _hasActiveQuest = true;
            StartBlink();
        }

        private void HandleAllQuestsCleared()
        {
            _hasActiveQuest = false;
            StopBlink();
            ApplyColor(defaultColor);
        }

        public void MarkSeen()
        {
            StopBlink();
            if (_hasActiveQuest)
                ApplyColor(activeColor);
        }

        private void StartBlink()
        {
            StopBlink();
            _blinkRoutine = StartCoroutine(BlinkRoutine());
        }

        private void StopBlink()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
        }

        private IEnumerator BlinkRoutine()
        {
            bool toggle = false;
            while (true)
            {
                ApplyColor(toggle ? defaultColor : activeColor);
                toggle = !toggle;
                yield return new WaitForSeconds(blinkInterval);
            }
        }

        private void ApplyColor(Color color)
        {
            if (icon != null) icon.color = color;
        }
    }
}