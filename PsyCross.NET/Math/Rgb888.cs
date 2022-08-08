using System.Runtime.InteropServices;

namespace PsyCross.Math {
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 3)]
    public struct Rgb888 {
        [FieldOffset(0)] public byte R;
        [FieldOffset(1)] public byte G;
        [FieldOffset(2)] public byte B;

        public Rgb888(byte r, byte g, byte b) {
            R = r;
            G = g;
            B = b;
        }
    }
}
