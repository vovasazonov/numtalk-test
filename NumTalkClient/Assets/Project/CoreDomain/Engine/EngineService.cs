using System;
using UnityEngine;
using VContainer.Unity;
using Object = UnityEngine.Object;

namespace Project.CoreDomain.Engine
{
    public class EngineService : IEngineService, IInitializable
    {
        public event Action Updating;
        public event Action FixedUpdating;
        public event Action Destroying;
        public event Action Paused;
        public event Action UnPaused;

        private EngineEvent _engineEvent;

        public void Initialize()
        {
            var gameObject = new GameObject(nameof(EngineService));
            _engineEvent = gameObject.AddComponent<EngineEvent>();
            _engineEvent.Initialize(this);
            Object.DontDestroyOnLoad(_engineEvent);
        }

        private void Update()
        {
            Updating?.Invoke();
        }

        private void FixedUpdate()
        {
            FixedUpdating?.Invoke();
        }

        private void Destroy()
        {
            Destroying?.Invoke();
        }
        
        private class EngineEvent : MonoBehaviour
        {
            private EngineService _engineService;

            public void Initialize(EngineService engineService)
            {
                _engineService = engineService;
            }

            private void Update()
            {
                _engineService.Update();
            }

            private void FixedUpdate()
            {
                _engineService.FixedUpdate();
            }

            private void OnDestroy()
            {
                _engineService.Destroy();
            }

            private void OnApplicationPause(bool isPaused)
            {
                if (isPaused)
                {
                    _engineService.Paused?.Invoke();
                }
                else
                {
                    _engineService.UnPaused?.Invoke();
                }
            }
        }
    }
}