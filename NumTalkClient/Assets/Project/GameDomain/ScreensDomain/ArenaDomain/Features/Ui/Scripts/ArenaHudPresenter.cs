using System;
using Arch.Core;
using Project.GameDomain.Features.Course.Scripts;
using Project.GameDomain.Features.Pickup.Scripts;
using Project.GameDomain.Features.Player.Scripts;
using VContainer.Unity;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Scripts
{
    /// <summary>
    /// Pushes the run into the HUD: lives, the coin route's progress, and the finish overlay. The simulation owns
    /// every one of those numbers; the presenter only notices when they change, and turns a restart click into the
    /// request the respawn system already consumes.
    /// </summary>
    public class ArenaHudPresenter : IInitializable, ITickable, IDisposable
    {
        private readonly IArenaHudView _view;
        private readonly World _world;

        private readonly QueryDescription _players = new QueryDescription()
            .WithAll<PlayerTagComponent, HealthComponent, RunStateComponent>();

        private readonly QueryDescription _pickups = new QueryDescription().WithAll<PickupComponent>();

        private readonly ForEach _readPlayer;
        private readonly ForEach _countCoins;

        private int _lives;
        private int _collected;
        private int _total;
        private bool _isComplete;

        private int _shownLives = -1;
        private int _shownCollected = -1;
        private int _shownTotal = -1;
        private bool _shownComplete;

        public ArenaHudPresenter(IArenaHudView view, World world)
        {
            _view = view;
            _world = world;
            _readPlayer = ReadPlayer;
            _countCoins = CountCoin;
        }

        public void Initialize()
        {
            _view.RestartClicked += OnRestartClicked;
            _view.SetLives(_view.PipCount);
            _view.SetRunComplete(false);
        }

        public void Dispose() => _view.RestartClicked -= OnRestartClicked;

        public void Tick()
        {
            _world.Query(in _players, _readPlayer);

            _collected = 0;
            _total = 0;
            _world.Query(in _pickups, _countCoins);

            if (_lives != _shownLives)
            {
                _shownLives = _lives;
                _view.SetLives(_lives);
            }

            if (_collected != _shownCollected || _total != _shownTotal)
            {
                _shownCollected = _collected;
                _shownTotal = _total;
                _view.SetCoins(_collected, _total);
            }

            if (_isComplete == _shownComplete) return;

            _shownComplete = _isComplete;
            _view.SetRunComplete(_isComplete);
        }

        private void OnRestartClicked() => _world.Query(in _players, RequestRestart);

        private void RequestRestart(Entity entity) => _world.Get<RunStateComponent>(entity).RestartRequested = true;

        private void ReadPlayer(Entity entity)
        {
            _lives = _world.Get<HealthComponent>(entity).Lives;
            _isComplete = _world.Get<RunStateComponent>(entity).IsComplete;
        }

        private void CountCoin(Entity entity)
        {
            _total++;
            if (_world.Get<PickupComponent>(entity).IsCollected) _collected++;
        }
    }
}
