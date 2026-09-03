using Cysharp.Threading.Tasks;

namespace Project.CoreDomain.Lifecycle
{
    public interface ITaskAsyncInitializable
    {
        UniTask InitializeAsync();
    }
}