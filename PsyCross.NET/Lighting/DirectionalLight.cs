using System.Numerics;

namespace PsyCross {
    public sealed class DirectionalLight : Light {
        public Vector3 Direction { get; set; }

        internal DirectionalLight() {
        }
    }
}
