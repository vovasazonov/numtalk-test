using System.Collections.Generic;
using Newtonsoft.Json;
using Project.GameDomain.Features.Configs.Scripts;

namespace Project.GameDomain.Features.Location.Scripts
{
    [ConfigKey("locations")]
    public class LocationsConfig
    {
        [JsonProperty("initial")] public LocationType Initial;
        [JsonProperty("locations")] public List<Location> Locations;

        public class Location
        {
            [JsonProperty("location_type")] public LocationType LocationType;
            [JsonProperty("gravity")] public float Gravity;
        }
    }
}
