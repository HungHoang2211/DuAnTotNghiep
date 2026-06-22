using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class Weather_SoundFade : MonoBehaviour
{
    private enum Fade { IN, OUT }
    private AudioSource _audioSource;

    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    public void FadeAudioIn(float fTimeToFadeIn, float fEndVolume)
    {
        if (_audioSource == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeAudio(fTimeToFadeIn, fEndVolume, Fade.IN));
    }

    public void FadeAudioOut(float fTimeToFadeOut, float fEndVolume)
    {
        if (_audioSource == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeAudio(fTimeToFadeOut, fEndVolume, Fade.OUT));
    }

    IEnumerator FadeAudio(float fTimeToFade, float fSoundVolume, Fade fadeType)
    {
        float startVolume = _audioSource.volume;
        float currentTime = 0.0f;

        while (currentTime < fTimeToFade)
        {
            currentTime += Time.deltaTime;
            float progress = currentTime / fTimeToFade;
            _audioSource.volume = Mathf.Lerp(startVolume, fSoundVolume, progress);
            yield return null;
        }

        _audioSource.volume = fSoundVolume;
        if (fadeType == Fade.OUT && fSoundVolume <= 0.01f)
        {
            _audioSource.Stop();
        }
    }
}