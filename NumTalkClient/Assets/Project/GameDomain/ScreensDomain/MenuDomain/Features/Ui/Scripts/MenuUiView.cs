using System;
using UnityEngine;
using UnityEngine.UI;

namespace Project.GameDomain.ScreensDomain.MenuDomain.Features.Ui.Scripts
{
    public class MenuUiView : MonoBehaviour, IMenuUiView
    {
        [SerializeField] private Button _playButton;

        public event Action PlayClicked;

        private void Awake()
        {
            _playButton.onClick.AddListener(OnPlayClicked);
        }

        private void OnDestroy()
        {
            _playButton.onClick.RemoveListener(OnPlayClicked);
        }

        private void OnPlayClicked()
        {
            PlayClicked?.Invoke();
        }
    }
}
