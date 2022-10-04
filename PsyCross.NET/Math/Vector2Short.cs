using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PsyCross.Math {
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Vector2Short {
        [FieldOffset( 0)] public short X;
        [FieldOffset( 2)] public short Y;

        public Vector2Short(short x, short y) {
            X = x;
            Y = y;
        }

        public static Vector2Short Zero => new Vector2Short(0, 0);

        public static Vector2Short One => new Vector2Short(1, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Int(Vector2Short v) => new Vector2Int(v.X, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Short(Vector2Int v) => new Vector2Short((short)v.X, (short)v.Y);

        public override string ToString() => $"<{X}, {Y}>";
    }
}
