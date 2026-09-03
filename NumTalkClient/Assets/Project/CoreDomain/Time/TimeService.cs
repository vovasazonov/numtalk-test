using System;

namespace Project.CoreDomain.Time
{
    public class TimeService : ITimeService
    {
        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float FixedTime => UnityEngine.Time.fixedTime;
        public float GameTimeInSeconds => UnityEngine.Time.time;
        public DateTime UtcTime => DateTime.UtcNow;
    }
}