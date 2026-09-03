namespace Project.CoreDomain.Management
{
    public interface IKeeper<out T>
    {
        T Value { get; }
    }
}