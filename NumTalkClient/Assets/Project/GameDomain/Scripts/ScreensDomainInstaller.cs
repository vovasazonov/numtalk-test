using System;
using System.Collections.Generic;
using Project.CoreDomain.Screen;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.GameDomain.Scripts
{
    [Serializable]
    public class ScreensDomainInstaller
    {
        [SerializeField] private List<ScreenEntry> _screens;

        public void Install(IContainerBuilder builder, LifetimeScope scope)
        {
            var factories = new List<IScreenFactory>();

            foreach (var screen in _screens)
            {
                factories.Add(new ScreenPrefabFactory(screen.ScreenId, screen.Prefab, scope));
            }

            builder.RegisterInstance<IReadOnlyList<IScreenFactory>>(factories);
        }

        [Serializable]
        public class ScreenEntry
        {
            public string ScreenId;
            public GameObject Prefab;
        }
    }
}
