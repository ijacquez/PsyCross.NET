using System.Runtime.CompilerServices;

namespace PsyCross.Math {
    public struct RectShort {
        public short X { get; set; }

        public short Y { get; set; }

        public ushort Width { get; set; }

        public ushort Height { get; set; }

        public RectShort(short x, short y, ushort width, ushort height) {
            X = x;
            Y = y;
            Width = width;
            Height = height;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RectShort(RectInt v) => new RectShort((short)v.X, (short)v.Y, (ushort)v.Width, (ushort)v.Height);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator RectInt(RectShort v) => new RectInt(v.X, v.Y, v.Width, v.Height);

        public override string ToString() => $"[<{X}, {Y}> {Width}x{Height}]";
    }
}
