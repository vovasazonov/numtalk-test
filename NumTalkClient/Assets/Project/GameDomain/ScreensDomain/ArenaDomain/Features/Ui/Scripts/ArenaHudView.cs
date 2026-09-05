using UnityEngine;
using UnityEngine.UI;

namespace Project.GameDomain.ScreensDomain.ArenaDomain.Features.Ui.Scripts
{
    /// <summary>Dumb view: it owns the pip graphics and nothing about when a life is lost.</summary>
    public class ArenaHudView : MonoBehaviour, IArenaHudView
    {
        [SerializeField] private Image[] _lifePips;
        [SerializeField] private Color _filledColor = Color.white;
        [SerializeField] private Color _emptyColor = new(1f, 1f, 1f, 0.2f);

        public int PipCount => _lifePips.Length;

        public void SetLives(int lives)
        {
            for (int index = 0; index < _lifePips.Length; index++)
            {
                _lifePips[index].color = index < lives ? _filledColor : _emptyColor;
            }
        }
    }
}
