namespace Project.GameDomain.Features.Platforms.Scripts
{
    /// <summary>Scales only the rider's intrinsic deceleration, so intent still accelerates but momentum carries.</summary>
    public struct IceSurfaceComponent
    {
        public float DecelerationScale;
    }
}
