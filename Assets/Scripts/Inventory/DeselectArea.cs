using UnityEngine;
using UnityEngine.EventSystems;

namespace SimpleSurvival.Items
{
    public sealed class DeselectArea : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] private InventorySelection selection;

        public void OnPointerClick(PointerEventData eventData)
        {
            selection.Deselect();
        }
    }
}