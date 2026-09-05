using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Checkpoints.Scripts
{
    public sealed class CheckpointModelPresentation : ModelPresentationFeature
    {
        public Transform Gate;
        public Renderer GateRenderer;
        private MaterialPropertyBlock _properties;
        private bool _observed, _activated;
        private float _activationTime;
        private static readonly int Activation = Shader.PropertyToID("_Activation");
        private static readonly int Tint = Shader.PropertyToID("_Tint");

        public override void Present(ref ModelPresentationFrame frame)
        {
            if (!World.TryGet(Entity, out CheckpointComponent checkpoint)) return;
            if (checkpoint.IsActivated)
            {
                frame.Tint = new Color(0.55f, 1f, 0.72f);
                frame.Glow = new Color(0.1f, 0.65f, 0.35f);
                if (_observed && !_activated)
                {
                    _activationTime = 1.1f;
                    CourseEffects.Instance?.Burst(frame.Position + Vector3.up, new Color(0.35f, 1f, 0.65f), 32);
                }
            }
            else _activationTime = 0f;
            _activationTime = Mathf.Max(0f, _activationTime - frame.DeltaTime);
            if (Gate != null)
            {
                Gate.gameObject.SetActive(!checkpoint.IsActivated || _activationTime > 0f);
                // Keep the entire ring above the platform, centered within the three-metre trigger.
                Gate.position = frame.Position + Vector3.up * 1.65f;
                Gate.rotation = Quaternion.identity;
                var scale = Gate.parent.lossyScale;
                Gate.localScale = new Vector3(3.2f / scale.x, 3.2f / scale.y, 1f / scale.z);
                _properties ??= new MaterialPropertyBlock();
                _properties.SetFloat(Activation, checkpoint.IsActivated ? 1f - _activationTime / 1.1f : 0f);
                _properties.SetColor(Tint, checkpoint.IsActivated ? new Color(1f, 0.85f, 0.3f) : new Color(0.2f, 1f, 0.8f));
                GateRenderer.SetPropertyBlock(_properties);
            }
            _observed = true;
            _activated = checkpoint.IsActivated;
        }

        public override void ResetPresentation()
        {
            _observed = _activated = false;
            _activationTime = 0f;
            if (Gate != null) Gate.gameObject.SetActive(false);
        }
    }
}
