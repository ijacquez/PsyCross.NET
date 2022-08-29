using PsyCross.Math;

namespace PsyCross {
    public abstract class Light {
        public Rgb888 Color { get; set; }

        public LightFlags Flags { get; set; }

        protected Light() {
        }
    }
}
