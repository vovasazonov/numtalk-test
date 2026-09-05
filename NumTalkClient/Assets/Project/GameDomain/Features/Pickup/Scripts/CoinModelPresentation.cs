using Project.GameDomain.Features.Audio.Scripts;
using Project.GameDomain.Features.Presentation.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Pickup.Scripts
{
    public sealed class CoinModelPresentation : ModelPresentationFeature
    {
        public override void Present(ref ModelPresentationFrame frame)
        {
            transform.localRotation = Quaternion.Euler(0f, frame.Time * 100f, 0f);
            transform.localPosition = Vector3.up * (Mathf.Sin(frame.Time * 3f + frame.Position.z) * 0.13f);
        }

        public override void ReleaseFeedback()
        {
            if (World.TryGet(Entity, out PickupComponent pickup) && pickup.IsCollected)
            {
                CourseEffects.Instance?.Burst(transform.position, new Color(1f, 0.76f, 0.12f), 16);
                CourseAudio.Instance?.Play(CourseSound.Coin);
            }
        }
    }
}
