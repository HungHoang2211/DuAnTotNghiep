using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.UI.Tutorial
{
    public class TutorialButtonController : MonoBehaviour
    {
        private const string HasSeenTutorialKey = "HasSeenTutorial";

        [SerializeField] private Button tutorialButton;
        [SerializeField] private TutorialPanelUI tutorialPanel;

        private void Awake()
        {
            tutorialButton.onClick.AddListener(tutorialPanel.OpenPanel);
        }

        private void Start()
        {
            if (PlayerPrefs.GetInt(HasSeenTutorialKey, 0) == 0)
            {
                tutorialPanel.OnPanelClosed += MarkTutorialSeen;
                tutorialPanel.OpenPanel();
            }
        }

        private void MarkTutorialSeen()
        {
            tutorialPanel.OnPanelClosed -= MarkTutorialSeen;
            PlayerPrefs.SetInt(HasSeenTutorialKey, 1);
            PlayerPrefs.Save();
        }
    }
}