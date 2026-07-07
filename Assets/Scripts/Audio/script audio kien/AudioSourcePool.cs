using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Audio
{
    public class AudioSourcePool
    {
        private readonly List<AudioSource> _sources = new List<AudioSource>();
        private int _nextIndex;

        public AudioSourcePool(Transform parent, int size, string sourceName)
        {
            for (int i = 0; i < size; i++)
                _sources.Add(CreateSource(parent, sourceName + i));
        }

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
                if (!source.isPlaying)
                    return source;
            }

            return null;
        }

        private AudioSource StealNextSource()
        {
            AudioSource source = _sources[_nextIndex];
            _nextIndex = (_nextIndex + 1) % _sources.Count;
            source.Stop();
            return source;
        }

        private AudioSource CreateSource(Transform parent, string sourceName)
        {
            GameObject holder = new GameObject(sourceName);
            holder.transform.SetParent(parent);

            AudioSource source = holder.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.rolloffMode = AudioRolloffMode.Linear;
            return source;
        }
    }
}
