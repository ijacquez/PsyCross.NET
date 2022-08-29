using System.Numerics;

namespace PsyCross {
    public sealed class PointLight : Light {
        private float _range;

        public Vector3 Position { get; set; }

        public float Range {
            get => _range;

            set {
                _range = System.Math.Max(value, 0.001f);
                RangeReciprocal = 1f / _range;
            }
        }

        public float RangeReciprocal { get; private set; }

        public float CutOffDistance { get; set; }

        internal PointLight() {
        }
    }
}
