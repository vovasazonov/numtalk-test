using System.Collections.Generic;
using Project.CoreDomain.VContainer;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Project.EntryDomain.Scripts.Scopes
{
    public class RootScope : LifetimeScope
    {
        [SerializeField] private List<ScriptableInstaller> _installers;

        protected override void Awake()
        {
            autoInjectGameObjects ??= new List<GameObject>();
            autoInjectGameObjects.Add(gameObject);
            base.Awake();
        }

        protected override void Configure(IContainerBuilder builder)
        {
            foreach (var module in _installers)
            {
                module.Install(builder, this);
            }
        }
    }
}
