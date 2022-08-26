using PsyCross.Math;

namespace PsyCross {
    public abstract class Light {
        public float ConstantAttenuation { get; set; }

        public Rgb888 Color { get; set; }

        public float DiffuseIntensity { get; set; }

        public float CutOffDistance { get; set; }

        public LightFlags Flags { get; set; }

        public Light() {
        }

        internal void Init() {
            ConstantAttenuation = 1.0f;
            Color = Rgb888.White;
            DiffuseIntensity = 1.0f;
            CutOffDistance = 100.0f;
            Flags = LightFlags.None;
        }
    }
}
