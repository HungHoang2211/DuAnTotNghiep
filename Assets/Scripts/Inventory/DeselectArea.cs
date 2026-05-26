using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.Items
{
    /// <summary>
    /// A background area that clears the inventory selection when clicked.
    /// Place this on an Image that covers the inventory panel and sits BEHIND
    /// the cells in draw order: clicking a cell is caught by the cell, clicking
    /// the gaps or margins falls through to this and deselects.
    ///
    /// The Image needs Raycast Target enabled so it can receive clicks; it can
    /// be fully transparent.
    /// </summary>
    public sealed class DeselectArea : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private InventorySelection selection;

        public void OnPointerClick(PointerEventData eventData)
        {
            selection.Deselect();
        }
    }
}