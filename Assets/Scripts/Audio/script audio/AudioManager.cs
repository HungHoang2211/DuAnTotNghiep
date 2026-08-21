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
        [SerializeField, Range(0f, 1f)]
        private float masterVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float sfxVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float uiVolume = 1f;

        [SerializeField, Range(0f, 1f)]
        private float musicVolume = 0.8f;

        [SerializeField, Range(0f, 1f)]
        private float ambienceVolume = 0.6f;

        [Header("Music")]
        [SerializeField]
        private float musicFadeDuration = 1.5f;

        [Header("Startup")]
        [SerializeField]
        private AudioCue defaultMusicCue;

        [SerializeField]
        private AudioCue defaultAmbienceCue;

        private AudioSourcePool _sfxPool;

        private AudioSource _musicSource;

        private AudioSource _ambienceSource;

        private readonly Dictionary<AudioCue, AudioSource>
            _activeLoops =
            new Dictionary<AudioCue, AudioSource>();

        private readonly HashSet<AudioSource>
            _gameplayLoopSources =
            new HashSet<AudioSource>();

        private readonly HashSet<AudioSource>
            _pausedGameplayLoopSources =
            new HashSet<AudioSource>();

        private Coroutine _musicFade;

        private float _musicBaseVolume = 1f;

        private float _ambienceBaseVolume = 1f;

        // TRUE khi Settings đang mở
        private bool _gameplayAudioPaused;


        // =========================================================
        // UNITY
        // =========================================================

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

            LoadVolumeSettings();
        }


        private void Start()
        {
            if (defaultMusicCue != null)
            {
                PlayMusic(defaultMusicCue);
            }

            if (defaultAmbienceCue != null)
            {
                PlayAmbience(defaultAmbienceCue);
            }
        }


        // =========================================================
        // LOAD SETTINGS
        // =========================================================

        private void LoadVolumeSettings()
        {
            masterVolume =
                PlayerPrefs.GetFloat(
                    "Audio_MasterVolume",
                    masterVolume
                );

            sfxVolume =
                PlayerPrefs.GetFloat(
                    "Audio_SfxVolume",
                    sfxVolume
                );

            uiVolume =
                PlayerPrefs.GetFloat(
                    "Audio_UiVolume",
                    uiVolume
                );

            ambienceVolume =
                PlayerPrefs.GetFloat(
                    "Audio_AmbienceVolume",
                    ambienceVolume
                );
        }


        // =========================================================
        // BUILD AUDIO SOURCES
        // =========================================================

        private void BuildAudioSources()
        {
            _sfxPool =
                new AudioSourcePool(
                    transform,
                    sfxPoolSize,
                    "SfxSource"
                );

            _musicSource =
                CreateStreamSource("MusicSource");

            _ambienceSource =
                CreateStreamSource("AmbienceSource");

            AddCategory(
                _musicSource,
                AudioCategory.Music,
                1f
            );

            AddCategory(
                _ambienceSource,
                AudioCategory.Ambience,
                1f
            );
        }


        private AudioSource CreateStreamSource(
            string sourceName)
        {
            GameObject holder =
                new GameObject(sourceName);

            holder.transform.SetParent(transform);

            AudioSource source =
                holder.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = true;

            return source;
        }


        // =========================================================
        // CATEGORY
        // =========================================================

        private AudioSourceCategory AddCategory(
            AudioSource source,
            AudioCategory category,
            float baseVolume)
        {
            if (source == null)
                return null;

            AudioSourceCategory component =
                source.GetComponent<AudioSourceCategory>();

            if (component == null)
            {
                component =
                    source.gameObject.AddComponent<AudioSourceCategory>();
            }

            component.SetData(
                category,
                baseVolume
            );

            return component;
        }


        // =========================================================
        // NORMAL SFX
        // =========================================================

        public AudioSource PlaySfx(
            AudioCue cue)
        {
            return PlayOneShot(
                cue,
                Vector3.zero,
                false,
                false
            );
        }


        public AudioSource PlaySfxAt(
            AudioCue cue,
            Vector3 position)
        {
            return PlayOneShot(
                cue,
                position,
                true,
                false
            );
        }


        // =========================================================
        // GAMEPLAY SFX
        // =========================================================

        public AudioSource PlayGameplaySfx(
            AudioCue cue)
        {
            return PlayOneShot(
                cue,
                Vector3.zero,
                false,
                true
            );
        }


        public AudioSource PlayGameplaySfxAt(
            AudioCue cue,
            Vector3 position)
        {
            return PlayOneShot(
                cue,
                position,
                true,
                true
            );
        }


        // =========================================================
        // IMPORTANT SFX
        // =========================================================

        public void PlayImportantSfxAt(
            AudioCue cue,
            Vector3 position)
        {
            if (!IsPlayable(cue))
                return;

            // Nếu đây là gameplay audio
            // thì không cho phát khi Settings đang mở.
            if (_gameplayAudioPaused &&
                cue.Category == AudioCategory.Ui)
            {
                return;
            }

            GameObject holder =
                new GameObject(
                    "ImportantSfx_" +
                    cue.name
                );

            holder.transform.SetParent(transform);

            AudioSource source =
                holder.AddComponent<AudioSource>();

            source.playOnAwake = false;

            ConfigureSource(
                source,
                cue,
                position,
                true
            );

            source.loop = false;

            source.Play();

            StartCoroutine(
                DestroyAfterPlay(
                    holder,
                    source
                )
            );
        }


        private IEnumerator DestroyAfterPlay(
            GameObject holder,
            AudioSource source)
        {
            float duration =
                source.clip != null
                    ? source.clip.length /
                      Mathf.Max(
                          source.pitch,
                          0.01f
                      )
                    : 0f;

            yield return new WaitForSeconds(
                duration
            );

            if (holder != null)
            {
                Destroy(holder);
            }
        }


        // =========================================================
        // PLAY ONE SHOT
        // =========================================================

        private AudioSource PlayOneShot(
            AudioCue cue,
            Vector3 position,
            bool positional,
            bool gameplayAudio)
        {
            if (!IsPlayable(cue))
                return null;

            // Gameplay audio bị khóa khi Settings mở.
            if (gameplayAudio &&
                _gameplayAudioPaused)
            {
                return null;
            }

            if (_sfxPool == null)
                return null;

            AudioSource source =
                _sfxPool.GetAvailable();

            if (source == null)
                return null;

            // Đánh dấu source hiện tại.
            _sfxPool.SetGameplaySource(
                source,
                gameplayAudio
            );

            ConfigureSource(
                source,
                cue,
                position,
                positional
            );

            source.loop = false;

            source.Play();

            return source;
        }


        // =========================================================
        // LOOP
        // =========================================================

        public void StartLoop(
            AudioCue cue)
        {
            StartLoopInternal(
                cue,
                false
            );
        }


        public void StartGameplayLoop(
            AudioCue cue)
        {
            StartLoopInternal(
                cue,
                true
            );
        }


        private void StartLoopInternal(
            AudioCue cue,
            bool gameplayAudio)
        {
            if (!IsPlayable(cue))
                return;

            if (gameplayAudio &&
                _gameplayAudioPaused)
            {
                return;
            }

            if (_activeLoops.ContainsKey(cue))
                return;

            AudioSource source =
                CreateStreamSource(
                    "Loop_" +
                    cue.name
                );

            ConfigureSource(
                source,
                cue,
                Vector3.zero,
                false
            );

            source.Play();

            _activeLoops.Add(
                cue,
                source
            );

            if (gameplayAudio)
            {
                _gameplayLoopSources.Add(source);
            }
        }


        public void StopLoop(
            AudioCue cue)
        {
            if (!_activeLoops.TryGetValue(
                cue,
                out AudioSource source))
            {
                return;
            }

            if (source != null)
            {
                source.Stop();

                _gameplayLoopSources.Remove(
                    source
                );

                _pausedGameplayLoopSources.Remove(
                    source
                );

                Destroy(
                    source.gameObject
                );
            }

            _activeLoops.Remove(cue);
        }


        // =========================================================
        // PAUSE GAMEPLAY AUDIO
        // =========================================================

        public void PauseGameplayAudio()
        {
            _gameplayAudioPaused = true;

            // Pause SFX gameplay
            if (_sfxPool != null)
            {
                _sfxPool.PauseGameplaySources();
            }

            // Pause gameplay loops
            _pausedGameplayLoopSources.Clear();

            foreach (AudioSource source
                     in _gameplayLoopSources)
            {
                if (source == null)
                    continue;

                if (!source.isPlaying)
                    continue;

                source.Pause();

                _pausedGameplayLoopSources.Add(
                    source
                );
            }
        }


        // =========================================================
        // RESUME GAMEPLAY AUDIO
        // =========================================================

        public void ResumeGameplayAudio()
        {
            _gameplayAudioPaused = false;

            // Resume SFX gameplay
            if (_sfxPool != null)
            {
                _sfxPool.ResumeGameplaySources();
            }

            // Resume gameplay loops
            foreach (AudioSource source
                     in _pausedGameplayLoopSources)
            {
                if (source == null)
                    continue;

                source.UnPause();
            }

            _pausedGameplayLoopSources.Clear();
        }


        // =========================================================
        // MUSIC
        // =========================================================

        public void PlayMusic(
            AudioCue cue)
        {
            if (!IsPlayable(cue))
                return;

            RestartMusicFade(
                FadeToTrack(cue)
            );
        }


        public void StopMusic()
        {
            RestartMusicFade(
                FadeOut(_musicSource)
            );
        }


        // =========================================================
        // AMBIENCE
        // =========================================================

        public void PlayAmbience(
            AudioCue cue)
        {
            if (!IsPlayable(cue))
                return;

            _ambienceBaseVolume =
                cue.Volume;

            _ambienceSource.clip =
                cue.PickClip();

            _ambienceSource.volume =
                StreamVolume(
                    AudioCategory.Ambience,
                    _ambienceBaseVolume
                );

            _ambienceSource.Play();
        }


        public void StopAmbience()
        {
            if (_ambienceSource != null)
            {
                _ambienceSource.Stop();
            }
        }


        // =========================================================
        // CONFIGURE SOURCE
        // =========================================================

        private void ConfigureSource(
            AudioSource source,
            AudioCue cue,
            Vector3 position,
            bool positional)
        {
            if (source == null ||
                cue == null)
            {
                return;
            }

            source.transform.position =
                position;

            source.clip =
                cue.PickClip();

            source.pitch =
                cue.PickPitch();

            source.priority =
                cue.Priority;

            source.spatialBlend =
                positional
                    ? cue.SpatialBlend
                    : 0f;

            source.minDistance =
                cue.MinDistance;

            source.maxDistance =
                cue.MaxDistance;

            AddCategory(
                source,
                cue.Category,
                cue.Volume
            );

            source.volume =
                OneShotVolume(cue);
        }


        // =========================================================
        // VOLUME
        // =========================================================

        private float OneShotVolume(
            AudioCue cue)
        {
            if (cue == null)
                return 0f;

            return
                masterVolume *
                CategoryVolume(
                    cue.Category
                ) *
                cue.Volume;
        }


        private float StreamVolume(
            AudioCategory category,
            float baseVolume)
        {
            return
                masterVolume *
                CategoryVolume(category) *
                baseVolume;
        }


        private float CategoryVolume(
            AudioCategory category)
        {
            switch (category)
            {
                case AudioCategory.Ui:
                    return uiVolume;

                case AudioCategory.Music:
                    return musicVolume;

                case AudioCategory.Ambience:
                    return ambienceVolume;

                case AudioCategory.Sfx:
                default:
                    return sfxVolume;
            }
        }


        // =========================================================
        // MASTER
        // =========================================================

        public void SetMasterVolume(
            float value)
        {
            masterVolume =
                Mathf.Clamp01(value);

            RefreshAllVolumes();
        }


        // =========================================================
        // SFX
        // =========================================================

        public void SetSfxVolume(
            float value)
        {
            sfxVolume =
                Mathf.Clamp01(value);

            RefreshCategoryVolumes(
                AudioCategory.Sfx
            );
        }


        // =========================================================
        // UI
        // =========================================================

        public void SetUiVolume(
            float value)
        {
            uiVolume =
                Mathf.Clamp01(value);

            RefreshCategoryVolumes(
                AudioCategory.Ui
            );
        }


        // =========================================================
        // MUSIC
        // =========================================================

        public void SetMusicVolume(
            float value)
        {
            musicVolume =
                Mathf.Clamp01(value);

            RefreshCategoryVolumes(
                AudioCategory.Music
            );

            RefreshStreamVolumes();
        }


        // =========================================================
        // AMBIENCE
        // =========================================================

        public void SetAmbienceVolume(
            float value)
        {
            ambienceVolume =
                Mathf.Clamp01(value);

            RefreshCategoryVolumes(
                AudioCategory.Ambience
            );

            RefreshStreamVolumes();
        }


        // =========================================================
        // REFRESH CATEGORY
        // =========================================================

        private void RefreshCategoryVolumes(
            AudioCategory targetCategory)
        {
            AudioSource[] sources =
                GetComponentsInChildren<AudioSource>(
                    true
                );

            foreach (AudioSource source
                     in sources)
            {
                if (source == null)
                    continue;

                AudioSourceCategory data =
                    source.GetComponent<AudioSourceCategory>();

                if (data == null)
                    continue;

                if (data.Category != targetCategory)
                    continue;

                source.volume =
                    masterVolume *
                    CategoryVolume(
                        data.Category
                    ) *
                    data.BaseVolume;
            }
        }


        // =========================================================
        // REFRESH ALL
        // =========================================================

        private void RefreshAllVolumes()
        {
            AudioSource[] sources =
                GetComponentsInChildren<AudioSource>(
                    true
                );

            foreach (AudioSource source
                     in sources)
            {
                if (source == null)
                    continue;

                AudioSourceCategory data =
                    source.GetComponent<AudioSourceCategory>();

                if (data == null)
                    continue;

                source.volume =
                    masterVolume *
                    CategoryVolume(
                        data.Category
                    ) *
                    data.BaseVolume;
            }
        }


        // =========================================================
        // REFRESH STREAM
        // =========================================================

        private void RefreshStreamVolumes()
        {
            if (_musicSource != null &&
                _musicSource.isPlaying)
            {
                _musicSource.volume =
                    StreamVolume(
                        AudioCategory.Music,
                        _musicBaseVolume
                    );
            }

            if (_ambienceSource != null &&
                _ambienceSource.isPlaying)
            {
                _ambienceSource.volume =
                    StreamVolume(
                        AudioCategory.Ambience,
                        _ambienceBaseVolume
                    );
            }
        }


        // =========================================================
        // MUSIC FADE
        // =========================================================

        private void RestartMusicFade(
            IEnumerator routine)
        {
            if (_musicFade != null)
            {
                StopCoroutine(_musicFade);
            }

            _musicFade =
                StartCoroutine(routine);
        }


        private IEnumerator FadeToTrack(
            AudioCue cue)
        {
            yield return FadeOut(
                _musicSource
            );

            _musicBaseVolume =
                cue.Volume;

            _musicSource.clip =
                cue.PickClip();

            _musicSource.volume = 0f;

            _musicSource.Play();

            yield return FadeIn(
                _musicSource,
                StreamVolume(
                    AudioCategory.Music,
                    _musicBaseVolume
                )
            );

            _musicFade = null;
        }


        private IEnumerator FadeOut(
            AudioSource source)
        {
            if (source == null)
                yield break;

            float start =
                source.volume;

            float elapsed = 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.deltaTime;

                source.volume =
                    Mathf.Lerp(
                        start,
                        0f,
                        elapsed /
                        musicFadeDuration
                    );

                yield return null;
            }

            source.Stop();
        }


        private IEnumerator FadeIn(
            AudioSource source,
            float target)
        {
            if (source == null)
                yield break;

            float elapsed = 0f;

            while (elapsed < musicFadeDuration)
            {
                elapsed += Time.deltaTime;

                source.volume =
                    Mathf.Lerp(
                        0f,
                        target,
                        elapsed /
                        musicFadeDuration
                    );

                yield return null;
            }

            source.volume =
                target;
        }


        // =========================================================
        // PLAYABLE
        // =========================================================

        private bool IsPlayable(
            AudioCue cue)
        {
            return
                cue != null &&
                cue.HasClip;
        }


        // =========================================================
        // PUBLIC VALUES
        // =========================================================

        public float MasterVolume =>
            masterVolume;

        public float SfxVolume =>
            sfxVolume;

        public float UiVolume =>
            uiVolume;

        public float MusicVolume =>
            musicVolume;

        public float AmbienceVolume =>
            ambienceVolume;
    }
}