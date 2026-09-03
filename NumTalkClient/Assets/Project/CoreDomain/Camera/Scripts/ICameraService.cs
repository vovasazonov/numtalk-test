using UnityEngine;

namespace Project.CoreDomain.Camera.Scripts
{
    public interface ICameraService
    {
        ICamera Camera { get; }
        
        Vector2 ConvertScreenToWorldPosition(Vector2 screenPosition);
    }
}