using System;
using Project.CoreDomain.Scripts.Logger;
using UnityEngine;

namespace Project.CoreDomain.Logger
{
    public class LoggerService : ILoggerService
    {
        public LoggerService()
        {
            ProjectLogger.SetLogger(this);
        }

        public void Log(string text)
        {
            Debug.Log(text);
        }

        public void LogError(string text)
        {
            Debug.LogError(text);
        }

        public void LogWarning(string text)
        {
            Debug.LogWarning(text);
        }

        public void LogException(Exception exception)
        {
            Debug.LogException(exception);
        }
    }
}
