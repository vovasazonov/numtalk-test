using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Project.CoreDomain.Serialization
{
    public class SerializerService : ISerializerService
    {
        public T DeserializeJson<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json);
        }

        public T DeserializeJson<T>(object toDeserialize)
        {
            switch (toDeserialize)
            {
                case JObject jObject:
                    return jObject.ToObject<T>();
                case string str:
                    return DeserializeJson<T>(str);
                default:
                    throw new NotSupportedException();
            }
        }

        public string SerializeToJson<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj);
        }
    }
}