using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Project.CoreDomain.Screen
{
    public class ScreensService : IScreensService, IScreenInitializable
    {
        private readonly Dictionary<string, IScreenFactory> _screenFactories;
        private readonly Queue<string> _screenToOpenQueue = new();
        private readonly Dictionary<string, IScreen> _aliveScreens = new();
        private bool _isChanging;
        private string _loadingScreenId;
        private string _splashScreenId;

        public static ScreensService Instance { get; private set; }

        public event Action Switched;
        public string Current { get; private set; }

        public ScreensService(IReadOnlyList<IScreenFactory> screenFactories)
        {
            _screenFactories = screenFactories.ToDictionary(k => k.Id, v => v);
            Instance = this;
        }

        public async UniTask SwitchAsync(string screenId)
        {
            if (Current != null && Current != _splashScreenId && _loadingScreenId != null)
            {
                _screenToOpenQueue.Enqueue(_loadingScreenId);
            }
            
            _screenToOpenQueue.Enqueue(screenId);

            if (!_isChanging)
            {
                await ChangeCurrent();
            }
        }

        private async UniTask ChangeCurrent()
        {
            _isChanging = true;

            while (_screenToOpenQueue.Count != 0)
            {
                var newScreenId = _screenToOpenQueue.Dequeue();
                var oldScreenId = Current;
                Current = newScreenId;

                if (!_aliveScreens.ContainsKey(newScreenId))
                {
                    var screen = _screenFactories[newScreenId].Create();
                    _aliveScreens.Add(newScreenId, screen);
                    await _aliveScreens[newScreenId].InitializeAsync();
                }
                
                await _aliveScreens[newScreenId].ShowAsync();

                if (oldScreenId != null)
                {
                    var oldScreen = _aliveScreens[oldScreenId];
                    
                    await oldScreen.HideAsync();

                    if (oldScreen.IsDisposeOnSwitch)
                    {
                        _aliveScreens.Remove(oldScreenId);
                        await oldScreen.DisposeAsync();
                    }
                }
                
                Switched?.Invoke();
            }

            _isChanging = false;
        }

        public void SetLoadingScreen(string id)
        {
            _loadingScreenId = id;
        }

        public void SetSplashScreen(string id)
        {
            _splashScreenId = id;
        }
    }
}