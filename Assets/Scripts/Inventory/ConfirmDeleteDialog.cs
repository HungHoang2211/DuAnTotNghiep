using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    public sealed class ConfirmDeleteDialog : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CanvasGroup rootCanvasGroup;
        [SerializeField] private GameObject background;
        [SerializeField] private Button backgroundButton;
        [SerializeField] private Button buttonDelete;
        [SerializeField] private Button buttonCancel;

        [Header("Text")]
        [SerializeField] private TMP_Text questionText;

        [Header("Animation")]
        [SerializeField] private Animation dialogAnimation;
        [SerializeField] private string showClipName = "Dialog_Appear";
        [SerializeField] private string hideClipName = "Dialog_Hide";

        private Action<bool> _onClose;

        private void Awake()
        {
            SetInteractable(false);

            backgroundButton.onClick.AddListener(HandleCancel);
            buttonCancel.onClick.AddListener(HandleCancel);
            buttonDelete.onClick.AddListener(HandleConfirm);
        }

        private void OnDestroy()
        {
            backgroundButton.onClick.RemoveListener(HandleCancel);
            buttonCancel.onClick.RemoveListener(HandleCancel);
            buttonDelete.onClick.RemoveListener(HandleConfirm);
        }

        public void Show(string question, Action<bool> callback)
        {
            _onClose = callback;
            questionText.text = question;

            SetInteractable(true);
            PlayAnimation(showClipName);
        }

        private void HandleConfirm()
        {
            Close(true);
        }

        private void HandleCancel()
        {
            Close(false);
        }

        private void Close(bool confirmed)
        {
            SetInteractable(false);
            PlayAnimation(hideClipName);

            _onClose?.Invoke(confirmed);
            _onClose = null;
        }

        private void SetInteractable(bool interactable)
        {
            background.SetActive(interactable);
            rootCanvasGroup.alpha = interactable ? 1f : 0f;
            rootCanvasGroup.interactable = interactable;
            rootCanvasGroup.blocksRaycasts = interactable;
        }

        private void PlayAnimation(string clipName)
        {
            if (dialogAnimation == null) return;
            dialogAnimation.Play(clipName);
        }
    }
}