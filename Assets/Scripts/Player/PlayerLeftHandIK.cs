using UnityEngine;

namespace SimpleSurvival.Player
{
    public sealed class PlayerLeftHandIK : MonoBehaviour
    {
        private static readonly int ParamLeftHand0Weight = Animator.StringToHash("LeftHand0Weight");
        private static readonly int ParamLeftHand1Weight = Animator.StringToHash("LeftHand1Weight");

        [SerializeField] private Animator animator;

        private Transform _leftHand0Target;
        private Transform _leftHand1Target;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
        }

        public void SetTargets(Transform leftHand0Target, Transform leftHand1Target)
        {
            _leftHand0Target = leftHand0Target;
            _leftHand1Target = leftHand1Target;

            if (animator == null) return;

            animator.SetFloat(ParamLeftHand0Weight, leftHand0Target != null ? 1f : 0f);
            animator.SetFloat(ParamLeftHand1Weight, leftHand1Target != null ? 1f : 0f);
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (_leftHand0Target == null || animator == null) return;

            float weight0 = WeightSinus(animator.GetFloat(ParamLeftHand0Weight));
            Vector3 position = _leftHand0Target.position;
            Quaternion rotation = _leftHand0Target.rotation;

            if (_leftHand1Target != null)
            {
                float weight1 = WeightSinus(animator.GetFloat(ParamLeftHand1Weight));
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