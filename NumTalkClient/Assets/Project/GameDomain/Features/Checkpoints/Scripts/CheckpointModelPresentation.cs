using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Checkpoints.Scripts
{
    public sealed class CheckpointModelPresentation : ModelPresentationFeature
    {
        public override void Present(ref ModelPresentationFrame frame)
        {
            if (World.TryGet(Entity, out CheckpointComponent checkpoint) && checkpoint.IsActivated)
                frame.Glow = new Color(0.1f, 0.65f, 0.35f);
        }
    }
}
