using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Cysharp.Threading.Tasks;

namespace Project.CoreDomain.FileLoader.Loaders
{
    internal class BinaryFileLoader : IFileLoader
    {
        public async UniTask<T> LoadAsync<T>(string path)
        {
            if (File.Exists(path))
            {
                byte[] bytes = await File.ReadAllBytesAsync(path).AsUniTask();
                BinaryFormatter formatter = new BinaryFormatter();
                using MemoryStream stream = new MemoryStream(bytes);

                return (T)formatter.Deserialize(stream);
            }

            return default;
        }

        public async UniTask SaveAsync<T>(T obj, string path)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using MemoryStream stream = new MemoryStream();

            formatter.Serialize(stream, obj);
            await File.WriteAllBytesAsync(path, stream.ToArray()).AsUniTask();
        }
    }
}
