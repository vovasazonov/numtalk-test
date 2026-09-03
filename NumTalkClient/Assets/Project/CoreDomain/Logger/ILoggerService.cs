using System;

namespace Project.CoreDomain.Logger
{
    public interface ILoggerService
    {
        void Log(string text);
        void LogError(string text);
        void LogWarning(string text);

        void LogException(Exception exception);
    }
}