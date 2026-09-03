using Cysharp.Threading.Tasks;

namespace Project.CoreDomain.Data
{
    public interface IDataStorageService
    {
        UniTask LoadAsync();
        UniTask SaveAsync();
        bool Contains(string key);
        T Get<T>(string key) where T : class;
        T Create<T>(string key) where T : class, new();
        void Flush();
    }
}
