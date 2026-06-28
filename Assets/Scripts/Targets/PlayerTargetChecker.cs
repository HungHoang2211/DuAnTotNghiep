using System;
using UnityEngine;

namespace SimpleSurvival.Targets
{
    public class PlayerTargetChecker : MonoBehaviour
    {
        [Header("Zones")]
        [SerializeField] private TargetZone enemyZone;
        [SerializeField] private TargetZone useZone;

        [Header("Visual Markers")]
        [SerializeField] private TargetMarker enemyMarker;
        [SerializeField] private TargetMarker usableMarker;

        public ITargetable CurrentEnemy { get; private set; }
        public ITargetable CurrentUsable { get; private set; }

        public event Action<ITargetable> OnEnemyChanged;
        public event Action<ITargetable> OnUsableChanged;

        private void OnEnable()
        {
            if (enemyZone != null) enemyZone.OnBestTargetChanged += HandleEnemyChanged;
            if (useZone != null) useZone.OnBestTargetChanged += HandleUsableChanged;
        }

        private void OnDisable()
        {
            if (enemyZone != null) enemyZone.OnBestTargetChanged -= HandleEnemyChanged;
            if (useZone != null) useZone.OnBestTargetChanged -= HandleUsableChanged;

            if (enemyMarker != null) enemyMarker.Hide();
            if (usableMarker != null) usableMarker.Hide();
        }

        private void Update()
        {
            UpdateMarker(enemyMarker, CurrentEnemy, true);
            UpdateMarker(usableMarker, CurrentUsable, false);
        }

        private void HandleEnemyChanged(ITargetable target)
        {
            CurrentEnemy = target;
            OnEnemyChanged?.Invoke(target);
        }

        private void HandleUsableChanged(ITargetable target)
        {
            CurrentUsable = target;
            OnUsableChanged?.Invoke(target);
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
    }
}