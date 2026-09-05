namespace Project.GameDomain.Features.Pushables.Scripts
{
    /// <summary>A dynamic body the player shoves by walking into it. Also a valid ride surface.</summary>
    public struct PushableComponent
    {
        /// <summary>Horizontal push acceleration applied to the body on a valid contact, in metres per second squared.</summary>
        public float PushAcceleration;
    }
}
