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

        public override string ToString() =>
            $"#{R:X02}{G:X02}{B:X02}";

        public static readonly Rgb888 Black        = new Rgb888(  0,   0,   0);
        public static readonly Rgb888 White        = new Rgb888(255, 255, 255);
        public static readonly Rgb888 Red          = new Rgb888(255,   0,   0);
        public static readonly Rgb888 Green        = new Rgb888(  0, 255,   0);
        public static readonly Rgb888 Blue         = new Rgb888(  0,   0, 255);
        public static readonly Rgb888 Yellow       = new Rgb888(255, 255,   0);
        public static readonly Rgb888 Gray         = new Rgb888(128, 128, 128);
        public static readonly Rgb888 TextureWhite = Gray;
    }
}
