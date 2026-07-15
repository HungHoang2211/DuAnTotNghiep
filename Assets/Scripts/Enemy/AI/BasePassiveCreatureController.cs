using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using SimpleSurvival.Audio;
using SimpleSurvival.Input;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public abstract class BasePassiveCreatureController : BaseAIController
    {
        protected enum PassiveState { Wandering, Grazing, Fleeing, Dead }
        protected PassiveState _state = PassiveState.Wandering;

        protected EnemyHearing _hearing;
        protected PlayerInputReader _playerInput;

        protected override void Awake()
        {
            base.Awake();
            _hearing = GetComponent<EnemyHearing>();
            if (_hearing != null)
                _hearing.OnSoundHeard += HandleSoundHeard;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_hearing != null)
                _hearing.OnSoundHeard -= HandleSoundHeard;
        }

        protected override void OnInitialized()
        {
            _state = PassiveState.Wandering;

            if (_playerInput == null)
            {
                var playerGO = GameObject.FindWithTag("Player");
                if (playerGO != null)
                    _playerInput = playerGO.GetComponentInChildren<PlayerInputReader>();
            }

            OnPassiveInitialized();
        }

        protected abstract void OnPassiveInitialized();

        protected virtual void HandleSoundHeard(SoundEvent soundEvent)
        {
            if (_isDead || _state == PassiveState.Fleeing) return;

            bool playerSneaking = _playerInput != null && _playerInput.IsSneakHeld;

            switch (soundEvent.Type)
            {
                case SoundType.AttackHit:
                case SoundType.Gunshot:
                    StartCoroutine(FleeFrom(soundEvent.Position));
                    break;

                case SoundType.GatherHit:
                case SoundType.Footstep:
                    if (!playerSneaking)
                        StartCoroutine(FleeFrom(soundEvent.Position));
                    break;
            }
        }

        protected abstract IEnumerator FleeFrom(Vector3 dangerPosition);

        protected override void HandleDamagedBy(GameObject source)
        {
            if (source == null || _isDead) return;
            StartCoroutine(FleeFrom(source.transform.position));
        }

        protected override void HandleDeath(GameObject source)
        {
            if (_isDead) return;
            _isDead = true;
            _state = PassiveState.Dead;

            StopAllCoroutines();
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.enabled = false;

            OnDying();
        }

        protected abstract void OnDying();
    }
}