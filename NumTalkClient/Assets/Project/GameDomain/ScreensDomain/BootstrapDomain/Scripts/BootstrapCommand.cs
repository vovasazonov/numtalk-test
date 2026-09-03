using Cysharp.Threading.Tasks;
using Project.CoreDomain.Data;

namespace Project.GameDomain.Features.Bootstrap
{
    public class BootstrapCommand
    {
        private readonly IDataStorageService _dataStorageService;

        public BootstrapCommand(IDataStorageService dataStorageService)
        {
            _dataStorageService = dataStorageService;
        }

        public async UniTask ExecuteAsync()
        {
            await _dataStorageService.LoadAsync();
        }
    }
}