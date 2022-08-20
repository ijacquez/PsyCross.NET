using System.Numerics;
using PsyCross.Math;

namespace PsyCross {
    public class Light {
        public Vector3 Position { get; set; }

        public float ConstantAttenuation { get; set; } = 1.0f;

        public Rgb888 Color { get; set; }

        public float DiffuseIntensity { get; set; }

        public float CutOffDistance { get; set; }

        public LightFlags Flags { get; set; }

        internal Light() {
        }
    }
}
