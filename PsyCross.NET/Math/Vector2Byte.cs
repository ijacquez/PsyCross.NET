using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace PsyCross.Math {
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = 2)]
    public struct Vector2Byte {
        [FieldOffset( 1)] public byte X;
        [FieldOffset( 0)] public byte Y;

        public Vector2Byte(byte x, byte y) {
            X = x;
            Y = y;
        }

        public static Vector2Byte Zero => new Vector2Byte(0, 0);

        public static Vector2Byte One => new Vector2Byte(1, 1);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Int(Vector2Byte v) => new Vector2Int(v.X, v.Y);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Vector2Byte(Vector2Int v) => new Vector2Byte((byte)v.X, (byte)v.Y);

        public override string ToString() => $"<{X}, {Y}>";
    }
}
