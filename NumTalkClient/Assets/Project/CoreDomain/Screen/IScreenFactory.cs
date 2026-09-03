namespace Project.CoreDomain.Screen
{
    public interface IScreenFactory
    {
        string Id { get; }
        
        IScreen Create();
    }
}