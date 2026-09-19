using ForgeFlow.Core.Systems;
using ForgeFlow.Presentation.Unity.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace ForgeFlow.Presentation.Unity.Cameras
{
    /// <summary>
    /// Runtime camera controller with smooth zoom, pan (WASD or right-click drag),
    /// and rotation (middle-click drag — free or 90° snap per CameraSettings).
    /// Starts centered on the terrain grid center.
    /// Attached to the root GameObject by <see cref="FactoryEntryPoint"/>.
    /// </summary>
    internal sealed class CameraController : MonoBehaviour
    {
        private Camera _camera = null!;
        private CameraSettings _settings = new();

        private float _currentZoom;
        private float _currentYaw;
        private float _targetYaw;

        private Vector3 _lastMousePosition;
        private bool _isPanning;
        private bool _isDraggingRotation;

        private float _terrainCenterX;
        private float _terrainCenterZ;

        public void Initialize(Camera baseCamera, CameraSettings settings, int terrainWidth, int terrainHeight)
        {
            _camera = baseCamera;
            _settings = settings;
            _currentZoom = settings.DefaultZoom;
            _currentYaw = 0f;
            _targetYaw = _currentYaw;

            _terrainCenterX = terrainWidth * 0.5f;
            _terrainCenterZ = terrainHeight * 0.5f;

            // Center the camera on the terrain
            var centerPos = new Vector3(_terrainCenterX, 0f, _terrainCenterZ);
            var offset = CalculateCameraOffset(_currentZoom, _currentYaw);
            _camera.transform.position = centerPos + offset;
            _camera.transform.LookAt(centerPos);

            if (_camera.orthographic)
            {
                _camera.orthographicSize = _currentZoom;
            }
        }

        private void Update()
        {
            if (_camera == null) return;

            HandleZoom();
            HandlePan();
            HandleRotation();
            ApplyCamera();
        }

        private void HandleZoom()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;
            var scrollDelta = mouse.scroll.ReadValue().y / 120f;
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                _currentZoom -= scrollDelta * _settings.ZoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, _settings.MinZoom, _settings.MaxZoom);
            }
        }

        private void HandlePan()
        {
            var panDir = Vector3.zero;
            float dt = Time.deltaTime;

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // WASD keys
            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed) panDir += Vector3.forward;
                if (keyboard.sKey.isPressed) panDir += Vector3.back;
                if (keyboard.aKey.isPressed) panDir += Vector3.left;
                if (keyboard.dKey.isPressed) panDir += Vector3.right;
            }

            // Right-click drag
            if (mouse != null)
            {
                if (mouse.rightButton.wasPressedThisFrame)
                {
                    _isPanning = true;
                    _lastMousePosition = mouse.position.ReadValue();
                }
                if (mouse.rightButton.wasReleasedThisFrame)
                {
                    _isPanning = false;
                }
                if (_isPanning)
                {
                    Vector2 currentPos = mouse.position.ReadValue();
                    var delta = currentPos - (Vector2)_lastMousePosition;
                    _lastMousePosition = currentPos;

                    var yawRad = _currentYaw * Mathf.Deg2Rad;
                    var right = new Vector3(Mathf.Cos(yawRad), 0f, -Mathf.Sin(yawRad));
                    var forward = new Vector3(Mathf.Sin(yawRad), 0f, Mathf.Cos(yawRad));

                    panDir += (-right * delta.x - forward * delta.y) * 0.02f;
                }
            }

            if (panDir.sqrMagnitude > 0.001f)
            {
                // Rotate the pan direction by the camera yaw for WASD
                if (!_isPanning || panDir.sqrMagnitude > 0.1f)
                {
                    var yawRot = Quaternion.Euler(0f, _currentYaw + 45f, 0f);
                    var worldPan = yawRot * panDir.normalized;
                    _terrainCenterX += worldPan.x * _settings.PanSpeed * dt;
                    _terrainCenterZ += worldPan.z * _settings.PanSpeed * dt;
                }
            }
        }

        private void HandleRotation()
        {
            var mouse = Mouse.current;
            if (mouse == null) return;

            // Middle-click drag
            if (mouse.middleButton.wasPressedThisFrame)
            {
                _isDraggingRotation = true;
                _lastMousePosition = mouse.position.ReadValue();
            }
            if (mouse.middleButton.wasReleasedThisFrame)
            {
                _isDraggingRotation = false;
                if (_settings.SnapRotation)
                {
                    // Snap to nearest 90°
                    _targetYaw = Mathf.Round(_currentYaw / 90f) * 90f;
                }
            }

            if (_isDraggingRotation && !_settings.SnapRotation)
            {
                Vector2 currentPos = mouse.position.ReadValue();
                var delta = currentPos - (Vector2)_lastMousePosition;
                _lastMousePosition = currentPos;
                _currentYaw += delta.x * _settings.RotationSpeed * Time.deltaTime * 0.1f;
                _targetYaw = _currentYaw;
            }
            else if (_isDraggingRotation && _settings.SnapRotation)
            {
                Vector2 currentPos = mouse.position.ReadValue();
                var delta = currentPos - (Vector2)_lastMousePosition;
                if (Mathf.Abs(delta.x) > 50f)
                {
                    _targetYaw += Mathf.Sign(delta.x) * 90f;
                    _lastMousePosition = currentPos;
                }
            }

            // Smoothly lerp to target yaw
            _currentYaw = Mathf.LerpAngle(_currentYaw, _targetYaw, Time.deltaTime * 8f);
        }

        private void ApplyCamera()
        {
            var centerPos = new Vector3(_terrainCenterX, 0f, _terrainCenterZ);

            if (_camera.orthographic)
            {
                _camera.orthographicSize = Mathf.Lerp(
                    _camera.orthographicSize, _currentZoom, Time.deltaTime * 10f);

                var offset = CalculateCameraOffset(_currentZoom, _currentYaw);
                _camera.transform.position = Vector3.Lerp(
                    _camera.transform.position, centerPos + offset, Time.deltaTime * 10f);
                _camera.transform.rotation = Quaternion.Slerp(
                    _camera.transform.rotation,
                    Quaternion.Euler(30f, _currentYaw + 45f, 0f),
                    Time.deltaTime * 10f);
            }
            else
            {
                var offset = CalculateCameraOffset(_currentZoom, _currentYaw);
                _camera.transform.position = Vector3.Lerp(
                    _camera.transform.position, centerPos + offset, Time.deltaTime * 10f);
                _camera.transform.LookAt(centerPos);
            }
        }

        private static Vector3 CalculateCameraOffset(float zoom, float yaw)
        {
            var distance = zoom * 1.4f;
            var height = zoom * 0.8f;
            var yawRad = (yaw + 45f) * Mathf.Deg2Rad;
            return new Vector3(
                Mathf.Sin(yawRad) * distance,
                height,
                -Mathf.Cos(yawRad) * distance);
        }

        public void UpdateSettings(CameraSettings settings)
        {
            _settings = settings;
        }

        /// <summary>
        /// Rotates the camera 90° clockwise. Called by <see cref="CameraRotateController"/>
        /// when the R key is not consumed by a higher-priority controller.
        /// </summary>
        public void RotateCamera90()
        {
            _targetYaw += 90f;
        }
    }
}
