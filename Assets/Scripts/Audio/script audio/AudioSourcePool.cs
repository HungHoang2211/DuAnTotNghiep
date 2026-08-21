using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class AudioSourcePool
    {
        private readonly List<AudioSource> _sources =
            new List<AudioSource>();

        // Những source đang được pause bởi Settings
        private readonly HashSet<AudioSource> _pausedSources =
            new HashSet<AudioSource>();

        // Những source hiện tại được đánh dấu là Gameplay Audio
        private readonly HashSet<AudioSource> _gameplaySources =
            new HashSet<AudioSource>();

        private int _nextIndex;

        public AudioSourcePool(
            Transform parent,
            int size,
            string sourceName)
        {
            if (size <= 0)
                size = 1;

            for (int i = 0; i < size; i++)
            {
                _sources.Add(
                    CreateSource(
                        parent,
                        sourceName + i
                    )
                );
            }
        }

        // =========================================================
        // GET AVAILABLE
        // =========================================================

        public AudioSource GetAvailable()
        {
            AudioSource idle = FindIdleSource();

            if (idle != null)
                return idle;

            return StealNextSource();
        }

        private AudioSource FindIdleSource()
        {
            foreach (AudioSource source in _sources)
            {
                if (source == null)
                    continue;

                // Source đang bị Settings pause
                // không được lấy ra để phát âm thanh khác.
                if (_pausedSources.Contains(source))
                    continue;

                if (!source.isPlaying)
                    return source;
            }

            return null;
        }

        private AudioSource StealNextSource()
        {
            int count = _sources.Count;

            for (int i = 0; i < count; i++)
            {
                AudioSource source =
                    _sources[_nextIndex];

                _nextIndex =
                    (_nextIndex + 1) % count;

                if (source == null)
                    continue;

                // Không steal source đang bị pause.
                if (_pausedSources.Contains(source))
                    continue;

                source.Stop();

                return source;
            }

            return null;
        }

        // =========================================================
        // GAMEPLAY FLAG
        // =========================================================

        public void SetGameplaySource(
            AudioSource source,
            bool isGameplay)
        {
            if (source == null)
                return;

            if (isGameplay)
            {
                _gameplaySources.Add(source);
            }
            else
            {
                _gameplaySources.Remove(source);
            }
        }

        // =========================================================
        // PAUSE GAMEPLAY
        // =========================================================

        public void PauseGameplaySources()
        {
            _pausedSources.Clear();

            foreach (AudioSource source in _gameplaySources)
            {
                if (source == null)
                    continue;

                if (!source.isPlaying)
                    continue;

                source.Pause();

                _pausedSources.Add(source);
            }
        }

        // =========================================================
        // RESUME GAMEPLAY
        // =========================================================

        public void ResumeGameplaySources()
        {
            foreach (AudioSource source in _pausedSources)
            {
                if (source == null)
                    continue;

                source.UnPause();
            }

            _pausedSources.Clear();
        }

        // =========================================================
        // STOP ALL
        // =========================================================

        public void StopAll()
        {
            foreach (AudioSource source in _sources)
            {
                if (source == null)
                    continue;

                source.Stop();
            }

            _pausedSources.Clear();
        }

        // =========================================================
        // REFRESH VOLUME
        // =========================================================

        public void RefreshVolumes(
            System.Action<AudioSource> action)
        {
            foreach (AudioSource source in _sources)
            {
                if (source != null && source.isPlaying)
                {
                    action?.Invoke(source);
                }
            }
        }

        // =========================================================
        // CREATE SOURCE
        // =========================================================

        private AudioSource CreateSource(
            Transform parent,
            string sourceName)
        {
            GameObject holder =
                new GameObject(sourceName);

            holder.transform.SetParent(parent);

            AudioSource source =
                holder.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.rolloffMode =
                AudioRolloffMode.Linear;

            return source;
        }
    }
}