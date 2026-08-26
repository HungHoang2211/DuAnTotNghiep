using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class QuestCompleteFillTest : MonoBehaviour
{
    public Image FillOverlay;
    public float FillInDuration = 0.2f;
    public float FillOutDuration = 0.15f;

    private Coroutine _routine;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.V))
        {
            PlayEffect();
        }
    }

    public void PlayEffect()
    {
        if (_routine != null)
        {
            StopCoroutine(_routine);
        }
        _routine = StartCoroutine(FillRoutine());
    }

    private IEnumerator FillRoutine()
    {
        yield return Fill(0f, 1f, FillInDuration);
        yield return Fill(1f, 0f, FillOutDuration);
    }

    private IEnumerator Fill(float from, float to, float duration)
    {
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = Mathf.Clamp01(time / duration);
            FillOverlay.fillAmount = Mathf.Lerp(from, to, t);
            yield return null;
        }

        FillOverlay.fillAmount = to;
    }
}