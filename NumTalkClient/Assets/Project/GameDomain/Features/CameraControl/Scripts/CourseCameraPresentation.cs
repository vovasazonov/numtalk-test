using System;
using Project.CoreDomain.Camera.Scripts;
using Project.GameDomain.Features.Configs.Scripts;
using Unity.Mathematics;
using UnityEngine;

namespace Project.GameDomain.Features.CameraControl.Scripts
{
    /// <summary>Owns Unity camera changes for the arena and restores the shared camera on release.</summary>
    public sealed class CourseCameraPresentation : IDisposable
    {
        private readonly ICameraService _cameras;
        private UnityEngine.Camera _camera;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        private bool _originalOrthographic;
        private float _originalFieldOfView;
        private Color _originalBackground;
        private CameraClearFlags _originalClearFlags;

        public CourseCameraPresentation(ICameraService cameras) => _cameras = cameras;

        public void Apply(float3 anchor, PlatformerTuningConfig tuning)
        {
            var camera = _cameras.Camera?.UnityCamera;
            if (camera == null) return;
            if (_camera != camera)
            {
                Restore();
                _camera = camera;
                _originalPosition = camera.transform.position;
                _originalRotation = camera.transform.rotation;
                _originalOrthographic = camera.orthographic;
                _originalFieldOfView = camera.fieldOfView;
                _originalBackground = camera.backgroundColor;
                _originalClearFlags = camera.clearFlags;
            }
            camera.orthographic = false;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.62f, 0.81f, 0.88f);
            camera.fieldOfView = tuning.CourseCameraFieldOfView;
            Vector3 position = (Vector3)anchor + tuning.CourseCameraOffset;
            Vector3 focus = (Vector3)anchor + tuning.CourseCameraFocusOffset;
            camera.transform.SetPositionAndRotation(position, Quaternion.LookRotation(focus - position, Vector3.up));
            // Discard cached/custom matrices left by the former 2D camera setup.
            camera.ResetWorldToCameraMatrix();
            camera.ResetProjectionMatrix();
            camera.ResetCullingMatrix();
        }

        public void Restore()
        {
            if (_camera != null)
            {
                _camera.transform.SetPositionAndRotation(_originalPosition, _originalRotation);
                _camera.orthographic = _originalOrthographic;
                _camera.fieldOfView = _originalFieldOfView;
                _camera.backgroundColor = _originalBackground;
                _camera.clearFlags = _originalClearFlags;
                _camera.ResetWorldToCameraMatrix();
                _camera.ResetProjectionMatrix();
                _camera.ResetCullingMatrix();
            }
            _camera = null;
        }

        public void Dispose() => Restore();
    }
}
