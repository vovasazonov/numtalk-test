using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.FileLoader;
using Project.CoreDomain.Serialization;
using UnityEngine;

namespace Project.CoreDomain.Data
{
    public class DataStorageService : IDataStorageService
    {
        private readonly string _path = $"{Application.persistentDataPath}/.data";
        private readonly IFileLoader _fileLoader;
        private readonly ISerializerService _serializerService;
        private readonly Dictionary<string, object> _deserializedData = new();
        private readonly Dictionary<string, object> _serializedData = new();
        private readonly SemaphoreSlim _ioLock = new(1, 1);

        public DataStorageService(
            ISerializerService serializerService,
            IFileLoaderService fileLoaderService
        )
        {
            _serializerService = serializerService;
            _fileLoader = fileLoaderService.Binary;
        }

        public async UniTask LoadAsync()
        {
            await _ioLock.WaitAsync().AsUniTask();
            try
            {
                object data = await _fileLoader.LoadAsync<object>(_path);
                _serializedData.Clear();
                if (data != null)
                {
                    var deserializedData = _serializerService.DeserializeJson<Dictionary<string, object>>(data);
                    if (deserializedData != null)
                    {
                        foreach (var key in deserializedData.Keys)
                        {
                            _serializedData.Add(key, deserializedData[key]);
                        }
                    }
                }
            }
            finally
            {
                _ioLock.Release();
            }
        }

        public async UniTask SaveAsync()
        {
            await _ioLock.WaitAsync().AsUniTask();
            try
            {
                foreach (var key in _deserializedData.Keys)
                {
                    Serialize(key);
                }

                object serialized = _serializerService.SerializeToJson(_serializedData);
                await _fileLoader.SaveAsync(serialized, _path);
            }
            finally
            {
                _ioLock.Release();
            }
        }

        public bool Contains(string key)
        {
            return _serializedData.ContainsKey(key) || _deserializedData.ContainsKey(key);
        }

        public T Get<T>(string key) where T : class
        {
            if (!_deserializedData.ContainsKey(key))
            {
                Deserialize<T>(key);
            }

            return _deserializedData[key] as T;
        }

        public T Create<T>(string key) where T : class, new()
        {
            T data = new T();
            _deserializedData.Add(key, data);
            return data;
        }

        public void Flush()
        {
            _serializedData.Clear();
            _deserializedData.Clear();
        }

        private void Serialize(string key)
        {
            var data = _deserializedData[key];
            _serializedData[key] = _serializerService.SerializeToJson(data);
        }

        private void Deserialize<T>(string key)
        {
            var data = _serializedData[key];
            _deserializedData[key] = _serializerService.DeserializeJson<T>(data);
        }
    }
}