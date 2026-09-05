namespace Project.GameDomain.Features.Player.Scripts
{
    /// <summary>Jump forgiveness timers, in seconds, and the held/ascending flags used for jump cutting.</summary>
    public struct JumpStateComponent
    {
        public bool IsHeld;
        public bool IsAscending;
        public float CoyoteTimer;
        public float BufferTimer;
    }
}
