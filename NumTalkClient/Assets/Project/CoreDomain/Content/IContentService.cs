using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Project.CoreDomain.Content
{
    public interface IContentService
    {
        IEnumerable<string> HandleContents { get; }

        UniTask<IContentKeeper<T>> LoadAsync<T>(string id) where T : class;

        float PercentComplete();
        float PercentComplete(string id);
        float PercentComplete(IEnumerable<string> ids);

        bool IsLoaded(string id);
        bool IsLoading(string id);
    }
}