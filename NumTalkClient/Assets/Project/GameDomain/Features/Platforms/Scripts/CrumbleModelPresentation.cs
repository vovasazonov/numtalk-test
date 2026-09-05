using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Platforms.Scripts
{
    public sealed class CrumbleModelPresentation : ModelPresentationFeature
    {
        public override void Present(ref ModelPresentationFrame frame)
        {
            if (!World.TryGet(Entity, out CrumbleStateComponent crumble) || crumble.Phase != CrumblePhase.Telegraphing) return;
            float pulse = 0.5f + 0.5f * Mathf.Sin(crumble.PhaseTimer * 32f);
            frame.Glow = new Color(1f, 0.36f, 0.015f) * pulse;
            transform.localPosition = new Vector3(Mathf.Sin(crumble.PhaseTimer * 70f) * 0.012f, 0f, 0f);
        }
    }
}
