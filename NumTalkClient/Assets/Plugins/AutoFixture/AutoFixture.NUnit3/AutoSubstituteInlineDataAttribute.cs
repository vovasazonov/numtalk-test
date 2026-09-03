namespace AutoFixture.NUnit3
{
    public class AutoSubstituteInlineDataAttribute : InlineAutoDataAttribute
    {
        public AutoSubstituteInlineDataAttribute(params object[] objects) : base(() => new Fixture().Customize(new AutoNSubstitute.AutoNSubstituteCustomization()), objects)
        {
        }
    }
}