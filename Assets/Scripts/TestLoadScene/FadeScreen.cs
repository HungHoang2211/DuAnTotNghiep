using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.World
{
    public class FadeScreen : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject loadingIndicator;
        [SerializeField] private TMP_Text loadingText;

        [Header("Loading illustration (static, random per load)")]
        [SerializeField] private Image loadingImage;
        [SerializeField] private Sprite[] loadingSprites;

        [Header("Spinner (rotating ring, always visible while loading)")]
        [SerializeField] private RectTransform spinnerTransform;
        [SerializeField] private float spinnerRotationSpeed = 180f;

        [Header("Loading text bounce")]
        [SerializeField] private float bounceAmplitude = 6f;
        [SerializeField] private float bounceSpeed = 6f;
        [SerializeField] private float bounceCharacterOffset = 0.3f;

        [Header("Done text")]
        [SerializeField] private string doneText = "Done";
        [SerializeField] private float doneDisplayDuration = 0.3f;

        private Coroutine bounceRoutine;
        private Coroutine spinRoutine;
        private string originalLoadingText;

        private void Awake()
        {
            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();

            if (loadingText != null)
                originalLoadingText = loadingText.text;

            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            ShowLoading(true);
        }

        public void SetBlack()
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            ShowLoading(true);
        }

        public IEnumerator FadeOut(float duration)
        {
            canvasGroup.blocksRaycasts = true;
            ShowLoading(true);
            yield return Fade(canvasGroup.alpha, 1f, duration);
        }

        public IEnumerator FadeIn(float duration)
        {
            if (loadingText != null)
            {
                loadingText.text = doneText;
                yield return new WaitForSecondsRealtime(doneDisplayDuration);
            }

            yield return Fade(canvasGroup.alpha, 0f, duration);
            ShowLoading(false);
            canvasGroup.blocksRaycasts = false;
        }

        private IEnumerator Fade(float from, float to, float duration)
        {
            if (duration <= 0f)
            {
                canvasGroup.alpha = to;
                yield break;
            }

            float time = 0f;
            while (time < duration)
            {
                time += Time.unscaledDeltaTime;
                canvasGroup.alpha = Mathf.Lerp(from, to, time / duration);
                yield return null;
            }

            canvasGroup.alpha = to;
        }

        private void ShowLoading(bool visible)
        {
            if (loadingIndicator != null)
                loadingIndicator.SetActive(visible);

            if (visible)
            {
                if (loadingText != null)
                    loadingText.text = originalLoadingText;

                PickRandomSprite();
                StartLoadingAnimations();
            }
            else
            {
                StopLoadingAnimations();
            }
        }

        private void PickRandomSprite()
        {
            if (loadingImage == null || loadingSprites == null || loadingSprites.Length == 0) return;

            loadingImage.sprite = loadingSprites[Random.Range(0, loadingSprites.Length)];
        }

        private void StartLoadingAnimations()
        {
            StopLoadingAnimations();
            bounceRoutine = StartCoroutine(BounceTextRoutine());
            spinRoutine = StartCoroutine(SpinRoutine());
        }

        private void StopLoadingAnimations()
        {
            if (bounceRoutine != null)
            {
                StopCoroutine(bounceRoutine);
                bounceRoutine = null;
            }

            if (spinRoutine != null)
            {
                StopCoroutine(spinRoutine);
                spinRoutine = null;
            }
        }

        private IEnumerator SpinRoutine()
        {
            if (spinnerTransform == null) yield break;

            while (true)
            {
                spinnerTransform.Rotate(0f, 0f, -spinnerRotationSpeed * Time.unscaledDeltaTime);
                yield return null;
            }
        }

        private IEnumerator BounceTextRoutine()
        {
            if (loadingText == null) yield break;

            while (true)
            {
                loadingText.ForceMeshUpdate();
                TMP_TextInfo textInfo = loadingText.textInfo;

                for (int i = 0; i < textInfo.characterCount; i++)
                {
                    TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                    if (!charInfo.isVisible) continue;

                    int materialIndex = charInfo.materialReferenceIndex;
                    Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

                    float offset = Mathf.Sin(Time.unscaledTime * bounceSpeed + i * bounceCharacterOffset) * bounceAmplitude;

                    int vertexIndex = charInfo.vertexIndex;
                    vertices[vertexIndex + 0].y += offset;
                    vertices[vertexIndex + 1].y += offset;
                    vertices[vertexIndex + 2].y += offset;
                    vertices[vertexIndex + 3].y += offset;
                }

                for (int i = 0; i < textInfo.meshInfo.Length; i++)
                {
                    TMP_MeshInfo meshInfo = textInfo.meshInfo[i];
                    meshInfo.mesh.vertices = meshInfo.vertices;
                    loadingText.UpdateGeometry(meshInfo.mesh, i);
                }

                yield return null;
            }
        }
    }
}