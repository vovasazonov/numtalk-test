namespace Project.GameDomain.Features.Course.Scripts
{
    /// <summary>
    /// The run as a whole, on the player entity. The goal sets <see cref="IsComplete"/>; the overlay sets
    /// <see cref="RestartRequested"/>, which is consumed by the same restart path a zero-life run takes.
    /// </summary>
    public struct RunStateComponent
    {
        public bool IsComplete;
        public bool RestartRequested;
    }
}
