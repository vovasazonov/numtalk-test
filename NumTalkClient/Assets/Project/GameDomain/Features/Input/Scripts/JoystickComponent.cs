using Unity.Mathematics;

namespace Project.GameDomain.Features.Input.Scripts
{
    public struct JoystickComponent
    {
        public bool IsDynamic;
        public bool IsPressed;
        public float2 Initial;
        public float2 Axis;
    }
}