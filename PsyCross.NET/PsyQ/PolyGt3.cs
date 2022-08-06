using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = PolyGt3.Size)]
        public struct PolyGt3 {
            public const byte CommandValue = 0x34;
            public const int Size = 36;

            [FieldOffset( 0)] public Rgb888 C0;
            [FieldOffset( 3)] public byte Command;
            [FieldOffset( 4)] public Point2d P0;
            [FieldOffset( 8)] public Texcoord T0;
            [FieldOffset(10)] public ushort ClutId;
            [FieldOffset(12)] public Rgb888 C1;
            [FieldOffset(16)] public Point2d P1;
            [FieldOffset(20)] public Texcoord T1;
            [FieldOffset(22)] public ushort TPageId;
            [FieldOffset(24)] public Rgb888 C2;
            [FieldOffset(28)] public Point2d P2;
            [FieldOffset(32)] public Texcoord T2;
        }
    }
}
