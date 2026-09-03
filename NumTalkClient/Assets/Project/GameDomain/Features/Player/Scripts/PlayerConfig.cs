using Newtonsoft.Json;
using Project.GameDomain.Features.Configs.Scripts;

namespace Project.GameDomain.Features.Player.Scripts
{
    [ConfigKey("player")]
    public class PlayerConfig
    {
        [JsonProperty("base_speed")] public float BaseSpeed;
        [JsonProperty("horizontal_boost")] public float HorizontalBoost;
        [JsonProperty("min_speed")] public float MinSpeed;
        [JsonProperty("vertical_speed")] public float VerticalSpeed;
        [JsonProperty("return_rate")] public float ReturnRate;
        [JsonProperty("jump_force")] public float JumpForce;
    }
}
