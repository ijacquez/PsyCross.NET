using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        public interface ITmdPrimitive {
            int IndexV0 { get; }
            int IndexV1 { get; }
            int IndexV2 { get; }
            int IndexV3 { get; }

            int IndexN0 { get; }
            int IndexN1 { get; }
            int IndexN2 { get; }
            int IndexN3 { get; }

            Texcoord T0 { get; }
            Texcoord T1 { get; }
            Texcoord T2 { get; }
            Texcoord T3 { get; }

            TmdTsb Tsb { get; }

            TmdCba Cba { get; }

            int VertexCount { get; }

            int NormalCount { get; }
        }
    }
}
