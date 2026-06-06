using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Targets
{
    public class PlayerTargetChecker : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;

        [Header("Visual Markers")]
        [SerializeField] private TargetMarker enemyMarker;
        [SerializeField] private TargetMarker usableMarker;

        public ITargetable CurrentEnemy { get; private set; }
        public ITargetable CurrentUsable { get; private set; }

        public event Action<ITargetable> OnEnemyChanged;
        public event Action<ITargetable> OnUsableChanged;

        private readonly List<ITargetable> _visibleTargets = new List<ITargetable>();

        private void Awake()
        {
            if (playerTransform == null)
                playerTransform = transform;
        }

        public void OnTargetEnter(Collider other)
        {
            ITargetable target = other.GetComponentInParent<ITargetable>();
            if (target == null) return;
            if (_visibleTargets.Contains(target)) return;

            _visibleTargets.Add(target);
            target.OnDestroyed += HandleTargetDestroyed;
        }

        public void OnTargetExit(Collider other)
        {
            ITargetable target = other.GetComponentInParent<ITargetable>();
            if (target == null) return;
            RemoveTarget(target);
        }

        private void HandleTargetDestroyed(ITargetable target) => RemoveTarget(target);

        private void RemoveTarget(ITargetable target)
        {
            if (!_visibleTargets.Remove(target)) return;
            target.OnDestroyed -= HandleTargetDestroyed;
        }

        private void Update()
        {
            ITargetable newEnemy = FindClosest(true);
            ITargetable newUsable = FindClosest(false);

            if (newEnemy != CurrentEnemy)
            {
                CurrentEnemy = newEnemy;
                UpdateMarker(enemyMarker, newEnemy, true);
                OnEnemyChanged?.Invoke(newEnemy);
            }

            if (newUsable != CurrentUsable)
            {
                CurrentUsable = newUsable;
                UpdateMarker(usableMarker, newUsable, false);
                OnUsableChanged?.Invoke(newUsable);
            }
        }

        private void UpdateMarker(TargetMarker marker, ITargetable target, bool followTransform)
        {
            if (marker == null) return;

            if (target == null || target.Transform == null)
            {
                marker.Hide();
                return;
            }

            if (followTransform)
                marker.Show(target.Transform, target.Radius);
            else
                marker.Show(target.Transform.position, target.Radius);
        }

        private ITargetable FindClosest(bool enemyOnly)
        {
            ITargetable best = null;
            float bestDistance = float.MaxValue;
            Vector3 playerPos = playerTransform.position;

            for (int i = _visibleTargets.Count - 1; i >= 0; i--)
            {
                ITargetable t = _visibleTargets[i];

                if (t == null || t.Transform == null)
                {
                    _visibleTargets.RemoveAt(i);
                    continue;
                }

                bool isEnemy = t.Type == TargetType.Enemy;
                if (enemyOnly != isEnemy) continue;
                if (!t.CanBeTargeted()) continue;

                float dist = Vector3.Distance(t.Transform.position, playerPos) - t.Radius;
                if (dist < 0f) dist = 0f;

                if (dist < bestDistance)
                {
                    bestDistance = dist;
                    best = t;
                }
            }

            return best;
        }

        private void OnDisable()
        {
            foreach (var target in _visibleTargets)
            {
                if (target != null)
                    target.OnDestroyed -= HandleTargetDestroyed;
            }
            _visibleTargets.Clear();
            CurrentEnemy = null;
            CurrentUsable = null;

            if (enemyMarker != null) enemyMarker.Hide();
            if (usableMarker != null) usableMarker.Hide();
        }
    }
}