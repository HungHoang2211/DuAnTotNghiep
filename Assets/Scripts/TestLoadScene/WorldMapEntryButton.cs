using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    public class WorldMapEntryButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color currentMapColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color tapFlashColor = new Color(0.6f, 1f, 0.6f, 1f);
        [SerializeField] private float tapFlashDuration = 0.15f;

        [Header("Hover Tooltip")]
        [SerializeField] private GameObject nameLabelRoot;
        [SerializeField] private TextMeshProUGUI nameLabel;

        private MapDestination destination;
        private Action<MapDestination> onClicked;

        public void Bind(MapDestination target, bool isCurrent, Action<MapDestination> clickCallback)
        {
            destination = target;
            onClicked = clickCallback;

            if (iconImage != null && target.Icon != null)
                iconImage.sprite = target.Icon;

            if (iconImage != null)
                iconImage.color = isCurrent ? currentMapColor : normalColor;

            if (nameLabel != null)
                nameLabel.text = target.DisplayName;

            HideTooltip();

            button.interactable = !isCurrent;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            HideTooltip();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowTooltip();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            HideTooltip();
        }

        private void ShowTooltip()
        {
            if (destination == null)
                return;

            if (nameLabelRoot != null)
                nameLabelRoot.SetActive(true);
        }

        private void HideTooltip()
        {
            if (nameLabelRoot != null)
                nameLabelRoot.SetActive(false);
        }

        private void HandleClick()
        {
            StartCoroutine(TapFlashRoutine());
        }

        private IEnumerator TapFlashRoutine()
        {
            if (iconImage != null)
                iconImage.color = tapFlashColor;

            yield return new WaitForSecondsRealtime(tapFlashDuration);

            onClicked?.Invoke(destination);
        }
    }
}