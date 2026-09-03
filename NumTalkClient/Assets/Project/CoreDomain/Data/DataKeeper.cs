using Cysharp.Threading.Tasks;
using Project.CoreDomain.Lifecycle;
using Project.CoreDomain.Management;

namespace Project.CoreDomain.Data
{
    public class DataKeeper<T> : IKeeper<T>, ITaskAsyncInitializable where T : class, new()
    {
        private readonly IDataStorageService _dataStorageService;
        private readonly string _key;

        public T Value { get; private set; }

        protected DataKeeper(IDataStorageService dataStorageService, string key)
        {
            _dataStorageService = dataStorageService;
            _key = key;
        }

        public UniTask InitializeAsync()
        {
            Value = _dataStorageService.Contains(_key) ? _dataStorageService.Get<T>(_key) : _dataStorageService.Create<T>(_key);
            return UniTask.CompletedTask;
        }
    }
}