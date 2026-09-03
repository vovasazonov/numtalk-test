using System;
using Project.GameDomain.Features.EcsArchitecture.Scripts;
using UnityEngine;

namespace Project.GameDomain.Features.Creature.Scripts
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CreatureComponentListener : ComponentListener<CreatureComponent>
    {
        [SerializeField] private SpriteRenderer _spriteRenderer;
        [SerializeField] private SpriteRenderer _shadowRenderer;
        [SerializeField] private CreatureStateConfigDatabase _database;
        [SerializeField] private float _framesPerSecond = 10f;

        private CreatureType _type = CreatureType.None;
        private CreatureState _state = CreatureState.None;
        private CreatureSide _side = CreatureSide.None;
        private Sprite[] _frames = Array.Empty<Sprite>();
        private Sprite[] _shadowFrames = Array.Empty<Sprite>();
        private int _frameIndex;
        private float _timer;

        private CreatureType _desiredType = CreatureType.None;
        private CreatureState _desiredState = CreatureState.None;
        private CreatureSide _desiredSide = CreatureSide.None;
        private float _lockRemaining;
        private bool _isOneShot;

        private bool IsDesiredDifferent =>
            _desiredType != _type || _desiredState != _state || _desiredSide != _side;

        public override void UpdateView(in CreatureComponent component)
        {
            ApplyHeightAboveGround(component.HeightAboveGround);

            _desiredType = component.Type;
            _desiredState = component.State;
            _desiredSide = component.Side;

            if (_lockRemaining > 0f)
            {
                _lockRemaining -= Time.deltaTime;
            }

            bool isReady = _lockRemaining <= 0f;
            if (_state == CreatureState.None || (isReady && IsDesiredDifferent))
            {
                ApplyDesired();
            }

            Advance();
        }

        private void ApplyHeightAboveGround(float height)
        {
            transform.localPosition = new Vector3(0f, height, 0f);

            if (_shadowRenderer != null)
            {
                _shadowRenderer.transform.localPosition = new Vector3(0f, -height, 0f);
            }
        }

        private void ApplyDesired()
        {
            _type = _desiredType;
            _state = _desiredState;
            _side = _desiredSide;
            _frames = ResolveFrames(_type, _state, _side);
            _shadowFrames = ResolveShadowFrames(_type, _state, _side);
            _isOneShot = ResolveIsOneShot(_type, _state);
            _frameIndex = 0;
            _timer = 0f;
            _lockRemaining = ResolveLockDuration();
            ApplyFrame();
        }

        private float ResolveLockDuration()
        {
            if (!_isOneShot || _framesPerSecond <= 0f || _frames.Length <= 1)
            {
                return 0f;
            }

            return _frames.Length / _framesPerSecond;
        }

        private bool ResolveIsOneShot(CreatureType type, CreatureState state)
        {
            return _database != null
                && _database.TryGet(type, out CreatureStateConfig config)
                && config.TryGet(state, out CreatureStateConfig.StateSprites sprites)
                && sprites.IsOneShot;
        }

        private void Advance()
        {
            if (_frames.Length <= 1 || _framesPerSecond <= 0f)
            {
                return;
            }

            _timer += Time.deltaTime;
            float frameDuration = 1f / _framesPerSecond;

            while (_timer >= frameDuration)
            {
                _timer -= frameDuration;

                if (_frameIndex + 1 < _frames.Length)
                {
                    _frameIndex++;
                    ApplyFrame();
                }
                else if (_isOneShot)
                {
                    _timer = 0f;
                    break;
                }
                else
                {
                    _frameIndex = 0;
                    ApplyFrame();
                }
            }
        }

        private void ApplyFrame()
        {
            if (_spriteRenderer != null && _frameIndex < _frames.Length)
            {
                _spriteRenderer.sprite = _frames[_frameIndex];
            }

            if (_shadowRenderer != null)
            {
                _shadowRenderer.sprite = _frameIndex < _shadowFrames.Length
                    ? _shadowFrames[_frameIndex]
                    : null;
            }
        }

        private Sprite[] ResolveFrames(CreatureType type, CreatureState state, CreatureSide side)
        {
            if (_database != null
                && _database.TryGet(type, out CreatureStateConfig config)
                && config.TryGet(state, out CreatureStateConfig.StateSprites sprites))
            {
                Sprite[] frames = Pick(side, sprites.DownRight, sprites.DownLeft, sprites.UpRight, sprites.UpLeft);
                if (frames is { Length: > 0 })
                {
                    return frames;
                }
            }

            return Array.Empty<Sprite>();
        }

        private Sprite[] ResolveShadowFrames(CreatureType type, CreatureState state, CreatureSide side)
        {
            if (_database != null
                && _database.TryGet(type, out CreatureStateConfig config)
                && config.TryGet(state, out CreatureStateConfig.StateSprites sprites))
            {
                Sprite[] frames = Pick(side, sprites.ShadowDownRight, sprites.ShadowDownLeft, sprites.ShadowUpRight, sprites.ShadowUpLeft);
                if (frames is { Length: > 0 })
                {
                    return frames;
                }
            }

            return Array.Empty<Sprite>();
        }

        private static Sprite[] Pick(CreatureSide side, Sprite[] downRight, Sprite[] downLeft, Sprite[] upRight, Sprite[] upLeft)
        {
            bool isUp = (side & CreatureSide.Up) != 0;
            bool isLeft = (side & CreatureSide.Left) != 0;

            Sprite[] frames = isUp
                ? (isLeft ? upLeft : upRight)
                : (isLeft ? downLeft : downRight);

            return frames.Length > 0 ? frames : downRight;
        }

        private void OnDisable()
        {
            _type = CreatureType.None;
            _state = CreatureState.None;
            _side = CreatureSide.None;
            _desiredType = CreatureType.None;
            _desiredState = CreatureState.None;
            _desiredSide = CreatureSide.None;
            _lockRemaining = 0f;
            _isOneShot = false;
        }
    }
}
