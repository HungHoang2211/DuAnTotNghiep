using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.AI
{
    public sealed class EscortEncounterManager : MonoBehaviour
    {
        public static EscortEncounterManager Instance { get; private set; }

        private readonly Queue<MonoBehaviour> _waitingQueue = new Queue<MonoBehaviour>();
        private MonoBehaviour _currentAttacker;

        public MonoBehaviour CurrentAttacker => _currentAttacker;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public bool RequestEngage(MonoBehaviour enemy)
        {
            if (enemy == null) return false;

            if (_currentAttacker == null)
            {
                _currentAttacker = enemy;
                return true;
            }

            if (_currentAttacker == enemy) return true;

            if (!_waitingQueue.Contains(enemy))
                _waitingQueue.Enqueue(enemy);

            return false;
        }

        public void ReleaseEngage(MonoBehaviour enemy)
        {
            if (_currentAttacker != enemy) return;

            _currentAttacker = null;

            while (_waitingQueue.Count > 0)
            {
                var next = _waitingQueue.Dequeue();
                if (next == null) continue;
                _currentAttacker = next;
                break;
            }
        }

        public void ResetEncounter()
        {
            _currentAttacker = null;
            _waitingQueue.Clear();
        }
    }
}