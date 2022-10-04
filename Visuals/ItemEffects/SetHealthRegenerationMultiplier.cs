namespace VisualsModifier.Effects
{
    public class SetHealthRegenerationMultiplier : ISetEffect
    {
        public float Percentage { get; private set; }

        public SetHealthRegenerationMultiplier(float percentage)
        {
            this.Percentage = percentage;
        }

        public void Apply(ref SE_Stats stats)
        {
            stats.m_healthRegenMultiplier = (this.Percentage / 100) + 1.0f;
        }
    }
}
