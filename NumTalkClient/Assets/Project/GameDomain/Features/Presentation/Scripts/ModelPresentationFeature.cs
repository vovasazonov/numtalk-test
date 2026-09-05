using Arch.Core;
using Project.GameDomain.Features.Configs.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Presentation.Scripts
{
    /// <summary>Shared per-frame visual output. Feature views may change this or their visual transform only.</summary>
    public struct ModelPresentationFrame
    {
        public Vector3 Position, PreviousPosition;
        public float DeltaTime, Time;
        public bool Initialized, Teleported;
        public Color Tint, Glow;
        public int AnimationState;
        public bool Visible;
    }

    /// <summary>Feature-owned behavior attached to an art prefab; the pooled model owns its lifetime.</summary>
    public abstract class ModelPresentationFeature : MonoBehaviour
    {
        protected World World { get; private set; }
        protected Entity Entity { get; private set; }
        protected PlatformerTuningConfig Tuning { get; private set; }

        public void Bind(World world, Entity entity, PlatformerTuningConfig tuning)
        {
            World = world;
            Entity = entity;
            Tuning = tuning;
            ResetPresentation();
        }

        public abstract void Present(ref ModelPresentationFrame frame);
        public virtual void ReleaseFeedback() { }
        public virtual void ResetPresentation() { }
    }
}
