using System.Runtime.CompilerServices;

namespace PsyCross.Math {
    public struct RectInt {
        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; }

        public int Height { get; set; }

        public RectInt(int x, int y, int width, int height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Rect(RectInt v) => new Rect(v.X, v.Y, v.Width, v.Height);

        public override string ToString() => $"[<{X}, {Y}> {Width}x{Height}]";
    }
}
