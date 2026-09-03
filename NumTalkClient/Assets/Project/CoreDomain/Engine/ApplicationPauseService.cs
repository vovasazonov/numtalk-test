using System;
using UnityEngine;
using VContainer.Unity;

namespace Project.CoreDomain.Engine
{
    public sealed class ApplicationPauseService : IInitializable, IDisposable
    {
        private readonly IEngineService _engineService;

        public ApplicationPauseService(IEngineService engineService)
        {
            _engineService = engineService;
        }

        public void Initialize()
        {
            _engineService.Paused += OnPaused;
            _engineService.UnPaused += OnUnPaused;
        }

        public void Dispose()
        {
            _engineService.Paused -= OnPaused;
            _engineService.UnPaused -= OnUnPaused;
        }

        private void OnPaused()
        {
            AudioListener.pause = true;
            UnityEngine.Time.timeScale = 0f;
        }

        private void OnUnPaused()
        {
            AudioListener.pause = false;
            UnityEngine.Time.timeScale = 1f;
        }
    }
}
