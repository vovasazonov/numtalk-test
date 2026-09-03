using System;
using Project.CoreDomain.Content;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.CoreDomain.View
{
    internal class ContentDisposableView<T> : IContentKeeper<T>
    {
        private readonly IDisposable _prefabKeeper;
        private readonly GameObject _gameObject;
        private bool _isDisposed;

        public T Value { get; }

        public ContentDisposableView(
            T component,
            GameObject gameObject,
            IDisposable prefabKeeper
        )
        {
            Value = component;
            _gameObject = gameObject;
            _prefabKeeper = prefabKeeper;
        }

        public void Dispose()
        {
            if (!_isDisposed)
            {
                Object.Destroy(_gameObject);
                _prefabKeeper.Dispose();

                _isDisposed = true;
            }
        }
    }
}