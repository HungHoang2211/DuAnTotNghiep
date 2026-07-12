using System;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    public class WorldMapEntryButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;

        private MapDestination destination;
        private Action<MapDestination> onClicked;

        public void Bind(MapDestination target, bool isCurrent, Action<MapDestination> clickCallback)
        {
            destination = target;
            onClicked = clickCallback;

            if (iconImage != null && target.Icon != null)
                iconImage.sprite = target.Icon;

            button.interactable = !isCurrent;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            onClicked?.Invoke(destination);
        }
    }
}