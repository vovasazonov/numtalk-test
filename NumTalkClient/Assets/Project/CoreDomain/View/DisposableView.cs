using Project.CoreDomain.Content;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Project.CoreDomain.View
{
    internal abstract class DisposableView<T> : IContentKeeper<T>
    {
        private readonly GameObject _gameObject;
        private bool _isDisposed;
        
        public T Value { get; }

        protected DisposableView(T value, GameObject gameObject)
        {
            _gameObject = gameObject;
            Value = value;
        }

        protected abstract void DisposeInternal();
        
        public void Dispose()
        {
            if (!_isDisposed)
            {
                Object.Destroy(_gameObject);
                DisposeInternal();
                
                _isDisposed = true;
            }
        }
    }
}