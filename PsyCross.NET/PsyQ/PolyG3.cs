using System.Runtime.InteropServices;
using PsyCross.Math;

namespace PsyCross {
    public static partial class PsyQ {
        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = PolyG3.Size)]
        public struct PolyG3 {
            public const byte CommandValue = 0x30;
            public const int Size = 24;

            [FieldOffset( 0)] public Rgb888 C0;
            [FieldOffset( 3)] public byte Command;
            [FieldOffset( 4)] public Point2d P0;
            [FieldOffset( 8)] public Rgb888 C1;
            [FieldOffset(12)] public Point2d P1;
            [FieldOffset(16)] public Rgb888 C2;
            [FieldOffset(20)] public Point2d P2;
        }
    }
}
