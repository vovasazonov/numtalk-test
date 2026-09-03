namespace Project.CoreDomain.FileLoader
{
    public interface IFileLoaderService
    {
        IFileLoader Binary { get; }
        IFileLoader Json { get; }
    }
}