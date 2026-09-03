using Cysharp.Threading.Tasks;
using Project.CoreDomain.Content;

namespace Project.CoreDomain.View
{
    public interface IViewService
    {
        UniTask<IContentKeeper<T>> CreateAsync<T>(string assetId);
    }
}