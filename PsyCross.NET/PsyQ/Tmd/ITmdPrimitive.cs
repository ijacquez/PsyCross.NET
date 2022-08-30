using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        public interface ITmdPrimitive {
            TmdPrimitiveType Type { get; }

            int IndexV0 { get; }
            int IndexV1 { get; }
            int IndexV2 { get; }
            int IndexV3 { get; }

            int IndexN0 { get; }
            int IndexN1 { get; }
            int IndexN2 { get; }
            int IndexN3 { get; }

            Rgb888 C0 { get; }
            Rgb888 C1 { get; }
            Rgb888 C2 { get; }
            Rgb888 C3 { get; }

            Texcoord T0 { get; }
            Texcoord T1 { get; }
            Texcoord T2 { get; }
            Texcoord T3 { get; }

            TmdTsb Tsb { get; }

            TmdCba Cba { get; }

            int VertexCount { get; }

            int NormalCount { get; }

            int ColorCount { get; }
        }
    }
}
