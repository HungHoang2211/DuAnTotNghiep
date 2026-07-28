using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public sealed class RaycastDebugger : MonoBehaviour
{
    private void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;
        if (EventSystem.current == null) return;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointerData, results);

        Debug.Log($"[RaycastDebug] {results.Count} đối tượng bị trúng tại {Input.mousePosition}:");
        foreach (RaycastResult r in results)
            Debug.Log($"  - {r.gameObject.name} (canvas={r.module.gameObject.name}, depth={r.depth}, sortingOrder={r.sortingOrder})");
    }
}