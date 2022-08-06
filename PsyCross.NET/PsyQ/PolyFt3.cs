using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = PolyFt3.Size)]
        public struct PolyFt3 {
            public const byte CommandValue = 0x25;
            public const int Size = 28;

            [FieldOffset( 0)] public Rgb888 Color;
            [FieldOffset( 3)] public byte Command;
            [FieldOffset( 4)] public Point2d P0;
            [FieldOffset( 8)] public Texcoord T0;
            [FieldOffset(10)] public ushort ClutId;
            [FieldOffset(12)] public Point2d P1;
            [FieldOffset(16)] public Texcoord T1;
            [FieldOffset(18)] public ushort TPageId;
            [FieldOffset(20)] public Point2d P2;
            [FieldOffset(24)] public Texcoord T2;
        }
    }
}
