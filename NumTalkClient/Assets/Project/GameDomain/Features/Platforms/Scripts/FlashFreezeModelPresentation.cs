using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    public sealed class FlashFreezeModelPresentation : ModelPresentationFeature
    {
        private Transform _frost;

        public override void Present(ref ModelPresentationFrame frame)
        {
            if (!World.TryGet(Entity, out FlashFreezeComponent freeze)) return;
            FlashFreezeNotice.Instance?.ShowWeather(freeze);
            if (_frost != null) _frost.gameObject.SetActive(freeze.Phase == FlashFreezePhase.Frozen);
            if (freeze.Phase == FlashFreezePhase.Warning)
                frame.Glow = new Color(0.15f, 0.65f, 1f) * (0.35f + 0.3f * Mathf.Sin(freeze.Elapsed * 8f));
            if (freeze.Phase == FlashFreezePhase.Frozen) frame.Tint = new Color(0.64f, 0.88f, 1f);
        }

        public override void ResetPresentation()
        {
            _frost = transform.Find("FrostOverlay");
            if (_frost != null) _frost.gameObject.SetActive(false);
        }
    }
}
