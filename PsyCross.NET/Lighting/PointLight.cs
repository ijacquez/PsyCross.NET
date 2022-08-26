using System.Numerics;

namespace PsyCross {
    public sealed class PointLight : Light {
        public Vector3 Position { get; set; }

        internal PointLight() {
        }
    }
}
