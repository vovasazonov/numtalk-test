using Project.CoreDomain.FileLoader.Loaders;
using Project.CoreDomain.Serialization;

namespace Project.CoreDomain.FileLoader
{
    public class FileLoaderService : IFileLoaderService
    {
        public IFileLoader Binary { get; }
        public IFileLoader Json { get; }
        
        public FileLoaderService(ISerializerService serializerService)
        {
            Binary = new BinaryFileLoader();
            Json = new JsonFileLoader(serializerService);
        }
    }
}