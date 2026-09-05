namespace Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Scripts
{
    public interface IArenaHudView
    {
        /// <summary>Number of life pips the view can show, so the presenter never over-reports.</summary>
        int PipCount { get; }

        void SetLives(int lives);
    }
}
