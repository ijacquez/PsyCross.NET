using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PsyCross.Math {
    [StructLayout(LayoutKind.Explicit, Size = 4)]
    public struct Point2d {
        [FieldOffset( 0)] public short X;
        [FieldOffset( 2)] public short Y;

        public Point2d(short x, short y) {
            X = x;
            Y = y;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Point2d(Vector2Int v) => new Point2d((short)v.X, (short)v.Y);

        public static Point2d Zero => new Point2d(0, 0);
    }
}
