using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SimpleSurvival.Quests;

namespace SimpleSurvival.UI
{
    public sealed class CraftButtonNotifyIndicator : MonoBehaviour
    {
        [SerializeField] private QuestData questCraftStoneHatchet;
        [SerializeField] private QuestData questCraftSpear;

        [SerializeField] private Image iconImage;
        [SerializeField] private GameObject notifyBadge;
        [SerializeField] private Image notifyBadgeImage;

        [SerializeField] private Color defaultColor = Color.white;
        [SerializeField] private Color activeColor = Color.yellow;
        [SerializeField] private float blinkInterval = 0.4f;

        private bool _subscribed;
        private Coroutine _blinkRoutine;

        private void OnEnable()
        {
            TrySubscribe();
            Refresh();
        }

        private void Start()
        {
            TrySubscribe();
            Refresh();
        }

        private void OnDisable()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null)
            {
                manager.OnQuestStarted -= HandleQuestStateChanged;
                manager.OnQuestCompleted -= HandleQuestStateChanged;
                manager.OnQuestFailed -= HandleQuestStateChanged;
            }
            _subscribed = false;
            StopBlink();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            QuestManager manager = QuestManager.Instance;
            if (manager == null) return;

            manager.OnQuestStarted += HandleQuestStateChanged;
            manager.OnQuestCompleted += HandleQuestStateChanged;
            manager.OnQuestFailed += HandleQuestStateChanged;
            _subscribed = true;
        }

        private void HandleQuestStateChanged(QuestData quest)
        {
            Refresh();
        }

        private void Refresh()
        {
            QuestManager manager = QuestManager.Instance;
            bool shouldBlink = manager != null &&
                ((questCraftStoneHatchet != null && manager.IsQuestActive(questCraftStoneHatchet)) ||
                 (questCraftSpear != null && manager.IsQuestActive(questCraftSpear)));

            if (notifyBadge != null)
                notifyBadge.SetActive(shouldBlink);

            if (shouldBlink)
            {
                if (_blinkRoutine == null)
                    _blinkRoutine = StartCoroutine(BlinkRoutine());
            }
            else
            {
                StopBlink();
            }
        }

        private void StopBlink()
        {
            if (_blinkRoutine != null)
            {
                StopCoroutine(_blinkRoutine);
                _blinkRoutine = null;
            }
            ApplyColor(defaultColor);
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
            if (iconImage != null) iconImage.color = color;
            if (notifyBadgeImage != null) notifyBadgeImage.color = color;
        }
    }
}