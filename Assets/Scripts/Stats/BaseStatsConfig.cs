using UnityEngine;

namespace SimpleSurvival.Stats
{
    public abstract class BaseStatsConfig : ScriptableObject
    {
        [Header("HP")]
        [SerializeField] private float maxHP = 100f;
        [SerializeField] private float startHP = 100f;

        [Header("Combat")]
        [SerializeField] private float baseDamage = 10f;
        [SerializeField] private float baseAttackSpeed = 1f;
        [SerializeField] private float armor = 0f;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 5f;

        public float MaxHP => maxHP;
        public float StartHP => startHP;
        public float BaseDamage => baseDamage;
        public float BaseAttackSpeed => baseAttackSpeed;
        public float Armor => armor;
        public float MoveSpeed => moveSpeed;
    }
}