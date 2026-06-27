using UnityEngine;

namespace SimpleSurvival.AI
{
    [RequireComponent(typeof(Animator))]
    public abstract class BaseEnemyAnimator : MonoBehaviour
    {
        protected Animator _animator;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
        }

        public abstract void SetMoving(bool moving);
        public abstract void SetIdle();
        public abstract void TriggerAttack(int attackIndex);
        public abstract void TriggerDeath();
        public abstract void ResetForSpawn();

        public virtual void CancelAttack() { }
    }
}