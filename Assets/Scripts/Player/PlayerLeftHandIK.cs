using UnityEngine;

namespace SimpleSurvival.Player
{
    public sealed class PlayerLeftHandIK : MonoBehaviour
    {
        private static readonly int ParamLeftHand0Weight = Animator.StringToHash("LeftHand0Weight");
        private static readonly int ParamLeftHand1Weight = Animator.StringToHash("LeftHand1Weight");

        [SerializeField] private Animator animator;
        [SerializeField] private float weightDampTime = 0.15f;

        private Transform _leftHand0Target;
        private Transform _leftHand1Target;
        private float _targetWeight0;
        private float _targetWeight1;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
        }

        public void SetTargets(Transform leftHand0Target, Transform leftHand1Target)
        {
            if (leftHand0Target != null)
                _leftHand0Target = leftHand0Target;
            if (leftHand1Target != null)
                _leftHand1Target = leftHand1Target;

            _targetWeight0 = leftHand0Target != null ? 1f : 0f;
            _targetWeight1 = leftHand1Target != null ? 1f : 0f;
        }

        private void Update()
        {
            if (animator == null) return;

            animator.SetFloat(ParamLeftHand0Weight, _targetWeight0, weightDampTime, Time.deltaTime);
            animator.SetFloat(ParamLeftHand1Weight, _targetWeight1, weightDampTime, Time.deltaTime);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (animator == null) return;

            if (_leftHand0Target == null)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                return;
            }

            float rawWeight0 = animator.GetFloat(ParamLeftHand0Weight);
            float rawWeight1 = animator.GetFloat(ParamLeftHand1Weight);

            if (rawWeight0 <= 0.001f && rawWeight1 <= 0.001f)
            {
                animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, 0f);
                animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, 0f);
                return;
            }

            float weight0 = WeightSinus(rawWeight0);
            Vector3 position = _leftHand0Target.position;
            Quaternion rotation = _leftHand0Target.rotation;

            if (_leftHand1Target != null && rawWeight1 > 0.001f)
            {
                float weight1 = WeightSinus(rawWeight1);
                weight0 = Mathf.Clamp01(weight0 + weight1);
                position = Vector3.Lerp(position, _leftHand1Target.position, weight1);
                rotation = Quaternion.Lerp(rotation, _leftHand1Target.rotation, weight1);
            }

            animator.SetIKPositionWeight(AvatarIKGoal.LeftHand, weight0);
            animator.SetIKPosition(AvatarIKGoal.LeftHand, position);
            animator.SetIKRotationWeight(AvatarIKGoal.LeftHand, weight0);
            animator.SetIKRotation(AvatarIKGoal.LeftHand, rotation);
        }

        private static float WeightSinus(float weight)
        {
            float eased = Mathf.Clamp01(weight * (Mathf.PI / 2f) - Mathf.PI / 2f + 1f);
            return eased * eased;
        }
    }
}