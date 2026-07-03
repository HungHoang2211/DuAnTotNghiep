using UnityEngine;
using SimpleSurvival.Input;
using SimpleSurvival.Stats;

namespace SimpleSurvival.AI
{
    public sealed class ZombieFatController : BaseEnemyController
    {
        [Header("Refs")]
        [SerializeField] private ZombieFatAnimator _fatAnimator;
        [SerializeField] private EnemyCorpseHandler _corpseHandler;

        [Header("Stuck Detection")]
        [SerializeField] private float stuckCheckInterval = 0.8f;
        [SerializeField] private float stuckDistanceThreshold = 0.15f;
        [SerializeField] private float unstuckRadius = 4f;
        [SerializeField] private float unstuckDuration = 1.2f;

        [Header("Hearing")]
        [SerializeField] private float footstepMinSpeed = 0.1f;

        private Vector3 _lastTrackedPosition;
        private float _nextStuckCheckTime;
        private Vector3 _unstuckPoint;
        private float _unstuckUntil;

        protected override void OnEnemyInitialized()
        {
            if (_fatAnimator != null) _fatAnimator.ResetForSpawn();
            _lastTrackedPosition = transform.position;
            _nextStuckCheckTime = 0f;
            _unstuckUntil = 0f;
        }

        protected override void BeginChase()
        {
            base.BeginChase();

            // Đảm bảo Claw luôn là đòn đầu tiên: ép JumpAttack vào cooldown ngay khi
            // bắt đầu chase (mỗi lần bắt đầu 1 lượt combat mới). Jump chỉ thực sự
            // available sau đúng Cooldown giây (hiện = 10s) kể từ lúc này.
            foreach (var skill in _skills)
            {
                if (skill is JumpAttackSkill jumpSkill)
                    jumpSkill.PutOnCooldown();
            }
        }

        protected override bool DetectByHearing()
        {
            if (Config == null) return false;

            Collider[] hits = playerLayer == 0
                ? Physics.OverlapSphere(transform.position, Config.HearingRadius)
                : Physics.OverlapSphere(transform.position, Config.HearingRadius, playerLayer);

            foreach (var hit in hits)
            {
                if (!hit.CompareTag("Player")) continue;

                Transform target = hit.transform;

                var inputReader = target.GetComponentInParent<PlayerInputReader>();
                if (inputReader == null) inputReader = target.root.GetComponentInChildren<PlayerInputReader>();
                if (inputReader != null && inputReader.IsSneakHeld) continue; // sneak -> luôn không nghe thấy, bất kể tốc độ

                float playerSpeed = 0f;
                var cc = target.GetComponentInParent<CharacterController>();
                if (cc != null) playerSpeed = cc.velocity.magnitude;

                if (playerSpeed < footstepMinSpeed) continue; // đứng yên -> không nghe thấy

                _player = target;
                return true;
            }
            return false;
        }

        protected override Vector3 GetChaseDestination()
        {
            if (Time.time < _unstuckUntil)
                return _unstuckPoint;
            return base.GetChaseDestination();
        }

