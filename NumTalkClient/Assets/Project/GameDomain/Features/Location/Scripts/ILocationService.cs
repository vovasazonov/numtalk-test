namespace Project.GameDomain.Features.Location.Scripts
{
    public interface ILocationService
    {
        LocationType Current { get; set; }
        float Gravity { get; }
    }
}
