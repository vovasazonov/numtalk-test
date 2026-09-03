namespace Project.CoreDomain.Serialization
{
    public interface ISerializerService
    {
        T DeserializeJson<T>(string json);
        T DeserializeJson<T>(object json);

        string SerializeToJson<T>(T obj);
    }
}