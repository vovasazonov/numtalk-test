using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Project.CoreDomain.Content
{
    public class ContentService : IContentService
    {
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new();
        private readonly Dictionary<string, int> _counter = new();

        public IEnumerable<string> HandleContents => _handles.Keys;

        public float PercentComplete()
        {
            return PercentComplete(_handles.Keys);
        }

        public float PercentComplete(string id)
        {
            float percent;

            if (_handles.TryGetValue(id, out var handle))
            {
                if (!handle.IsDone && handle.PercentComplete < 1)
                {
                    percent = handle.PercentComplete;
                }
                else if (handle.IsDone)
                {
                    percent = 1;
                }
                else
                {
                    percent = 0;
                }
            }
            else
            {
                percent = 0;
            }

            return percent;
        }
        
        public float PercentComplete(IEnumerable<string> ids)
        {
            int count = 0;
            
            float sum = ids.Select(i =>
            {
                ++count;
                return PercentComplete(i);
            }).Sum();
            
            return count != 0 ? sum / count : 0;
        }

        public bool IsLoaded(string id)
        {
            return _handles.ContainsKey(id) && _handles[id].IsDone && _handles[id].Status == AsyncOperationStatus.Succeeded;
        }

        public bool IsLoading(string id)
        {
            return _handles.ContainsKey(id) && _handles[id].Status != AsyncOperationStatus.Failed && !_handles[id].IsDone;
        }

        public async UniTask<IContentKeeper<T>> LoadAsync<T>(string id) where T : class
        {
            AsyncOperationHandle handle;

            if (_handles.ContainsKey(id))
            {
                handle = _handles[id];
            }
            else
            {
                handle = Addressables.LoadAssetAsync<T>(id);
                _handles.Add(id, handle);
            }

            if (!_counter.ContainsKey(id))
            {
                _counter.Add(id, 0);
            }

            _counter[id]++;
            
            await handle.Task;
            
            var result = handle.Result as T;

            if (result == null)
            {
                throw new NullReferenceException();
            }
            
            return new ContentKeeper<T>(this, id, result);
        }

        internal void Unload(string id)
        {
            if (_counter[id] == 0)
            {
                throw new ArgumentOutOfRangeException();
            }
            
            _counter[id]--;

            if (_counter[id] == 0 && _handles.ContainsKey(id))
            {
                var handle = _handles[id];
                _handles.Remove(id);
                Addressables.Release(handle);
            }
        }
    }
}