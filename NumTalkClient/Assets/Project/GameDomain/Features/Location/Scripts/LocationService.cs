using Project.CoreDomain.Data;
using Project.GameDomain.Features.Configs.Scripts;

namespace Project.GameDomain.Features.Location.Scripts
{
    public class LocationService : ILocationService
    {
        private const string DataKey = "location";

        private readonly IConfigService _configService;
        private readonly IDataStorageService _dataStorageService;

        private LocationData _data;

        public LocationType Current
        {
            get => Data.Current;
            set => Data.Current = value;
        }

        public float Gravity
        {
            get
            {
                LocationType current = Current;
                foreach (LocationsConfig.Location location in Config.Locations)
                {
                    if (location.LocationType == current)
                    {
                        return location.Gravity;
                    }
                }

                return 0f;
            }
        }

        private LocationsConfig Config => _configService.Get<LocationsConfig>();

        private LocationData Data
        {
            get
            {
                if (_data == null)
                {
                    _data = _dataStorageService.Contains(DataKey)
                        ? _dataStorageService.Get<LocationData>(DataKey)
                        : CreateInitialData();
                }

                return _data;
            }
        }

        public LocationService(IConfigService configService, IDataStorageService dataStorageService)
        {
            _configService = configService;
            _dataStorageService = dataStorageService;
        }

        private LocationData CreateInitialData()
        {
            LocationData data = _dataStorageService.Create<LocationData>(DataKey);
            data.Current = Config.Initial;
            return data;
        }
    }
}
