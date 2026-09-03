using System;

namespace Project.CoreDomain.Time
{
    public interface ITimeService
    {
        float DeltaTime { get; }
        float FixedTime { get; }
        float GameTimeInSeconds { get; }
        DateTime UtcTime { get; }
    }
}