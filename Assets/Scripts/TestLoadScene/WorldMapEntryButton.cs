using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    public class WorldMapEntryButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color currentMapColor = new Color(0.4f, 0.4f, 0.4f, 1f);
        [SerializeField] private Color tapFlashColor = new Color(0.6f, 1f, 0.6f, 1f);
        [SerializeField] private float tapFlashDuration = 0.15f;

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

            button.interactable = !isCurrent;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
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