using UnityEngine;
using UnityEngine.UI;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.Features.PlayerInput.Scripts
{
    /// <summary>
    /// Draws the floating stick and the jump press so the controls are visible while playing and tuning. It reads
    /// the input source and never writes to it, so removing this view cannot change how the game plays.
    /// </summary>
    public sealed class TouchControlsView : MonoBehaviour, ITickable
    {
        private const float KnobDiameterScale = 0.45f;

        [SerializeField] private Image _stickRing;
        [SerializeField] private Image _stickKnob;
        [SerializeField] private Image _jumpMarker;

        private TouchPlayerInputSource _source;

        [Inject]
        public void Construct(TouchPlayerInputSource source)
        {
            _source = source;
        }

        public void Tick()
        {
            DrawStick();
            DrawJump();
        }

        private void DrawStick()
        {
            bool isDown = _source.StickIsDown;
            _stickRing.enabled = isDown;
            _stickKnob.enabled = isDown;

            if (!isDown)
            {
                return;
            }

            float diameter = _source.StickRadiusPixels * 2f;
            Place(_stickRing, _source.StickCenter, diameter);
            Place(_stickKnob, _source.StickKnob, diameter * KnobDiameterScale);
        }

        private void DrawJump()
        {
            bool isHeld = _source.JumpHeld;
            _jumpMarker.enabled = isHeld;

            if (isHeld)
            {
                Place(_jumpMarker, _source.JumpPosition, _source.StickRadiusPixels);
            }
        }

        private static void Place(Image image, Vector2 screenPosition, float diameter)
        {
            var rect = (RectTransform)image.transform;
            rect.sizeDelta = new Vector2(diameter, diameter);
            rect.anchoredPosition = screenPosition;
        }
    }
}
