using System.Numerics;

namespace PsyCross {
    public sealed class DirectionalLight : Light {
        private Vector3 _direction;

        public Vector3 Direction {
            get => _direction;

            set {
                _direction = value;
                Length = _direction.Length();
                NormalizedDirection = _direction / Length;
            }
        }

        public float Length { get; private set; }

        public Vector3 NormalizedDirection { get; private set; }

        internal DirectionalLight() {
        }
    }
}
