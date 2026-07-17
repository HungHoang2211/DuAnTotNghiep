using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Pool")]
        [SerializeField] private int sfxPoolSize = 8;

        [Header("Volumes")]
        [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float sfxVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float uiVolume = 1f;
        [SerializeField, Range(0f, 1f)] private float musicVolume = 0.8f;
        [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.6f;

        [Header("Music")]
        [SerializeField] private float musicFadeDuration = 1.5f;

        [Header("Startup")]
        [SerializeField] private AudioCue defaultMusicCue;
        [SerializeField] private AudioCue defaultAmbienceCue;
       

        private AudioSourcePool _sfxPool;
        private AudioSource _musicSource;
        private AudioSource _ambienceSource;

        private readonly Dictionary<AudioCue, AudioSource> _activeLoops =
            new Dictionary<AudioCue, AudioSource>();

        private Coroutine _musicFade;
        private float _musicBaseVolume = 1f;
        private float _ambienceBaseVolume = 1f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            BuildAudioSources();
        }

        private void Start()
        {
            if (defaultMusicCue != null)
                PlayMusic(defaultMusicCue);

            if (defaultAmbienceCue != null)
                PlayAmbience(defaultAmbienceCue);
        }


        private void BuildAudioSources()
        {
            _sfxPool = new AudioSourcePool(transform, sfxPoolSize, "SfxSource");
            _musicSource = CreateStreamSource("MusicSource");
            _ambienceSource = CreateStreamSource("AmbienceSource");
        }

        private AudioSource CreateStreamSource(string sourceName)
        {
            GameObject holder = new GameObject(sourceName);
            holder.transform.SetParent(transform);

            AudioSource source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = true;
            return source;
        }

        public AudioSource PlaySfx(AudioCue cue)
        {
            return PlayOneShot(cue, Vector3.zero, false);
        }

        public AudioSource PlaySfxAt(AudioCue cue, Vector3 position)
        {
            return PlayOneShot(cue, position, true);
        }

        public void PlayImportantSfxAt(AudioCue cue, Vector3 position)
        {
            if (!IsPlayable(cue))
                return;

            GameObject holder = new GameObject("ImportantSfx_" + cue.name);
            holder.transform.SetParent(transform);

            AudioSource source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            ConfigureSource(source, cue, position, true);
            source.loop = false;
            source.Play();

            StartCoroutine(DestroyAfterPlay(holder, source));
        }

        private IEnumerator DestroyAfterPlay(GameObject holder, AudioSource source)
        {
            float duration = source.clip != null ? source.clip.length / Mathf.Max(source.pitch, 0.01f) : 0f;
            yield return new WaitForSeconds(duration);
            Destroy(holder);
        }

        private AudioSource PlayOneShot(AudioCue cue, Vector3 position, bool positional)
        {
            if (!IsPlayable(cue))
                return null;

            AudioSource source = _sfxPool.GetAvailable();
            ConfigureSource(source, cue, position, positional);
            source.loop = false;
            source.Play();
            return source;
        }

        public void StartLoop(AudioCue cue)
        {
            if (!IsPlayable(cue))
                return;

            if (_activeLoops.ContainsKey(cue))
                return;

            AudioSource source = CreateStreamSource("Loop_" + cue.name);
            ConfigureSource(source, cue, Vector3.zero, false);
            source.Play();
            _activeLoops.Add(cue, source);
        }

        public void StopLoop(AudioCue cue)
        {
            if (!_activeLoops.TryGetValue(cue, out AudioSource source))
                return;

            source.Stop();
            Destroy(source.gameObject);
            _activeLoops.Remove(cue);
        }

        public void PlayMusic(AudioCue cue)
        {
            if (!IsPlayable(cue))
                return;

            RestartMusicFade(FadeToTrack(cue));
        }

        public void StopMusic()
        {
            RestartMusicFade(FadeOut(_musicSource));
        }

        public void PlayAmbience(AudioCue cue)
        {
            if (!IsPlayable(cue))
                return;

            _ambienceBaseVolume = cue.Volume;
            _ambienceSource.clip = cue.PickClip();
            _ambienceSource.volume = StreamVolume(AudioCategory.Ambience, _ambienceBaseVolume);
            _ambienceSource.Play();
        }

        public void StopAmbience()
        {
            _ambienceSource.Stop();
        }

        private void ConfigureSource(AudioSource source, AudioCue cue, Vector3 position, bool positional)
        {
            source.transform.position = position;
            source.clip = cue.PickClip();
            source.volume = OneShotVolume(cue);
            source.pitch = cue.PickPitch();
            source.priority = cue.Priority;
            source.spatialBlend = positional ? cue.SpatialBlend : 0f;
            source.minDistance = cue.MinDistance;
            source.maxDistance = cue.MaxDistance;
        }

        private float OneShotVolume(AudioCue cue)
        {
            return masterVolume * CategoryVolume(cue.Category) * cue.Volume;
        }

        private float StreamVolume(AudioCategory category, float baseVolume)
        {
            return masterVolume * CategoryVolume(category) * baseVolume;
        }

        private float CategoryVolume(AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Ui: return uiVolume;
                case AudioCategory.Music: return musicVolume;
                case AudioCategory.Ambience: return ambienceVolume;
                default: return sfxVolume;
            }
        }

        private bool IsPlayable(AudioCue cue)
        {
            return cue != null && cue.HasClip;
        }

        private void RestartMusicFade(IEnumerator routine)
        {
            if (_musicFade != null)
                StopCoroutine(_musicFade);

            _musicFade = StartCoroutine(routine);
        }

        private IEnumerator FadeToTrack(AudioCue cue)
        {
            yield return FadeOut(_musicSource);

            _musicBaseVolume = cue.Volume;
            _musicSource.clip = cue.PickClip();
            _musicSource.volume = 0f;
            _musicSource.Play();

            yield return FadeIn(_musicSource, StreamVolume(AudioCategory.Music, _musicBaseVolume));
            _musicFade = null;
        }

        private IEnumerator FadeOut(AudioSource source)
        {
            float start = source.volume;
            float elapsed = 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(start, 0f, elapsed / musicFadeDuration);
                yield return null;
            }

            source.Stop();
        }

        private IEnumerator FadeIn(AudioSource source, float target)
        {
            float elapsed = 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.deltaTime;
                source.volume = Mathf.Lerp(0f, target, elapsed / musicFadeDuration);
                yield return null;
            }

            source.volume = target;
        }

        public void SetMasterVolume(float value)
        {
            masterVolume = Mathf.Clamp01(value);
            RefreshStreamVolumes();
        }

        public void SetSfxVolume(float value)
        {
            sfxVolume = Mathf.Clamp01(value);
        }

        public void SetUiVolume(float value)
        {
            uiVolume = Mathf.Clamp01(value);
        }

        public void SetMusicVolume(float value)
        {
            musicVolume = Mathf.Clamp01(value);
            RefreshStreamVolumes();
        }

        public void SetAmbienceVolume(float value)
        {
            ambienceVolume = Mathf.Clamp01(value);
            RefreshStreamVolumes();
        }

        private void RefreshStreamVolumes()
        {
            if (_musicSource.isPlaying)
                _musicSource.volume = StreamVolume(AudioCategory.Music, _musicBaseVolume);

            if (_ambienceSource.isPlaying)
                _ambienceSource.volume = StreamVolume(AudioCategory.Ambience, _ambienceBaseVolume);
        }

        public float MasterVolume => masterVolume;
        public float SfxVolume => sfxVolume;
        public float MusicVolume => musicVolume;
        public float AmbienceVolume => ambienceVolume;
        public float UiVolume => uiVolume;
    }
}