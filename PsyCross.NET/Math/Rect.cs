using System.Runtime.CompilerServices;

namespace PsyCross.Math {
    public struct Rect {
        public float X { get; set; }

        public float Y { get; set; }

        public float Width { get; set; }

        public float Height { get; set; }

        public Rect(float x, float y, float width, float height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RectInt(Rect v) => new RectInt((int)v.X, (int)v.Y, (int)v.Width, (int)v.Height);

        public override string ToString() => $"[<{X}, {Y}> {Width}x{Height}]";
    }
}
