using System;
using System.Collections.Generic;
using Project.CoreDomain.Serialization;
using UnityEngine;

namespace Project.GameDomain.Features.Configs.Scripts
{
    public class ConfigService : IConfigService
    {
        private readonly ISerializerService _serializerService;
        private readonly Dictionary<Type, object> _cache = new();

        public ConfigService(ISerializerService serializerService)
        {
            _serializerService = serializerService;
        }

        public T Get<T>()
        {
            Type type = typeof(T);
            if (_cache.TryGetValue(type, out object cached))
            {
                return (T)cached;
            }

            TextAsset asset = Resources.Load<TextAsset>(ResolveKey(type));
            T config = _serializerService.DeserializeJson<T>(asset.text);
            _cache[type] = config;
            return config;
        }

        private static string ResolveKey(Type type)
        {
            ConfigKeyAttribute key = (ConfigKeyAttribute)Attribute.GetCustomAttribute(type, typeof(ConfigKeyAttribute));
            return key != null ? key.Key : type.Name;
        }
    }
}
