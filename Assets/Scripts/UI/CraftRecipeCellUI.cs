using System;
using System.Collections;
using SimpleSurvival.Items;
using SimpleSurvival.Quests;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public sealed class CraftRecipeCellUI : MonoBehaviour
    {
        [SerializeField] private Button backgroundButton;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private GameObject craftableIcon;

        [Header("Quest Highlight")]
        [SerializeField] private GameObject questHighlightIcon;
        [SerializeField] private Image questHighlightImage;
        [SerializeField] private Color questDefaultColor = Color.white;
        [SerializeField] private Color questActiveColor = Color.yellow;
        [SerializeField] private float questBlinkInterval = 0.4f;

        private bool _subscribed;
        private Coroutine _blinkRoutine;

        public CraftingRecipeData Recipe { get; private set; }

        public void Init(CraftingRecipeData recipe, Action<CraftingRecipeData> onClicked)
        {
            Recipe = recipe;
            iconImage.sprite = recipe.ResultItem.Icon;
            nameText.text = recipe.ResultItem.ItemName;
            backgroundButton.onClick.AddListener(() => onClicked(Recipe));

            RefreshQuestHighlight();
        }

        public void SetCraftable(bool canCraft)
        {
            craftableIcon.SetActive(canCraft);
        }

        private void OnEnable()
        {
            TrySubscribe();
            RefreshQuestHighlight();
        }

        private void Start()
        {
            TrySubscribe();
            RefreshQuestHighlight();
        }

        private void OnDisable()
        {
            if (QuestHighlightManager.Instance != null)
                QuestHighlightManager.Instance.OnHighlightChanged -= RefreshQuestHighlight;
            _subscribed = false;

            StopBlink();
        }

        private void TrySubscribe()
        {
            if (_subscribed) return;
            if (QuestHighlightManager.Instance == null) return;
            QuestHighlightManager.Instance.OnHighlightChanged += RefreshQuestHighlight;
            _subscribed = true;
        }

        private void RefreshQuestHighlight()
        {
            if (Recipe == null) return;

            QuestHighlightManager manager = QuestHighlightManager.Instance;
            bool shouldShow = manager != null && manager.IsItemCraftHighlighted(Recipe.ResultItem);

            if (questHighlightIcon != null)
                questHighlightIcon.SetActive(shouldShow);

            if (shouldShow)
            {
                if (_blinkRoutine == null)
                    StartBlink();
            }
            else
            {
                StopBlink();
            }
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
            ApplyColor(questDefaultColor);
        }

        private IEnumerator BlinkRoutine()
        {
            bool toggle = false;
            while (true)
            {
                ApplyColor(toggle ? questDefaultColor : questActiveColor);
                toggle = !toggle;
                yield return new WaitForSeconds(questBlinkInterval);
            }
        }

        private void ApplyColor(Color color)
        {
            if (questHighlightImage != null) questHighlightImage.color = color;
        }
    }
}