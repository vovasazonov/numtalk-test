namespace Project.GameDomain.Features.Physics.Scripts
{
    /// <summary>A dynamic Unity body. Its listener requires a Rigidbody on the entity root.</summary>
    public struct PhysicsBodyComponent
    {
        public float Mass;
        public bool FreezeRotation;
    }
}
