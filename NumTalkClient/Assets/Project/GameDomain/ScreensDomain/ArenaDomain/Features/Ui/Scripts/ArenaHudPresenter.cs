using Arch.Core;
using Project.GameDomain.Features.Player.Scripts;
using VContainer.Unity;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Scripts
{
    /// <summary>
    /// Pushes the player's remaining lives into the HUD. The simulation owns the number; the presenter only
    /// notices when it changes, so a respawn or a full restart shows up without either side knowing about the other.
    /// </summary>
    public class ArenaHudPresenter : IInitializable, ITickable
    {
        private readonly IArenaHudView _view;
        private readonly World _world;

        private readonly QueryDescription _players = new QueryDescription()
            .WithAll<PlayerTagComponent, HealthComponent>();

        private readonly ForEach _readLives;

        private int _lives;
        private int _shownLives = -1;

        public ArenaHudPresenter(IArenaHudView view, World world)
        {
            _view = view;
            _world = world;
            _readLives = ReadLives;
        }

        public void Initialize() => _view.SetLives(_view.PipCount);

        public void Tick()
        {
            _world.Query(in _players, _readLives);
            if (_lives == _shownLives) return;

            _shownLives = _lives;
            _view.SetLives(_lives);
        }

        private void ReadLives(Entity entity) => _lives = _world.Get<HealthComponent>(entity).Lives;
    }
}
