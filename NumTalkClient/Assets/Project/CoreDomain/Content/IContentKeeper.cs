using System;

namespace Project.CoreDomain.Content
{
    public interface IContentKeeper<out T> : IDisposable
    {
        T Value { get; }
    }
}