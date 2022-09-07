using System;
using System.Numerics;
using PsyCross.Math;

namespace PsyCross.Testing {
    public class Camera {
        // Rotation around the X axis (radians)
        private float _pitch;

        // Rotation around the Y axis (radians). Without this, you would be
        // started rotated 90 degrees right
        private float _yaw = -System.MathF.PI / 2.0f;

        // The field of view of the camera (radians)
        private float _fov = System.MathF.PI / 2.0f;

        private Camera() {
        }

        public Camera(int screenWidth, int screenHeight) {
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
            AspectRatio = ScreenWidth / (float)ScreenHeight;

            UpdateViewDistance();
            UpdateVectors();
        }

        public int ScreenWidth { get; }

        public int ScreenHeight { get; }

        public float DepthNear { get; set; } = 0.5f;

        public float DepthFar => ViewDistance / 2f;

        public float ViewDistance { get; private set; }

        // The position of the camera
        public Vector3 Position { get; set; } = Vector3.Zero;

        // This is simply the aspect ratio of the viewport, used for the
        // projection matrix
        public float AspectRatio { get; }

        public Vector3 Forward { get; private set; } = -Vector3.UnitZ;

        public Vector3 Up { get; private set; } = Vector3.UnitY;

        public Vector3 Right { get; private set; } = Vector3.UnitX;

        // We convert from degrees to radians as soon as the property is set to
        // improve performance
        public float Pitch {
            get => MathHelper.RadiansToDegrees(_pitch);

            set {
                // We clamp the pitch value between -89 and 89 to prevent the
                // camera from going upside down, and a bunch of weird "bugs"
                // when you are using euler angles for rotation. If you want to
                // read more about this you can try researching a topic called
                // gimbal lock
                var angle = System.Math.Clamp(value, -89.5f, 89.5f);

                _pitch = MathHelper.DegreesToRadians(angle);

                UpdateVectors();
            }
        }

        // We convert from degrees to radians as soon as the property is set to
        // improve performance
        public float Yaw {
            get => MathHelper.RadiansToDegrees(_yaw);

            set {
                _yaw = MathHelper.DegreesToRadians(value);

                UpdateVectors();
            }
        }

        public float Fov {
            get => MathHelper.RadiansToDegrees(_fov);

            set {
                var angle = System.Math.Clamp(value, 1.0f, 180.0f);

                _fov = MathHelper.DegreesToRadians(angle);

                UpdateViewDistance();
            }
        }

        public Matrix4x4 GetViewMatrix() =>
            Matrix4x4.CreateLookAt(Position, Position + Forward, Up);

        private void UpdateViewDistance() {
            // This calculation will give us the correct d value if we want to
            // merge the perspective transform and screen transform into one
            ViewDistance = System.MathF.Round(0.5f * (float)ScreenWidth * MathF.Tan(_fov * 0.5f));
        }

        private void UpdateVectors() {
            // First, the forward matrix is calculated using some basic trigonometry
            Vector3 forward = new Vector3() {
                X = MathF.Cos(_pitch) * MathF.Cos(_yaw),
                Y = MathF.Sin(_pitch),
                Z = MathF.Cos(_pitch) * MathF.Sin(_yaw)
            };

            // We need to make sure the vectors are all normalized, as otherwise
            // we would get some funky results
            Forward = Vector3.Normalize(forward);

            // Calculate both the right and the up vector using cross product.
            // Note that we are calculating the right from the global up; this
            // behaviour might not be what you need for all cameras so keep this
            // in mind if you do not want a FPS camera
            Right = Vector3.Normalize(Vector3.Cross(Forward, Vector3.UnitY));
            Up = Vector3.Normalize(Vector3.Cross(Right, Forward));
        }
    }
}
