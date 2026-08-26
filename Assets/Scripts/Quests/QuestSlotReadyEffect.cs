using System.Collections;
using UnityEngine;

namespace SimpleSurvival.Quests
{
    public sealed class QuestSlotReadyEffect : MonoBehaviour
    {
        [SerializeField] private QuestCompleteFillTest fillEffect;
        [SerializeField] private float orbitDuration = 2f;

        private RectTransform _rectTransform;
        private TrailRenderer _trail;
        private Coroutine _orbitRoutine;
        private int _currentSegment;
        private bool _wantsActive;
        private readonly Vector3[] _corners = new Vector3[4];

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _trail = GetComponentInChildren<TrailRenderer>(true);

            if (_trail != null) _trail.gameObject.SetActive(false);
            if (fillEffect != null) fillEffect.StopEffect();
        }

        private void OnEnable()
        {
            if (_wantsActive) StartEffect();
        }

        private void OnDisable()
        {
            StopEffectInternal();
        }

        public void SetActive(bool value)
        {
            _wantsActive = value;

            if (value) StartEffect();
            else StopEffectInternal();
        }

        private void StartEffect()
        {
            if (fillEffect != null) fillEffect.PlayEffect();

            if (_orbitRoutine == null)
            {
                _currentSegment = -1;
                _orbitRoutine = StartCoroutine(OrbitRoutine());
            }
        }

        private void StopEffectInternal()
        {
            if (fillEffect != null) fillEffect.StopEffect();

            if (_orbitRoutine != null)
            {
                StopCoroutine(_orbitRoutine);
                _orbitRoutine = null;
            }
            if (_trail != null)
            {
                _trail.emitting = false;
                _trail.gameObject.SetActive(false);
            }
        }

        private IEnumerator OrbitRoutine()
        {
            while (true)
            {
                bool blocked = TrailVisibilityGate.IsBlocked;

                if (blocked)
                {
                    if (_trail != null && _trail.gameObject.activeSelf)
                    {
                        _trail.emitting = false;
                        _trail.gameObject.SetActive(false);
                    }
                }
                else
                {
                    if (_trail != null && !_trail.gameObject.activeSelf)
                    {
                        _trail.gameObject.SetActive(true);
                        _trail.Clear();
                        _trail.emitting = true;
                        _currentSegment = -1;
                    }

                    _rectTransform.GetWorldCorners(_corners);

                    float d0 = Vector3.Distance(_corners[0], _corners[1]);
                    float d1 = Vector3.Distance(_corners[1], _corners[2]);
                    float d2 = Vector3.Distance(_corners[2], _corners[3]);
                    float d3 = Vector3.Distance(_corners[3], _corners[0]);
                    float perimeter = d0 + d1 + d2 + d3;

                    if (perimeter > 0f && _trail != null && orbitDuration > 0f)
                    {
                        float progress = (Time.time % orbitDuration) / orbitDuration;
                        float distanceTraveled = progress * perimeter;

                        int segment = ResolveSegment(distanceTraveled, d0, d1, d2, d3, out float localDistance, out float segmentLength);

                        if (segment != _currentSegment)
                        {
                            _currentSegment = segment;
                            _trail.AddPosition(_corners[segment]);
                        }

                        Vector3 point = Vector3.Lerp(_corners[segment], _corners[(segment + 1) % 4], localDistance / segmentLength);
                        _trail.transform.position = point;
                    }
                }

                yield return null;
            }
        }

        private int ResolveSegment(float distance, float d0, float d1, float d2, float d3, out float localDistance, out float segmentLength)
        {
            if (distance < d0)
            {
                localDistance = distance;
                segmentLength = d0;
                return 0;
            }

            distance -= d0;
            if (distance < d1)
            {
                localDistance = distance;
                segmentLength = d1;
                return 1;
            }

            distance -= d1;
            if (distance < d2)
            {
                localDistance = distance;
                segmentLength = d2;
                return 2;
            }

            distance -= d2;
            localDistance = distance;
            segmentLength = d3;
            return 3;
        }
    }
}