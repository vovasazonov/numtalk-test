using System.IO;
using Cysharp.Threading.Tasks;
using Project.CoreDomain.Serialization;

namespace Project.CoreDomain.FileLoader.Loaders
{
    internal class JsonFileLoader : IFileLoader
    {
        private readonly ISerializerService _serializerService;

        public JsonFileLoader(ISerializerService serializerService)
        {
            _serializerService = serializerService;
        }

        public async UniTask<T> LoadAsync<T>(string path)
        {
            if (File.Exists(path))
            {
                string json = await File.ReadAllTextAsync(path).AsUniTask();
                return _serializerService.DeserializeJson<T>(json);
            }

            return default;
        }

        public async UniTask SaveAsync<T>(T obj, string path)
        {
            string json = _serializerService.SerializeToJson(obj);
            await File.WriteAllTextAsync(path, json).AsUniTask();
        }
    }
}
