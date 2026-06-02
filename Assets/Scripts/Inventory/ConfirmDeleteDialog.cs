using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI
{
    /// <summary>
    /// Dialog xác nhận trước khi xóa item.
    /// Show() nhận callback — true = xác nhận xóa, false = hủy.
    /// Background click hoặc ButtonCancel đều = hủy.
    /// Dùng Animation component (2 clip: Show, Hide) để tween.
    /// </summary>
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

        // ── Unity lifecycle ──────────────────────────────────────────────────

        private void Awake()
        {
            // Ẩn ngay từ đầu, không block raycast
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

        // ── Public API ───────────────────────────────────────────────────────

        /// <summary>
        /// Hiện dialog với câu hỏi tùy chỉnh.
        /// callback(true) = người dùng xác nhận xóa.
        /// callback(false) = người dùng hủy.
        /// </summary>
        public void Show(string question, Action<bool> callback)
        {
            _onClose = callback;
            questionText.text = question;

            SetInteractable(true);
            PlayAnimation(showClipName);
        }

        // ── Button handlers ──────────────────────────────────────────────────

        private void HandleConfirm()
        {
            Close(true);
        }

        private void HandleCancel()
        {
            Close(false);
        }

        // ── Private helpers ──────────────────────────────────────────────────

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