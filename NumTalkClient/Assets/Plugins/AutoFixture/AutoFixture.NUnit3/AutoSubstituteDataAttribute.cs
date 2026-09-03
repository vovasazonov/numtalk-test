namespace AutoFixture.NUnit3
{
    public class AutoSubstituteDataAttribute : AutoDataAttribute
    {
        public AutoSubstituteDataAttribute() : base(() => new Fixture().Customize(new AutoNSubstitute.AutoNSubstituteCustomization()))
        {
        }
    }
}