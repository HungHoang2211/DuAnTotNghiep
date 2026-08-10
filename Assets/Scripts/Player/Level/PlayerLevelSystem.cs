using System;
using UnityEngine;
using SimpleSurvival.Quests;
using SimpleSurvival.Stats;
using SimpleSurvival.SaveLoad;

namespace SimpleSurvival.Progression
{
    public sealed class PlayerLevelSystem : MonoBehaviour
    {
        public static PlayerLevelSystem Instance { get; private set; }

        [SerializeField] private PlayerLevelConfig config;
        [SerializeField] private PlayerStats playerStats;

        private int _level = 1;
        private int _currentExp;

        public int CurrentLevel => _level;
        public int CurrentExp => _currentExp;
        public int ExpToNextLevel => config != null ? config.GetExpRequired(_level) : 0;
        public bool IsMaxLevel => config != null && _level >= config.MaxLevel;

        public event Action<int, int, int> OnExpChanged;
        public event Action<int> OnLevelUp;
        public event Action<int> OnExpGained;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            QuestManager manager = QuestManager.Instance;
            if (manager != null)
                manager.OnQuestCompleted += HandleQuestCompleted;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;

            QuestManager manager = QuestManager.Instance;
            if (manager != null)
                manager.OnQuestCompleted -= HandleQuestCompleted;
        }

        public bool HasReachedLevel(int level) => _level >= level;

        public void AddExperience(int amount)
        {
            if (amount <= 0 || config == null || IsMaxLevel) return;

            OnExpGained?.Invoke(amount);

            _currentExp += amount;

            while (!IsMaxLevel && _currentExp >= config.GetExpRequired(_level))
            {
                _currentExp -= config.GetExpRequired(_level);
                _level++;
                ApplyLevelUpHPBonus(_level);
                OnLevelUp?.Invoke(_level);
            }

            if (IsMaxLevel) _currentExp = 0;

            OnExpChanged?.Invoke(_currentExp, ExpToNextLevel, _level);
        }

        private void ApplyLevelUpHPBonus(int newLevel)
        {
            if (playerStats == null) return;
            playerStats.AddMaxHPBonus(GetHPBonusForLevel(newLevel));
        }

        private void HandleQuestCompleted(QuestData quest)
        {
            AddExperience(quest.ExpReward);
        }
        public LevelData Capture()
        {
            return new LevelData
            {
                level = _level,
                currentExp = _currentExp
            };
        }
        private static float GetHPBonusForLevel(int level)
        {
            return level <= 10 ? 1f : 2f;
        }
        private static float GetTotalHPBonus(int level)
        {
            float total = 0f;
            for (int lv = 2; lv <= level; lv++)
                total += GetHPBonusForLevel(lv);
            return total;
        }

        public void Restore(LevelData data)
        {
            _level = 1;
            _currentExp = 0;

            if (data != null && config != null)
            {
                _level = Mathf.Clamp(data.level, 1, config.MaxLevel);
                _currentExp = Mathf.Max(0, data.currentExp);
            }

            if (playerStats != null)
                playerStats.SetMaxHPBonus(GetTotalHPBonus(_level));

            OnExpChanged?.Invoke(_currentExp, ExpToNextLevel, _level);
        }

    }
}