using Cysharp.Threading.Tasks;
using Project.CoreDomain.Lifecycle;

namespace Project.CoreDomain.Screen
{
    public interface IScreen : ITaskAsyncDisposable, ITaskAsyncInitializable
    {
        bool IsDisposeOnSwitch { get; }

        UniTask ShowAsync();
        UniTask HideAsync();
    }
}