using System;
using System.Collections;
using SimpleSurvival.SaveLoad;
using UnityEngine;

namespace SimpleSurvival.World
{
    public class MapTransitionController : MonoBehaviour
    {
        public static MapTransitionController Instance { get; private set; }

        [SerializeField] private MapLoader mapLoader;
        [SerializeField] private FadeScreen fadeScreen;
        [SerializeField] private string startMapScene = "BaseMap";
        public string StartMapScene => startMapScene;
        [SerializeField] private float fadeDuration = 0.3f;
        [SerializeField] private float minBlackDuration = 0.2f;

        public event Action TransitionStarted;
        public event Action TransitionFinished;

        private bool isTransitioning;

        public bool IsTransitioning => isTransitioning;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            StartCoroutine(InitialLoadRoutine());
        }

        public void GoToMap(string mapScene)
        {
            if (isTransitioning) return;
            if (mapScene == mapLoader.CurrentMapScene) return;

            StartCoroutine(TransitionRoutine(mapScene));
        }

        private IEnumerator InitialLoadRoutine()
        {
            isTransitioning = true;
            TransitionStarted?.Invoke();

            SaveService.Instance?.Read();

            fadeScreen.SetBlack();
            yield return mapLoader.SwapRoutine(startMapScene);
            SaveService.Instance?.ApplyColdBoot();
            yield return fadeScreen.FadeIn(fadeDuration);

            TransitionFinished?.Invoke();
            isTransitioning = false;
        }

        private IEnumerator TransitionRoutine(string mapScene)
        {
            isTransitioning = true;
            TransitionStarted?.Invoke();

            yield return fadeScreen.FadeOut(fadeDuration);

            float startTime = Time.unscaledTime;
            yield return mapLoader.SwapRoutine(mapScene);

            float elapsed = Time.unscaledTime - startTime;
            if (elapsed < minBlackDuration)
                yield return new WaitForSecondsRealtime(minBlackDuration - elapsed);

            yield return fadeScreen.FadeIn(fadeDuration);

            TransitionFinished?.Invoke();
            isTransitioning = false;
        }
    }
}