        protected override void UpdateChase()
        {
            if (_fatAnimator != null && _fatAnimator.IsInAttackState)
            {
                _agent.isStopped = true;
                _agent.nextPosition = transform.position;
                if (_player != null) FaceTarget(_player, Config?.RotationSpeed ?? 360f);
                return;
            }

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

            // Không dùng thẳng Config.AttackRange (tầm cận chiến) làm mốc dừng, vì AcidAttack
            // là skill tầm xa, cần được xét đến ngay cả khi player còn ở xa ngoài AttackRange.
            // engageRange = max(AttackRange cận chiến, MaxRange xa nhất trong các skill đang có).
            float engageRange = GetMaxEngageRange();

            if (dist <= engageRange)
            {
                // Luôn thử dùng skill khi đã vào tầm xa nhất. Chỉ đứng yên nếu skill THỰC SỰ
                // được thi triển (state chuyển sang Attacking). Nếu không skill nào sẵn sàng
                // ngay lúc này (vd: Acid còn đang chờ đủ 3s trong tầm hoặc đang cooldown, còn
                // Claw/Jump thì player chưa đủ gần), phải tiếp tục tiến lại gần chứ không được
                // đứng khựng lại ở khoảng cách xa chờ mãi.
                FaceTarget(_player, Config.RotationSpeed);
                TryUseSkill();

                if (_state == EnemyState.Attacking)
                {
                    _agent.isStopped = true;
                    _agent.ResetPath();
                    _agent.nextPosition = transform.position;
                    if (_fatAnimator != null) _fatAnimator.SetMoveSpeed(0f);
                    return;
                }
            }

            _agent.isStopped = false;
            _agent.SetDestination(GetChaseDestination());
            MoveAlongAgentPath(Config.MoveSpeed, Config.RotationSpeed);

            if (_fatAnimator != null)
            {
                float speed = _characterController != null ? _characterController.velocity.magnitude : 0f;
                _fatAnimator.SetMoveSpeed(speed);
            }

            if (!CanStillDetect())
            {
                _lostTargetTimer += Time.deltaTime;
                if (_lostTargetTimer >= Config.LoseTargetTime)
                    BeginIdle();
            }
            else _lostTargetTimer = 0f;

            CheckStuck();
        }

        private float GetMaxEngageRange()
        {
            float max = Config.AttackRange;
            foreach (var skill in _skills)
            {
                if (skill != null && skill.MaxRange > max)
                    max = skill.MaxRange;
            }
            return max;
        }

        protected override void UpdateAttacking()
        {
            base.UpdateAttacking();

            // Khi chuyển sang Attacking, UpdateChase() không còn được gọi nên MoveSpeed
            // có thể bị "đóng băng" ở giá trị cuối cùng khác 0, gây giật chân khi animation
            // tấn công (Claw/Jump) đang chạy. Ép về 0 mỗi frame trong lúc Attacking để
            // tránh Blend Tree đi/đứng tiếp tục blend chồng lên animation tấn công.
            if (_fatAnimator != null)
                _fatAnimator.SetMoveSpeed(0f);
        }

        private void CheckStuck()
        {
            if (_state != EnemyState.Chasing) return;
            if (Time.time < _unstuckUntil) return;
            if (Time.time < _nextStuckCheckTime) return;

            float moved = Vector3.Distance(transform.position, _lastTrackedPosition);
            Vector3 desiredVel = _agent.desiredVelocity;

            if (moved < stuckDistanceThreshold && desiredVel.sqrMagnitude > 0.01f)
            {
                _unstuckPoint = GetRandomNavMeshPoint(transform.position, unstuckRadius);
                _unstuckUntil = Time.time + unstuckDuration;
            }

            _lastTrackedPosition = transform.position;
            _nextStuckCheckTime = Time.time + stuckCheckInterval;
        }

        public override void NotifySkillComplete()
        {
            base.NotifySkillComplete();
            if (_fatAnimator != null && _player != null)
                _fatAnimator.SetMoveSpeed(Config != null ? Config.MoveSpeed : 1f);
        }

        protected override void BeginIdle()
        {
            base.BeginIdle();
            if (_fatAnimator != null) _fatAnimator.SetIdle();
        }

        protected override void OnDying()
        {
            if (_characterController != null)
                _characterController.enabled = false;

            var mainCol = GetComponent<Collider>();
            if (mainCol != null) mainCol.enabled = false;

            if (_fatAnimator != null)
            {
                _fatAnimator.SetIdle();
                _fatAnimator.SetRagdollLayer(LayerMask.NameToLayer("Corpse"));
                _fatAnimator.TriggerDeath();
            }

            if (_corpseHandler != null)
                _corpseHandler.SpawnCorpseLoot(Config?.CorpseLootTable);

            float despawnDelay = Config != null ? Config.DespawnDelay : 120f;
            Destroy(gameObject, despawnDelay);
            if (_spawnPoint != null)
                _spawnPoint.NotifyDespawned(despawnDelay);
        }
    }
}