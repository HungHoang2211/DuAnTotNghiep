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

        [Header("Swap Hysteresis")]
        [Tooltip("Margin (mét) target mới phải gần hơn current target để swap. Tránh flicker giữa 2 target distance gần nhau.")]
        [SerializeField] private float swapHysteresis = 0.5f;

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

            if (CurrentEnemy == target)
            {
                CurrentEnemy = null;
                OnEnemyChanged?.Invoke(null);
            }

            if (CurrentUsable == target)
            {
                CurrentUsable = null;
                OnUsableChanged?.Invoke(null);
            }
        }

        private void Update()
        {
            ITargetable newEnemy = PickBest(CurrentEnemy, enemyOnly: true);
            ITargetable newUsable = PickBest(CurrentUsable, enemyOnly: false);

            UpdateMarker(enemyMarker, newEnemy, true);
            UpdateMarker(usableMarker, newUsable, false);

            if (newEnemy != CurrentEnemy)
            {
                CurrentEnemy = newEnemy;
                OnEnemyChanged?.Invoke(newEnemy);
            }

            if (newUsable != CurrentUsable)
            {
                CurrentUsable = newUsable;
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

        /// <summary>
        /// Pick best target với hysteresis:
        /// - Nếu current target vẫn valid → chỉ swap nếu candidate gần hơn current ÍT NHẤT swapHysteresis mét
        /// - Nếu current target invalid → pick candidate gần nhất ngay
        /// </summary>
        private ITargetable PickBest(ITargetable current, bool enemyOnly)
        {
            ITargetable bestCandidate = null;
            float bestCandidateDist = float.MaxValue;

            float currentDist = float.MaxValue;
            bool currentValid = current != null && current.Transform != null && current.CanBeTargeted();

            if (currentValid)
            {
                bool currentMatchesType = (current.Type == TargetType.Enemy) == enemyOnly;
                if (currentMatchesType)
                    currentDist = ComputeDistance(current);
                else
                    currentValid = false;
            }

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
            CurrentEnemy = null;
            CurrentUsable = null;

            if (enemyMarker != null) enemyMarker.Hide();
            if (usableMarker != null) usableMarker.Hide();
        }
    }
}