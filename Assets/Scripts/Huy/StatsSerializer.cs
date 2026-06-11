using SimpleSurvival.Stats;

namespace SimpleSurvival.SaveLoad
{
    public sealed class StatsSerializer
    {
        public PlayerStatsData Capture(PlayerStats stats)
        {
            return new PlayerStatsData
            {
                hp = stats.HP,
                hunger = stats.Hunger,
                thirst = stats.Thirst
            };
        }

        public void Restore(PlayerStatsData data, PlayerStats stats)
        {
            if (data == null || stats == null)
                return;

            stats.RestoreHP(data.hp);
            stats.RestoreSurvival(data.hunger, data.thirst);
        }
    }
}