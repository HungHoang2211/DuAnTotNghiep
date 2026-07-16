using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SimpleSurvival.UI.MainMenu
{
    public class StartScreenController : MonoBehaviour
    {
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text tapToStartText;
        [SerializeField] private string coreSceneName = "Core";
        [SerializeField] private float minAlpha = 0.2f;
        [SerializeField] private float maxAlpha = 1f;
        [SerializeField] private float blinkSpeed = 1.5f;

        private void Awake()
        {
            startButton.onClick.AddListener(HandleTap);
        }

        private void Update()
        {
            float t = Mathf.PingPong(Time.time * blinkSpeed, 1f);
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);
            Color color = tapToStartText.color;
            color.a = alpha;
            tapToStartText.color = color;
        }

        private void HandleTap()
        {
            startButton.interactable = false;
            SceneManager.LoadScene(coreSceneName, LoadSceneMode.Single);
        }
    }
}