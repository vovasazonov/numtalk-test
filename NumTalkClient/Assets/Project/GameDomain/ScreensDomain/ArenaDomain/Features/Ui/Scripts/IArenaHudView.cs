using System;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Scripts
{
    public interface IArenaHudView
    {
        event Action RestartClicked;

        /// <summary>Number of life pips the view can show, so the presenter never over-reports.</summary>
        int PipCount { get; }

        void SetLives(int lives);

        void SetCoins(int collected, int total);

        /// <summary>Shows the finish overlay, which is the only place the restart is offered.</summary>
        void SetRunComplete(bool isComplete);
    }
}
