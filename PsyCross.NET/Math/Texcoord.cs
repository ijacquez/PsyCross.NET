using System.Runtime.InteropServices;

namespace PsyCross.Math {
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 2)]
    public struct Texcoord {
        [FieldOffset( 0)] public byte X;
        [FieldOffset( 1)] public byte Y;

        public Texcoord(byte x, byte y) {
            X = x;
            Y = y;
        }
    }
}
