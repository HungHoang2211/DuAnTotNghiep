using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public abstract class BaseEnemyController : BaseAIController
    {
        protected enum EnemyState { Idle, Detected, Chasing, Attacking, Dead }
        protected EnemyState _state = EnemyState.Idle;

        [Header("Skills (compose qua Inspector)")]
        [SerializeField] protected List<BaseEnemySkill> _skills = new List<BaseEnemySkill>();

        protected float _lostTargetTimer;

        protected EnemyStatsConfig Config => _stats != null ? _stats.EnemyConfig : null;

        protected override void OnInitialized()
        {
            if (Config == null)
            {
                Debug.LogError($"[{name}] EnemyStatsConfig missing", this);
                return;
            }

            _state = EnemyState.Idle;
            _lostTargetTimer = 0f;
            _agent.speed = Config.MoveSpeed;

            OnEnemyInitialized();

            StopAllCoroutines();
            StartCoroutine(DetectionRoutine());
        }

        protected abstract void OnEnemyInitialized();

        protected virtual IEnumerator DetectionRoutine()
        {
            while (!_isDead)
            {
                yield return new WaitForSeconds(0.2f);
                if (_state == EnemyState.Dead) yield break;
                if (_state != EnemyState.Idle) continue;

                if (DetectByVision() || DetectByHearing())
                    OnPlayerDetected();
            }
        }

        protected virtual void OnPlayerDetected()
        {
            BeginChase();
        }

        protected virtual void BeginChase()
        {
            if (Config == null) return;
            _state = EnemyState.Chasing;
            _agent.isStopped = false;
            _agent.speed = Config.MoveSpeed;
            _lostTargetTimer = 0f;
        }

        protected virtual void BeginIdle()
        {
            _state = EnemyState.Idle;
            _agent.isStopped = true;
            _agent.ResetPath();
            _player = null;
            _lostTargetTimer = 0f;
        }

        protected virtual void Update()
        {
            if (_isDead) return;
            if (_state == EnemyState.Chasing) UpdateChase();
            else if (_state == EnemyState.Attacking) UpdateAttacking();
        }

        protected virtual Vector3 GetChaseDestination()
        {
            return _player != null ? _player.position : transform.position;
        }

        protected virtual void UpdateChase()
        {
            if (Config == null || _player == null)
            {
                BeginIdle();
                return;
            }

            float dist = Vector3.Distance(transform.position, _player.position);

            if (dist > Config.ChaseRadius)
            {
                BeginIdle();
                return;
            }

            if (dist <= Config.AttackRange)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                _agent.nextPosition = transform.position;
                FaceTarget(_player, Config.RotationSpeed);
                TryUseSkill();
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(GetChaseDestination());

            Vector3 desiredVel = _agent.desiredVelocity;
            Vector3 move = desiredVel.normalized * Config.MoveSpeed;
            move.y += Physics.gravity.y * Time.deltaTime;
            _characterController.Move(move * Time.deltaTime);
            _agent.nextPosition = transform.position;

            Vector3 lookDir = new Vector3(desiredVel.x, 0, desiredVel.z);
            if (lookDir.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation, targetRot,
                    Config.RotationSpeed * Time.deltaTime);
            }

            if (!CanStillDetect())
            {
                _lostTargetTimer += Time.deltaTime;
                if (_lostTargetTimer >= Config.LoseTargetTime)
                    BeginIdle();
            }
            else _lostTargetTimer = 0f;
        }

        protected virtual void UpdateAttacking()
        {
            _agent.isStopped = true;
            _agent.nextPosition = transform.position;
            if (_player != null)
                FaceTarget(_player, Config != null ? Config.RotationSpeed : 360f);
        }

        protected virtual void TryUseSkill()
        {
            if (_player == null) return;
            float dist = Vector3.Distance(transform.position, _player.position);

            BaseEnemySkill best = null;
            foreach (var skill in _skills)
            {
                if (skill == null) continue;
                if (!skill.IsAvailable(_player, dist)) continue;
                if (best == null || skill.Priority > best.Priority) best = skill;
            }

            if (best != null)
            {
                _state = EnemyState.Attacking;
                best.Execute(_player);
            }
        }

        public virtual void NotifySkillComplete()
        {
            _state = _player != null ? EnemyState.Chasing : EnemyState.Idle;
        }

        protected virtual bool DetectByVision()
        {
            if (Config == null) return false;

            Collider[] hits = playerLayer == 0
                ? Physics.OverlapSphere(transform.position, Config.VisionRange)
                : Physics.OverlapSphere(transform.position, Config.VisionRange, playerLayer);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;
                Transform target = hit.transform;
                Vector3 dirToTarget = (target.position - transform.position).normalized;
                float angle = Vector3.Angle(transform.forward, dirToTarget);
                if (angle > Config.VisionAngle * 0.5f) continue;
                float dist = Vector3.Distance(transform.position, target.position);
                Ray ray = new Ray(transform.position + Vector3.up * 0.8f, dirToTarget);
                if (obstacleLayer != 0 && Physics.Raycast(ray, dist, obstacleLayer)) continue;
                _player = target;
                return true;
            }
            return false;
        }

        protected virtual bool DetectByHearing()
        {
            return false;
        }

        protected virtual bool CanStillDetect()
        {
            if (Config == null || _player == null) return false;
            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist <= Config.VisionRange)
            {
                Vector3 dir = (_player.position - transform.position).normalized;
                Ray ray = new Ray(transform.position + Vector3.up * 0.8f, dir);
                if (obstacleLayer == 0 || !Physics.Raycast(ray, dist, obstacleLayer))
                    return true;
            }
            if (dist <= Config.HearingRadius) return true;
            return false;
        }

        protected override void HandleDamagedBy(GameObject source)
        {
            if (source == null || _isDead) return;
            _player = source.transform;

            if (_state == EnemyState.Idle)
                BeginChase();
        }

        protected override void HandleDeath()
        {
            if (_isDead) return;
            _isDead = true;
            _state = EnemyState.Dead;

            StopAllCoroutines();
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.enabled = false;

            foreach (var skill in _skills)
                skill?.Cancel();

            OnDying();
        }

        protected abstract void OnDying();
    }
}