namespace VisualsModifier.Effects
{
    public class SetDamageModifier : ISetEffect
    {
        public float Percentage { get; private set; }

        public SetDamageModifier(float percentage)
        {
            this.Percentage = percentage;
        }

        public void Apply(ref SE_Stats stats)
        {
            stats.m_damageModifier = (this.Percentage / 100) + 1.0f;
        }
    }
}
