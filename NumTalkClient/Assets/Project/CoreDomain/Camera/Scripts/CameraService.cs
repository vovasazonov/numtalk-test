using System;
using UnityEngine;
using VContainer.Unity;

namespace Project.CoreDomain.Camera.Scripts
{
    public class CameraService : ICameraService, IInitializable
    {
        public ICamera Camera { get; private set; }

        public Vector2 ConvertScreenToWorldPosition(Vector2 screenPosition)
        {
            return Camera.UnityCamera.ScreenToWorldPoint(new Vector3(screenPosition.x, screenPosition.y, -1 * Camera.UnityCamera.transform.position.z));
        }

        public void Initialize()
        {
            if (UnityEngine.Camera.main != null)
            {
                Camera = UnityEngine.Camera.main.GetComponent<CameraView>();
            }
            else
            {
                throw new NullReferenceException();
            }
        }
    }
}