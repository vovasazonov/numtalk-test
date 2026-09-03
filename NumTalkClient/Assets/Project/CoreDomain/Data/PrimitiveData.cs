using Newtonsoft.Json;

namespace Project.CoreDomain.Data
{
    public class PrimitiveData<T>
    {
        [JsonProperty("value")] public T Value;
    }
}