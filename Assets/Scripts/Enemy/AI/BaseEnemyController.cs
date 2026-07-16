using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public abstract class BaseEnemyController : BaseAIController
    {
        protected enum EnemyState { Idle, Detected, Chasing, Attacking, Retreating, Hidden, Dead }
        protected EnemyState _state = EnemyState.Idle;

        [Header("Skills (compose qua Inspector)")]
        [SerializeField] protected List<BaseEnemySkill> _skills = new List<BaseEnemySkill>();

        [Header("Retreat & Hide (dùng khi enemy cần lui về spawn point và biến mất, vd Witch triệu hồi)")]
        [Tooltip("Khoảng cách tới spawn point được coi là đã 'tới nơi' để bắt đầu ẩn")]
        [SerializeField] protected float retreatArrivalThreshold = 0.3f;

        protected float _lostTargetTimer;

        /// <summary>
        /// True khi enemy được EscortEnemyDirector gán để chỉ bám/tấn công 1 target cố định
        /// (vd Emily) trong lúc hộ tống. Khi bật, enemy bỏ qua hoàn toàn detection/damage từ Player.
        /// Enemy spawn sẵn trên map bình thường KHÔNG bao giờ bật cờ này nên hành vi không đổi.
        /// </summary>
        protected bool _escortMode;

        protected EnemyStatsConfig Config => _stats != null ? _stats.EnemyConfig : null;

        public float LastDamageDealtTime { get; private set; } = -999f;

        public void NotifyDamageDealt()
        {
            LastDamageDealtTime = Time.time;
        }

        /// <summary>
        /// Gọi bởi EscortEnemyDirector ngay sau khi spawn enemy ẩn cho nhiệm vụ hộ tống.
        /// Enemy sẽ khóa cứng mục tiêu vào target này, không detect/react với Player nữa.
        /// </summary>
        public void SetEscortTarget(Transform target)
        {
            if (_isDead || target == null) return;
            _escortMode = true;
            _player = target;
            BeginChase();
        }

        /// <summary>
        /// Gỡ escort mode, trả enemy về hành vi bình thường: đứng Idle rồi tự phát hiện
        /// và tấn công Player thật qua DetectionRoutine như mọi enemy khác trên map.
        /// Gọi khi nhiệm vụ hộ tống đã kết thúc (thành công hoặc thất bại) nhưng enemy vẫn còn sống.
        /// </summary>
        public void ReleaseEscortTarget()
        {
            if (_isDead || !_escortMode) return;

            _escortMode = false;
            BeginIdle();

            // DetectionRoutine đã yield break khi còn ở escort mode nên phải khởi động lại.
            StopAllCoroutines();
            StartCoroutine(DetectionRoutine());
        }

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
            LastDamageDealtTime = -999f;
            _escortMode = false;

            OnEnemyInitialized();

            StopAllCoroutines();
            StartCoroutine(DetectionRoutine());
        }

        protected abstract void OnEnemyInitialized();

        protected virtual IEnumerator DetectionRoutine()
        {
            while (!_isDead)
            {
                if (_escortMode) yield break; // enemy hộ tống không tự phát hiện Player

                yield return new WaitForSeconds(0.2f);
                if (_state == EnemyState.Dead) yield break;
                if (_playerDead) continue;
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
            if (_isDead || _playerDead) return;

            // Chỉ chạy khi enemy đang ở escort mode (_escortMode luôn false với enemy bình thường
            // nên đoạn này không ảnh hưởng gì tới hành vi cũ - kể cả Zombie Acid đánh Player).
            if (_escortMode) UpdateEscortWatchdog();

            if (_state == EnemyState.Chasing) UpdateChase();
            else if (_state == EnemyState.Attacking) UpdateAttacking();
            else if (_state == EnemyState.Retreating) UpdateRetreat();
        }

        /// <summary>
        /// Chỉ áp dụng cho enemy đang bám mục tiêu hộ tống (Emily). Đảm bảo enemy không bị kẹt
        /// vĩnh viễn ở trạng thái Attacking khi target đã đi xa và không skill nào đang thực thi.
        /// Không hủy skill đang chạy (kể cả skill tầm xa như acid) - chỉ tự thoát khi thực sự
        /// không có gì đang diễn ra.
        /// </summary>
        private void UpdateEscortWatchdog()
        {
            if (_state != EnemyState.Attacking || _player == null || Config == null) return;

            foreach (var skill in _skills)
            {
                if (skill != null && skill.IsExecuting) return; // đang có skill chạy -> để yên, không can thiệp
            }

            float dist = Vector3.Distance(transform.position, _player.position);
            if (dist > Config.AttackRange * 1.5f)
                _state = EnemyState.Chasing;
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

            // Enemy hộ tống bám mục tiêu không giới hạn ChaseRadius (không "mất dấu" Emily).
            if (!_escortMode && dist > Config.ChaseRadius)
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

            MoveAlongAgentPath(Config.MoveSpeed, Config.RotationSpeed);

            if (!_escortMode)
            {
                if (!CanStillDetect())
                {
                    _lostTargetTimer += Time.deltaTime;
                    if (_lostTargetTimer >= Config.LoseTargetTime)
                        BeginIdle();
                }
                else _lostTargetTimer = 0f;
            }
        }

        protected virtual void UpdateAttacking()
        {
            _agent.isStopped = true;
            _agent.nextPosition = transform.position;
            if (_player != null)
                FaceTarget(_player, Config != null ? Config.RotationSpeed : 360f);
        }

        /// <summary>
        /// Di chuyển enemy quay về spawn point (dùng khi cần "lui vào ẩn nấp",
        /// vd Witch sau khi triệu hồi minion). Khi tới nơi sẽ tự chuyển sang Hidden.
        /// </summary>
        protected virtual void UpdateRetreat()
        {
            Vector3 destination = _spawnPoint != null ? _spawnPoint.Position : transform.position;
            float dist = Vector3.Distance(transform.position, destination);

            if (dist <= retreatArrivalThreshold)
            {
                _agent.isStopped = true;
                _agent.ResetPath();
                EnterHidden();
                return;
            }

            _agent.isStopped = false;
            _agent.SetDestination(destination);
            MoveAlongAgentPath(
                Config != null ? Config.MoveSpeed : 3.5f,
                Config != null ? Config.RotationSpeed : 360f);
        }

        /// <summary>
        /// Bắt đầu lui về spawn point. Gọi từ skill (vd sau khi triệu hồi xong minion).
        /// Trong lúc Retreating/Hidden, DetectionRoutine tự động không chạy vì state != Idle.
        /// </summary>
        public virtual void BeginRetreat()
        {
            if (_isDead) return;
            _state = EnemyState.Retreating;
            _agent.isStopped = false;
            _stats?.SetInvulnerable(true);
        }

        /// <summary>
        /// Enemy đã về tới spawn point — ẩn model, chờ tín hiệu ReappearAndResume().
        /// </summary>
        protected virtual void EnterHidden()
        {
            _state = EnemyState.Hidden;
            SetModelVisible(false);
        }

        /// <summary>
        /// Hiện lại và tiếp tục chiến đấu. Gọi từ skill khi điều kiện chờ đã xong
        /// (vd tất cả minion triệu hồi đã bị tiêu diệt).
        /// </summary>
        public virtual void ReappearAndResume()
        {
            if (_isDead) return;
            SetModelVisible(true);
            _stats?.SetInvulnerable(false);

            if (_player != null && !_playerDead)
                BeginChase();
            else
                BeginIdle();
        }

        /// <summary>
        /// Bật/tắt renderer + collider chính (không đụng ragdoll collider vì chúng
        /// nằm trên object con — theo đúng convention OnDying() đang dùng).
        /// </summary>
        protected void SetModelVisible(bool visible)
        {
            foreach (var r in GetComponentsInChildren<Renderer>(true))
                if (r != null) r.enabled = visible;

            foreach (var col in GetComponents<Collider>())
            {
                if (col is CharacterController) continue;
                col.enabled = visible;
            }
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
            if (source == null || _isDead || _playerDead) return;
            if (_escortMode) return; // enemy hộ tống không đổi mục tiêu dù bị player đánh trả
            if (!source.CompareTag("Player")) return; // bỏ qua damage từ Dog / nguồn không phải Player thật

            _player = source.transform;

            if (_state == EnemyState.Idle)
                BeginChase();
        }

        protected override void HandleDeath(GameObject source)
        {
            if (_isDead) return;
            _isDead = true;
            _state = EnemyState.Dead;

            // Đảm bảo xác hiện ra đầy đủ dù enemy đang Hidden lúc chết
            SetModelVisible(true);

            StopAllCoroutines();
            _agent.isStopped = true;
            _agent.ResetPath();
            _agent.enabled = false;

            foreach (var skill in _skills)
                skill?.Cancel();

            OnDying();
        }

        protected abstract void OnDying();

        protected override void HandlePlayerDeath(GameObject source)
        {
            if (_isDead) return; // quái này đã chết thật thì thôi
            if (_escortMode) return; // enemy hộ tống không quan tâm Player thật sống/chết, không dừng theo

            base.HandlePlayerDeath(source); // set đúng _playerDead, không đụng _isDead

            _state = EnemyState.Idle;
            _player = null;
            _lostTargetTimer = 0f;

            _agent.isStopped = true;
            _agent.ResetPath();

            foreach (var skill in _skills)
                skill?.Cancel();

            StopAllActions();
        }
        protected virtual void StopAllActions() { }
    }
}