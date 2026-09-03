using System;

namespace Project.GameDomain.Features.Configs.Scripts
{
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ConfigKeyAttribute : Attribute
    {
        public string Key { get; }

        public ConfigKeyAttribute(string key)
        {
            Key = key;
        }
    }
}
