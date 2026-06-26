using UnityEngine;
using SimpleSurvival.Stats;

namespace SimpleSurvival.UI
{
    [RequireComponent(typeof(StatLabel))]
    public sealed class CombatStatLabel : MonoBehaviour
    {
        public enum StatType
        {
            Damage,
            Defense,
            Speed,
            AttackSpeed
        }

        [SerializeField] private StatType statType;
        [SerializeField] private PlayerStats playerStats;

        private StatLabel _label;
        private float _lastValue;

        private void Awake()
        {
            _label = GetComponent<StatLabel>();
            if (playerStats == null)
                playerStats = FindAnyObjectByType<PlayerStats>();
        }

        private void OnEnable()
        {
            if (playerStats != null)
                playerStats.OnCombatStatsChanged += HandleStatsChanged;

            Refresh(animated: false);
        }

        private void OnDisable()
        {
            if (playerStats != null)
                playerStats.OnCombatStatsChanged -= HandleStatsChanged;
        }

        private void HandleStatsChanged()
        {
            Refresh(animated: true);
        }

        private void Refresh(bool animated)
        {
            float raw = ReadValue();

            if (statType == StatType.AttackSpeed)
            {
                float value = Mathf.Round(raw * 10f) / 10f;
                if (animated && !Mathf.Approximately(value, _lastValue))
                {
                    bool isIncrease = value > _lastValue;
                    _label.SetAmountAnimated(value, isIncrease);
                }
                else
                {
                    _label.SetAmount(value);
                }
                _lastValue = value;
            }
            else
            {
                int value = ConvertToInt(raw);
                int last = Mathf.RoundToInt(_lastValue);
                if (animated && value != last)
                {
                    bool isIncrease = value > last;
                    _label.SetAmountAnimated(value, isIncrease);
                }
                else
                {
                    _label.SetAmount(value);
                }
                _lastValue = value;
            }
        }

        private int ConvertToInt(float raw)
        {
            if (statType == StatType.Speed)
                return Mathf.RoundToInt(raw * 10f);
            return Mathf.RoundToInt(raw);
        }

        private float ReadValue()
        {
            if (playerStats == null) return 0f;

            return statType switch
            {
                StatType.Damage => playerStats.TotalDamage,
                StatType.Defense => playerStats.TotalDefense,
                StatType.Speed => playerStats.TotalMoveSpeed,
                StatType.AttackSpeed => playerStats.TotalAttackSpeed,
                _ => 0f
            };
        }
    }
}