using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using SimpleSurvival.Player;

namespace SimpleSurvival.UI
{
    public sealed class PenaltyRegenToggle : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button toggleButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private RectTransform knob;

        [Header("Positions (local anchoredPosition.x của Knob)")]
        [SerializeField] private float knobOffPosX = -20f;
        [SerializeField] private float knobOnPosX = 20f;

        [Header("Colors")]
        [SerializeField] private Color onColor = new Color(0.25f, 0.75f, 0.35f);  // xanh
        [SerializeField] private Color offColor = new Color(0.8f, 0.25f, 0.25f);  // đỏ

        [Header("Animation")]
        [SerializeField] private float animDuration = 0.15f;

        [Header("Persistence")]
        [Tooltip("Key lưu lựa chọn vào PlayerPrefs, load lại đúng trạng thái ở lần chơi sau.")]
        [SerializeField] private string prefsKey = "Gameplay_PenaltyRegenEnabled";

        private bool _isOn = true;
        private Coroutine _animCoroutine;

        private void Awake()
        {
            if (toggleButton == null) toggleButton = GetComponent<Button>();
            if (backgroundImage == null) backgroundImage = GetComponent<Image>();
        }

        private void Start()
        {
            _isOn = PlayerPrefs.GetInt(prefsKey, 1) == 1;
            ApplyStateInstant(_isOn);
            ApplyToPlayerStats(_isOn);

            if (toggleButton != null)
                toggleButton.onClick.AddListener(OnClicked);
            else
                Debug.LogWarning("[PenaltyRegenToggle] Thiếu Button reference — nút sẽ không bấm được.");
        }

        private void OnDestroy()
        {
            if (toggleButton != null)
                toggleButton.onClick.RemoveListener(OnClicked);
        }

        private void OnClicked()
        {
            _isOn = !_isOn;

            PlayerPrefs.SetInt(prefsKey, _isOn ? 1 : 0);
            PlayerPrefs.Save();

            ApplyToPlayerStats(_isOn);

            if (_animCoroutine != null) StopCoroutine(_animCoroutine);
            _animCoroutine = StartCoroutine(AnimateTo(_isOn));
        }

        private void ApplyToPlayerStats(bool isOn)
        {
            var playerStats = PlayerActionController.Instance != null
                ? PlayerActionController.Instance.PlayerStats
                : null;

            if (playerStats == null)
            {
                Debug.LogWarning("[PenaltyRegenToggle] Không tìm thấy PlayerStats (PlayerActionController.Instance chưa sẵn sàng) " +
                    "— chưa set được EnableWeakenedPenalty/AllowRegen. Trạng thái vẫn được lưu PlayerPrefs, sẽ áp dụng lần mở panel kế tiếp.");
                return;
            }

            playerStats.EnableWeakenedPenalty = isOn;
            playerStats.AllowRegen = isOn;
        }

        private void ApplyStateInstant(bool isOn)
        {
            if (knob != null)
            {
                Vector2 pos = knob.anchoredPosition;
                pos.x = isOn ? knobOnPosX : knobOffPosX;
                knob.anchoredPosition = pos;
            }
            if (backgroundImage != null)
                backgroundImage.color = isOn ? onColor : offColor;
        }

        private IEnumerator AnimateTo(bool isOn)
        {
            float startX = knob != null ? knob.anchoredPosition.x : 0f;
            float targetX = isOn ? knobOnPosX : knobOffPosX;
            Color startColor = backgroundImage != null ? backgroundImage.color : Color.white;
            Color targetColor = isOn ? onColor : offColor;

            float t = 0f;
            while (t < animDuration)
            {
                t += Time.unscaledDeltaTime;
                float lerpT = Mathf.Clamp01(t / animDuration);

                if (knob != null)
                {
                    Vector2 pos = knob.anchoredPosition;
                    pos.x = Mathf.Lerp(startX, targetX, lerpT);
                    knob.anchoredPosition = pos;
                }
                if (backgroundImage != null)
                    backgroundImage.color = Color.Lerp(startColor, targetColor, lerpT);

                yield return null;
            }

            ApplyStateInstant(isOn);
            _animCoroutine = null;
        }
    }
}