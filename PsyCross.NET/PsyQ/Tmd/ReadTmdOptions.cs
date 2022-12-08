using System.Numerics;

namespace PsyCross {
    public static partial class PsyQ {
        public class ReadTmdOptions {
            public ReadTmdFlags Flags { get; set; }

            public Matrix4x4 Axis { get; set; } = Matrix4x4.Identity;

            public float Scale { get; set; } = 1f;
        }
    }
}
