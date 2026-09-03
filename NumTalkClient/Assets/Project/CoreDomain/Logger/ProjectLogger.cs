using System;
using Project.CoreDomain.Logger;

// ReSharper disable All

namespace Project.CoreDomain.Scripts.Logger
{
    public static class ProjectLogger
    {
        private static ILoggerService _loggerService;

        public static void Log(string text)
        {
            CheckForLogger();
            _loggerService.Log(text);
        }

        public static void LogError(string text)
        {
            CheckForLogger();
            _loggerService.LogError(text);
        }

        public static void LogWarning(string text)
        {
            CheckForLogger();
            _loggerService.LogWarning(text);
        }

        public static void LogException(Exception exception)
        {
            CheckForLogger();
            _loggerService.LogException(exception);
        }

        internal static void SetLogger(ILoggerService loggerService)
        {
            _loggerService = loggerService;
        }

        private static void CheckForLogger()
        {
            if (_loggerService == null)
            {
                _loggerService = new LoggerService();
                LogWarning("Logger was null, using Unity's logger");
            }
        }
    }
}
