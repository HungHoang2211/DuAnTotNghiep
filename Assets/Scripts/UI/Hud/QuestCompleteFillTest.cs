using UnityEngine;
using UnityEngine.UI;

public class QuestCompleteFillTest : MonoBehaviour
{
    public Image FillOverlay;
    public float FillInDuration = 0.6f;
    public float FillOutDuration = 0.5f;
    public float PauseBetweenLoops = 0.3f;

    private bool _playing;

    public void PlayEffect()
    {
        _playing = true;
    }

    public void StopEffect()
    {
        _playing = false;
        if (FillOverlay != null) FillOverlay.fillAmount = 0f;
    }

    private void Update()
    {
        if (!_playing || FillOverlay == null) return;

        float cycle = FillInDuration + FillOutDuration + PauseBetweenLoops;
        if (cycle <= 0f) return;

        float t = Time.time % cycle;

        if (t < FillInDuration)
        {
            FillOverlay.fillAmount = FillInDuration > 0f ? Mathf.Clamp01(t / FillInDuration) : 1f;
        }
        else if (t < FillInDuration + FillOutDuration)
        {
            float localT = t - FillInDuration;
            FillOverlay.fillAmount = FillOutDuration > 0f ? Mathf.Clamp01(1f - localT / FillOutDuration) : 0f;
        }
        else
        {
            FillOverlay.fillAmount = 0f;
        }
    }
}