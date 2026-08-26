using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SimpleSurvival.Quests
{
    public sealed class QuestSlotReadyEffect : MonoBehaviour
    {
        [SerializeField] private Image readyBorderImage;
        [SerializeField] private float orbitDuration = 2f;

        private RectTransform _rectTransform;
        private TrailRenderer _trail;
        private Coroutine _orbitRoutine;
        private float _distanceTraveled;
        private int _currentSegment;
        private readonly Vector3[] _corners = new Vector3[4];

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
            _trail = GetComponentInChildren<TrailRenderer>(true);

            if (readyBorderImage != null) readyBorderImage.gameObject.SetActive(false);
            if (_trail != null) _trail.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            SetActive(false);
        }

        public void SetActive(bool value)
        {
            if (readyBorderImage != null) readyBorderImage.gameObject.SetActive(value);

            if (value)
            {
                if (_orbitRoutine == null)
                {
                    _distanceTraveled = 0f;
                    _currentSegment = -1;
                    if (_trail != null)
                    {
                        _trail.gameObject.SetActive(true);
                        _trail.Clear();
                        _trail.emitting = true;
                    }
                    _orbitRoutine = StartCoroutine(OrbitRoutine());
                }
            }
            else
            {
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
        }

        private IEnumerator OrbitRoutine()
        {
            while (true)
            {
                _rectTransform.GetWorldCorners(_corners);

                float d0 = Vector3.Distance(_corners[0], _corners[1]);
                float d1 = Vector3.Distance(_corners[1], _corners[2]);
                float d2 = Vector3.Distance(_corners[2], _corners[3]);
                float d3 = Vector3.Distance(_corners[3], _corners[0]);
                float perimeter = d0 + d1 + d2 + d3;

                if (perimeter > 0f && _trail != null)
                {
                    _distanceTraveled += (perimeter / orbitDuration) * Time.deltaTime;
                    _distanceTraveled %= perimeter;

                    int segment = ResolveSegment(_distanceTraveled, d0, d1, d2, d3, out float localDistance, out float segmentLength);

                    if (segment != _currentSegment)
                    {
                        _currentSegment = segment;
                        _trail.AddPosition(_corners[segment]);
                    }

                    Vector3 point = Vector3.Lerp(_corners[segment], _corners[(segment + 1) % 4], localDistance / segmentLength);
                    _trail.transform.position = point;
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