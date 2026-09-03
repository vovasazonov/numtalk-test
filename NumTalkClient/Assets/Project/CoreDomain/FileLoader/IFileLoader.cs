using Cysharp.Threading.Tasks;

namespace Project.CoreDomain.FileLoader
{
    public interface IFileLoader
    {
        UniTask<T> LoadAsync<T>(string path);
        UniTask SaveAsync<T>(T obj, string path);
    }
}
