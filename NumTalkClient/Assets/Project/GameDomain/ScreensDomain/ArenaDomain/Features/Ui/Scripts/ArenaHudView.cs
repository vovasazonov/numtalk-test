using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Scripts
{
    /// <summary>Dumb view: it owns the pips, the counter and the finish overlay, and nothing about the run.</summary>
    public class ArenaHudView : MonoBehaviour, IArenaHudView
    {
        [SerializeField] private Image[] _lifePips;
        [SerializeField] private Text _coinLabel;
        [SerializeField] private GameObject _completePanel;
        [SerializeField] private Button _restartButton;
        [SerializeField] private Color _filledColor = Color.white;
        [SerializeField] private Color _emptyColor = new(1f, 1f, 1f, 0.2f);

        public event Action RestartClicked;

        public int PipCount => _lifePips.Length;

        private void Awake()
        {
            _restartButton.onClick.AddListener(OnRestartClicked);
        }

        private void OnDestroy()
        {
            _restartButton.onClick.RemoveListener(OnRestartClicked);
        }

        public void SetLives(int lives)
        {
            for (int index = 0; index < _lifePips.Length; index++)
            {
                _lifePips[index].color = index < lives ? _filledColor : _emptyColor;
            }
        }

        public void SetCoins(int collected, int total) => _coinLabel.text = $"COINS   {collected:00} / {total}";

        public void SetRunComplete(bool isComplete)
        {
            _completePanel.SetActive(isComplete);
            Project.GameDomain.Features.Platforms.Scripts.FlashFreezeNotice.Instance?.SetRunComplete(isComplete);
        }

        private void OnRestartClicked() => RestartClicked?.Invoke();
    }
}
