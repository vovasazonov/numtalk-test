using Cysharp.Threading.Tasks;

namespace Project.CoreDomain.Lifecycle
{
    public interface ITaskAsyncDisposable
    {
        UniTask DisposeAsync();
    }
}