using PsyCross.Devices.Input;

namespace PsyCross.Testing {
    public sealed class FlyCamera {
        private const float _CameraSpeed = 1.0f;

        private const float _YawAngleSpeed   = 30.0f;
        private const float _PitchAngleSpeed = 30.0f;

        private readonly Camera _camera;

        private FlyCamera() {
        }

        public FlyCamera(Camera camera) {
            _camera = camera;
        }

        public void Update() {
            if ((Psx.Input & JoyPad.Up) == JoyPad.Up) {
                _camera.Position -= _camera.Forward * _CameraSpeed * Psx.Time.DeltaTime; // Forward
            }

            if ((Psx.Input & JoyPad.Down) == JoyPad.Down) {
                _camera.Position += _camera.Forward * _CameraSpeed * Psx.Time.DeltaTime; // Backwards
            }

            if ((Psx.Input & JoyPad.Left) == JoyPad.Left) {
                _camera.Position -= _camera.Right * _CameraSpeed * Psx.Time.DeltaTime; // Left
            }

            if ((Psx.Input & JoyPad.Right) == JoyPad.Right) {
                _camera.Position += _camera.Right * _CameraSpeed * Psx.Time.DeltaTime; // Right
            }

            if ((Psx.Input & JoyPad.Triangle) == JoyPad.Triangle) {
                _camera.Position += _camera.Up * _CameraSpeed * Psx.Time.DeltaTime; // Up
            }

            if ((Psx.Input & JoyPad.Cross) == JoyPad.Cross) {
                _camera.Position -= _camera.Up * _CameraSpeed * Psx.Time.DeltaTime; // Down
            }

            if ((Psx.Input & JoyPad.L1) == JoyPad.L1) {
                _camera.Yaw += -_YawAngleSpeed * Psx.Time.DeltaTime;
            }

            if ((Psx.Input & JoyPad.R1) == JoyPad.R1) {
                _camera.Yaw += _YawAngleSpeed * Psx.Time.DeltaTime;
            }

            if ((Psx.Input & JoyPad.L2) == JoyPad.L2) {
                _camera.Pitch += -_PitchAngleSpeed * Psx.Time.DeltaTime;
            }

            if ((Psx.Input & JoyPad.R2) == JoyPad.R2) {
                _camera.Pitch += _PitchAngleSpeed * Psx.Time.DeltaTime;
            }
        }
    }
}
