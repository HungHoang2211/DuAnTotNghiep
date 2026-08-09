using UnityEngine;
using SimpleSurvival.Audio;

public class StartScreenAudio : MonoBehaviour
{
    public static StartScreenAudio Instance;

    [Header("Audio Sources")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource musicSource;

    [Header("Start Screen Audio Cues")]
    [SerializeField] private AudioCue tapStartCue;
    [SerializeField] private AudioCue newGameCue;
    [SerializeField] private AudioCue confirmYesCue;
    [SerializeField] private AudioCue confirmNoCue;

    [Header("Background Music")]
    [SerializeField] private AudioCue backgroundMusicCue;

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
        PlayBackgroundMusic();
    }

    public void PlayTapStart()
    {
        PlaySFX(tapStartCue);
    }

    public void PlayNewGame()
    {
        PlaySFX(newGameCue);
    }

    public void PlayConfirmYes()
    {
        PlaySFX(confirmYesCue);
    }

    public void PlayConfirmNo()
    {
        PlaySFX(confirmNoCue);
    }

    private void PlaySFX(AudioCue cue)
    {
        if (cue == null)
        {
            Debug.LogWarning("AudioCue chưa được gán!");
            return;
        }

        if (!cue.HasClip)
        {
            Debug.LogWarning("AudioCue không có AudioClip!");
            return;
        }

        AudioClip clip = cue.PickClip();

        if (clip == null)
            return;

        sfxSource.clip = null;

        sfxSource.volume = cue.Volume;
        sfxSource.pitch = cue.PickPitch();
        sfxSource.spatialBlend = cue.SpatialBlend;
        sfxSource.minDistance = cue.MinDistance;
        sfxSource.maxDistance = cue.MaxDistance;
        sfxSource.priority = cue.Priority;

        sfxSource.PlayOneShot(clip);
    }

    private void PlayBackgroundMusic()
    {
        if (backgroundMusicCue == null)
        {
            Debug.LogWarning("Background Music Cue chưa được gán!");
            return;
        }

        if (!backgroundMusicCue.HasClip)
        {
            Debug.LogWarning("Background Music Cue không có AudioClip!");
            return;
        }

        AudioClip clip = backgroundMusicCue.PickClip();

        if (clip == null)
            return;

        musicSource.Stop();

        musicSource.clip = clip;
        musicSource.volume = backgroundMusicCue.Volume;
        musicSource.pitch = backgroundMusicCue.PickPitch();
        musicSource.spatialBlend = backgroundMusicCue.SpatialBlend;
        musicSource.minDistance = backgroundMusicCue.MinDistance;
        musicSource.maxDistance = backgroundMusicCue.MaxDistance;
        musicSource.priority = backgroundMusicCue.Priority;

        musicSource.loop = true;
        musicSource.Play();
    }
}