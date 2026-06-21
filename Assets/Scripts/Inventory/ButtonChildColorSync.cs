using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class ButtonChildColorSync : MonoBehaviour
    {
        [SerializeField] private List<Graphic> targets = new List<Graphic>();

        [SerializeField] private Color activeColor = Color.white;
        [SerializeField] private Color disabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

        private Button button;
        private bool lastInteractable;

        private void Awake()
        {
            button = GetComponent<Button>();
        }

        private void Start()
        {
            lastInteractable = button.interactable;
            ApplyColor(lastInteractable);
        }

        private void Update()
        {
            if (button.interactable == lastInteractable)
                return;

            lastInteractable = button.interactable;
            ApplyColor(lastInteractable);
        }

        private void ApplyColor(bool interactable)
        {
            Color color = interactable ? activeColor : disabledColor;
            foreach (Graphic target in targets)
            {
                if (target != null)
                    target.color = color;
            }
        }
    }
}