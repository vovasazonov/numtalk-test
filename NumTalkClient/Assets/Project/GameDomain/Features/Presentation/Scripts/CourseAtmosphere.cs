using UnityEngine;
using UnityEngine.Rendering;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>Additive scenes do not own active RenderSettings, so arena atmosphere has an explicit lifetime.</summary>
    public sealed class CourseAtmosphere : MonoBehaviour
    {
        private bool _fog;
        private FogMode _fogMode;
        private float _density;
        private Color _fogColor, _ambient;
        private AmbientMode _ambientMode;

        private void Awake()
        {
            _fog = RenderSettings.fog; _fogMode = RenderSettings.fogMode;
            _density = RenderSettings.fogDensity; _fogColor = RenderSettings.fogColor;
            _ambient = RenderSettings.ambientLight; _ambientMode = RenderSettings.ambientMode;
            Apply();
        }

        public static void Apply()
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.012f;
            RenderSettings.fogColor = new Color(0.62f, 0.81f, 0.88f);
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.64f, 0.72f, 0.78f);
        }

        private void OnDestroy()
        {
            RenderSettings.fog = _fog; RenderSettings.fogMode = _fogMode;
            RenderSettings.fogDensity = _density; RenderSettings.fogColor = _fogColor;
            RenderSettings.ambientLight = _ambient; RenderSettings.ambientMode = _ambientMode;
        }
    }
}
