using Project.CoreDomain.Scripts.Logger;
using UnityEngine.Localization.Settings;

namespace Project.CoreDomain.Localization
{
    public class LocalizationService : ILocalizationService
    {
        private const string TableName = "General";

        public string Get(string key)
        {
            var result = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key);

            if (string.IsNullOrEmpty(result) || result == key)
            {
                ProjectLogger.LogWarning($"[Localization] Missing key: {key}");
            }

            return result;
        }

        public string Get(string key, params object[] arguments)
        {
            var result = LocalizationSettings.StringDatabase.GetLocalizedString(TableName, key, arguments);

            if (string.IsNullOrEmpty(result) || result == key)
            {
                ProjectLogger.LogWarning($"[Localization] Missing key: {key}");
            }

            return result;
        }
    }
}
