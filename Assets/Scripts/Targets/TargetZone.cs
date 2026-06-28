using System;
using System.Collections.Generic;
using UnityEngine;

namespace SimpleSurvival.Targets
{
    public class TargetZone : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private Transform playerTransform;
        [Tooltip("Margin (m) candidate phải gần hơn current ít nhất bao nhiêu để swap. Tránh flicker.")]
        [SerializeField] private float swapHysteresis = 0.5f;

        public ITargetable CurrentTarget { get; private set; }
        public event Action<ITargetable> OnBestTargetChanged;

        private readonly List<ITargetable> _visibleTargets = new List<ITargetable>();

        private void Awake()
        {
            if (playerTransform == null)
                playerTransform = transform.parent != null ? transform.parent : transform;
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

            if (CurrentTarget == target)
            {
                CurrentTarget = null;
                OnBestTargetChanged?.Invoke(null);
            }
        }

        private void Update()
        {
            ITargetable newBest = PickBest(CurrentTarget);

            if (newBest != CurrentTarget)
            {
                CurrentTarget = newBest;
                OnBestTargetChanged?.Invoke(newBest);
            }
        }

        private ITargetable PickBest(ITargetable current)
        {
            ITargetable bestCandidate = null;
            float bestCandidateDist = float.MaxValue;

            float currentDist = float.MaxValue;
            bool currentValid = current != null && current.Transform != null && current.CanBeTargeted();

            if (currentValid)
                currentDist = ComputeDistance(current);

            for (int i = _visibleTargets.Count - 1; i >= 0; i--)
            {
                ITargetable t = _visibleTargets[i];

                if (t == null || t.Transform == null)
                {
                    _visibleTargets.RemoveAt(i);
                    continue;
                }

                if (!t.CanBeTargeted()) continue;

                float dist = ComputeDistance(t);

                if (dist < bestCandidateDist)
                {
                    bestCandidateDist = dist;
                    bestCandidate = t;
                }
            }

            if (bestCandidate == null) return null;
            if (!currentValid) return bestCandidate;
            if (bestCandidate == current) return current;

            if (bestCandidateDist < currentDist - swapHysteresis)
                return bestCandidate;

            return current;
        }

        private float ComputeDistance(ITargetable target)
        {
            Vector3 playerPos = playerTransform.position;

            if (target.DistanceCollider != null)
            {
                Vector3 closestPoint = target.DistanceCollider.ClosestPoint(playerPos);
                return Vector3.Distance(playerPos, closestPoint);
            }

            float dist = Vector3.Distance(target.Transform.position, playerPos) - target.Radius;
            return dist < 0f ? 0f : dist;
        }

        private void OnDisable()
        {
            foreach (var target in _visibleTargets)
            {
                if (target != null)
                    target.OnDestroyed -= HandleTargetDestroyed;
            }
            _visibleTargets.Clear();
            CurrentTarget = null;
        }
    }
}