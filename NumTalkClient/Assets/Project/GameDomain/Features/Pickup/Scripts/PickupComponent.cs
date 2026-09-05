namespace Project.GameDomain.Features.Pickup.Scripts
{
    /// <summary>
    /// A collectable. <see cref="Id"/> is authored and stable, so a checkpoint snapshot can record which coins are
    /// already collected without paying the reward twice on respawn.
    /// </summary>
    public struct PickupComponent
    {
        public int Id;
        public int Value;
        public bool IsCollected;
    }
}
