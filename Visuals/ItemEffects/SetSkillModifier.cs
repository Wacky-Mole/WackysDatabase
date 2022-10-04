namespace VisualsModifier.Effects
{
    public class SetSkillModifier : ISetEffect
    {
        public Skills.SkillType SkillType { get; private set; }
        public float Value { get; private set; }

        public SetSkillModifier(Skills.SkillType skillType, float value)
        {
            this.SkillType = skillType;
            this.Value = value;
        }

        public void Apply(ref SE_Stats stats)
        {
            stats.m_skillLevel = this.SkillType;
            stats.m_skillLevelModifier = this.Value;
        }
    }
}
