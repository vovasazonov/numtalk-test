using System.Runtime.CompilerServices;

namespace Project.GameDomain.Features.Universe.Scripts
{
    public static class UniverseConsts
    {
        public const int PixelsPerUnit = 32;
        public const float UnitsPerPixel = 1f / PixelsPerUnit;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float CalculateUnitsBasePixels(int pixels)
        {
            return pixels * UnitsPerPixel;
        }
    }
}